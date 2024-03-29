// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using ImageResizer.Collections;
using Imazen.Common.Issues;
using ImageResizer.Configuration.Xml;
using ImageResizer.Util;

namespace ImageResizer.Configuration.Plugins
{
    /// <summary>
    ///     Provides automatic download of native dependencies (which VS doesn't see). Gets the correct bitness as well - very
    ///     nice if you're changing app pool bitness and forgot to change binaries.
    /// </summary>
    public class NativeDependencyManager : IssueSink
    {
        public NativeDependencyManager() : base("NativeDependencyManager")
        {
            try
            {
                var a = GetType().Assembly;
                //Use CodeBase if it is physical; this means we don't re-download each time we recycle. 
                //If it's a URL, we fall back to Location, which is often the shadow-copied version.
                TargetFolder = a.CodeBase.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
                    ? a.CodeBase
                    : a.Location;
                //Convert UNC paths 
                TargetFolder = Path.GetDirectoryName(TargetFolder.Replace("file:///", "").Replace("/", "\\"));
            }
            catch (SecurityException)
            {
                TargetFolder = null;
            }
        }


        private string TargetFolder = null;

        private SafeList<string> filesVerified = new SafeList<string>();

        private SafeList<string> assembliesProcessed = new SafeList<string>();


        public void EnsureLoaded(Assembly a)
        {
            if (assembliesProcessed.Contains(a.FullName)) return;
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var attrs = a.GetCustomAttributes(typeof(NativeDependenciesAttribute), true);
                if (attrs.Length == 0) return;
                var attr = attrs[0] as NativeDependenciesAttribute;
                var shortName = a.GetName().Name;
                var resourceName = shortName + "." + attr.Value;

                var info = a.GetManifestResourceInfo(resourceName);
                if (info == null)
                {
                    AcceptIssue(new Issue("Plugin error - failed to find embedded resource " + resourceName,
                        IssueSeverity.Error));
                    return;
                }

                using (var s = a.GetManifestResourceStream(resourceName))
                {
                    var x = new XmlDocument();
                    x.Load(s);

                    var n = new Node(x.DocumentElement, this);
                    EnsureLoaded(n, shortName, sw);
                }
            }
            finally
            {
                assembliesProcessed.Add(a.FullName);
            }
        }

        private class Dependency
        {
            public string Name;
            public long ExistingLength;
            public bool Exists;
            public WebClient Client;
            public long ExpectedLength;
            public string Url;
            public string DestPath;
            public string RequestingAssembly;
        }

        public void EnsureLoaded(Node manifest, string assemblyName, Stopwatch sw = null)
        {
            if (TargetFolder == null)
            {
                AcceptIssue(
                    new Issue("Application does not have IOPermission; Native dependencies for " + assemblyName +
                              " will not be downloaded if missing"));
                return;
            }

            var platform = IntPtr.Size == 8 ? "64" : "32";

            var q = new Queue<Dependency>();
            try
            {
                foreach (var c in manifest.childrenByName("file"))
                {
                    var bitness = c.Attrs["bitness"]; //Skip files with the wrong bitness
                    if (bitness != null && !bitness.Equals(platform, StringComparison.OrdinalIgnoreCase)) continue;

                    var name = c.Attrs["name"]; //Skip duplicate names
                    if (string.IsNullOrEmpty(name))
                        AcceptIssue(new Issue(
                            "Missing attribute 'name' in native dependency manifest for " + assemblyName,
                            IssueSeverity.Warning));
                    if (filesVerified.Contains(name)) continue;

                    //What is the expected size? If none listed, any size will work. 
                    var fileBytes = 0;
                    if (c.Attrs["fileBytes"] != null && !int.TryParse(c.Attrs["fileBytes"], NumberStyles.Number,
                            NumberFormatInfo.InvariantInfo, out fileBytes))
                        AcceptIssue(new Issue(
                            "Failed to parse fileBytes value " + c.Attrs["fileBytes"] +
                            " in native dependency manifest for " + assemblyName, IssueSeverity.Warning));

                    //Download URL?
                    var url = c.Attrs["url"];


                    var destPath = Path.Combine(TargetFolder, name);

                    long existingLength = 0;

                    //Does it already exist?
                    if (File.Exists(destPath))
                    {
                        if (fileBytes < 1)
                        {
                            filesVerified.Add(name);
                            continue;
                        }
                        else
                        {
                            existingLength = new FileInfo(destPath).Length;
                            if (existingLength == fileBytes)
                            {
                                filesVerified.Add(name);
                                continue;
                            }
                        }
                    }

                    var d = new Dependency()
                    {
                        Exists = existingLength > 0, Name = name, Url = url, DestPath = destPath,
                        ExistingLength = existingLength, ExpectedLength = fileBytes, Client = new WebClient(),
                        RequestingAssembly = assemblyName
                    };
                    q.Enqueue(d);
                }

                sw.Stop();
                if (sw.ElapsedMilliseconds > 100 && q.Count < 1)
                    AcceptIssue(new Issue(
                        "Verifying native dependencies for " + assemblyName + " took " + sw.ElapsedMilliseconds + "ms.",
                        IssueSeverity.Warning));

                ServicePointManager.DefaultConnectionLimit = 1000; //Allow more than 2 simultaneous HTTP requests.
                var message = new StringBuilder();
                if (q.Count > 0)
                {
                    var dsw = new Stopwatch();
                    dsw.Start();
                    using (var cd = new Countdown(q.Count))
                    {
                        foreach (var current in q.ToArray())
                        {
                            var d = current;
                            ThreadPool.QueueUserWorkItem(x =>
                            {
                                DownloadFile(d, message);
                                cd.Signal();
                            });
                        }

                        cd.Wait();
                    }

                    dsw.Stop();
                    AcceptIssue(new Issue(
                        "Some native dependencies for " + assemblyName +
                        " were missing, but were downloaded successfully. This delayed startup time by " +
                        (sw.ElapsedMilliseconds + dsw.ElapsedMilliseconds).ToString() + "ms.", message.ToString(),
                        IssueSeverity.Warning));
                }
            }
            finally
            {
                foreach (var d in q.ToArray()) d.Client.Dispose();
            }
        }

        private void DownloadFile(Dependency d, StringBuilder message)
        {
            try
            {
                if (File.Exists(d.DestPath)) File.Delete(d.DestPath);
                d.Client.DownloadFile(d.Url, d.DestPath);
                lock (message)
                {
                    if (d.Exists)
                        message.AppendLine(d.Name + " reported size of " + d.ExistingLength + " instead of expected " +
                                           d.ExpectedLength + " Re-downloaded from " + d.Url);
                    else message.AppendLine(d.Name + " was missing. Downloaded from " + d.Url);
                }

                var downloadLength = new FileInfo(d.DestPath).Length;
                if (downloadLength != d.ExpectedLength)
                    AcceptIssue(new Issue("Infinite dependency download! Expected file length " + d.ExpectedLength +
                                          " Downloaded file length " + downloadLength +
                                          ". Please notify support that the dependency manifest for " +
                                          d.RequestingAssembly + " needs to be updated."));
            }
            catch (Exception we)
            {
                AcceptIssue(new Issue(
                    "Failed to download native dependency " + d.Name + " for " + d.RequestingAssembly + " from " +
                    d.Url, we.Message, IssueSeverity.Error));
            }
        }


        /// <summary>
        ///     Thread safe countdown class
        /// </summary>
        private class Countdown : IDisposable
        {
            private readonly ManualResetEvent done;
            private readonly int total;
            private volatile int current;

            public Countdown(int total)
            {
                this.total = total;
                current = total;
                done = new ManualResetEvent(false);
            }

            public void Signal()
            {
                lock (done)
                {
                    if (current > 0 && --current == 0)
                        done.Set();
                }
            }

            public void Wait()
            {
                if (current > 0) done.WaitOne();
            }

            public void Dispose()
            {
                ((IDisposable)done).Dispose();
            }
        }
    }
}