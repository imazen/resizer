// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using ImageResizer.Util;
using System.Drawing;
using System.Diagnostics;
using System.Web;
using System.Threading;
using System.Globalization;
using ImageResizer.ExtensionMethods;
using System.Xml.Linq;
using System.Xml;
using System.Web.Hosting;
using ImageResizer.Configuration;
using AForge.Imaging.Filters;
using System.Linq;

namespace ImageResizer.Plugins.FFmpeg
{
    public class FFmpegManager
    {

        public FFmpegManager()
        {
            MaxConcurrentExecutions = 0;
            MaxConcurrentWaitingThreads = 0;

        }

 
        private string ffmpegPath;
        private string ffprobePath;

        public string GetFFmpegPath()
        {
            if (ffmpegPath == null) LocateExeFiles();
            return ffmpegPath;
        }

        public string GetFFprobePath()
        {
            if (ffprobePath == null) LocateExeFiles();
            return ffprobePath;
        }

        private object _locateLock = new object();
        private void LocateExeFiles()
        {
            lock (_locateLock)
            {
                List<string> searchFolders = new List<string>() { };

                var a = this.GetType().Assembly;
                //Use CodeBase if it is physical; this means we don't re-download each time we recycle. 
                //If it's a URL, we fall back to Location, which is often the shadow-copied version.
                var searchFolder = a.CodeBase.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
                                    ? a.CodeBase
                                    : a.Location;
                //Convert UNC paths 
                searchFolder = Path.GetDirectoryName(searchFolder.Replace("file:///", "").Replace("/", "\\"));

                searchFolders.Add(searchFolder);

                foreach (string basePath in searchFolders)
                {
                    if (ffmpegPath == null)
                    {
                        string m = basePath.TrimEnd('\\') + '\\' + "ffmpeg.exe";
                        if (File.Exists(Path.GetFullPath(m))) ffmpegPath = Path.GetFullPath(m);
                    }
                    if (ffprobePath == null)
                    {
                        string p = basePath.TrimEnd('\\') + '\\' + "ffprobe.exe";
                        if (File.Exists(Path.GetFullPath(p))) ffprobePath = Path.GetFullPath(p);
                    }

                }
                if (ffmpegPath == null) throw new FileNotFoundException("Failed to locate ffmpeg.exe in the bin folder");
                if (ffprobePath == null) throw new FileNotFoundException("Failed to locate ffprobe.exe in the bin folder");
            }
        }

        public int MaxConcurrentExecutions { get; set; }

        public int MaxConcurrentWaitingThreads { get; set; }
        protected string cairPath = null;
        protected object cairLock = new object();

        /// <summary>
        /// Number of executing CAIR.exe processes
        /// </summary>
        private int _concurrentExecutions = 0;
        /// <summary>
        /// Number of threads waiting for a CAIR.exe process.
        /// </summary>
        private int _concurrentWaitingThreads = 0;
        /// <summary>
        /// Used for efficient thread waiting
        /// </summary>
        private AutoResetEvent turnstile = new AutoResetEvent(true);

        public bool Execute(FFmpegJob job)
        {

            //If we have too many threads waiting to run CAIR, just kill the request.
            if (MaxConcurrentWaitingThreads > 0 &&
                _concurrentWaitingThreads > MaxConcurrentWaitingThreads)
                throw new Exception("FFmpeg failed - too many threads waiting. Try again later.");

            //If there are any threads waiting in line, or if the permitted number of CAIR.exe instances has been reached, get in line
            if (_concurrentWaitingThreads > 0 || (MaxConcurrentExecutions > 0 &&
                    _concurrentExecutions > MaxConcurrentExecutions))
            {
                try
                {
                    Interlocked.Increment(ref _concurrentWaitingThreads);
                    //Wait for a free slot
                    while (MaxConcurrentExecutions > 0 &&
                        _concurrentExecutions > MaxConcurrentExecutions)
                    {
                        turnstile.WaitOne(1000);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _concurrentWaitingThreads);
                }
            }
            //Ok, there should be a free slot now.
            try
            {
                //Register, we have our own process slot now.
                Interlocked.Increment(ref _concurrentExecutions);

                return InnerExecute(job);

            }
            finally
            {
                Interlocked.Decrement(ref _concurrentExecutions);
                turnstile.Set();
            }




        }


        private XElement GetVideoInfo(FFmpegJob job)
        {
            //*ffprobe.exe -loglevel error -show_format -show_streams inputFile.extension -print_format json*/
            string result = RunExecutable(GetFFprobePath(), " -loglevel error -show_format -print_format xml -i \"" + job.SourcePath + "\"", job.Timeout /2);
            return XElement.Parse(result);
        }

  
        private bool InnerExecute(FFmpegJob job)
        {
            if (job.Seconds == null){
                var xml = GetVideoInfo(job);
                var duration = (double)xml.Descendants("format").FirstOrDefault().Attributes("duration").FirstOrDefault();
                job.Seconds = duration * job.Percent / 100;
            }

            bool failedTest = false;
            var tries = 0;
            do
            {
                if (tries > 4) throw new Exception("Failed to locate a non-blank frame");
                
                var path = Path.GetTempFileName();
                byte[] result;
                try{
                    string message = RunExecutable(GetFFmpegPath(), " -ss " + job.Seconds.ToString() + " -y -i \"" + job.SourcePath + "\" -an -vframes 1 -r 1 -f image2 -pix_fmt rgb24  \"" + path + "\"", job.Timeout);

                    if (message.Contains("Output file is empty, nothing was encoded"))
                    {
                        throw new Exception("You are outside the bounds of the video");
                    }
                    result = File.ReadAllBytes(path);
                    job.Result = new MemoryStream(result);
                }finally{
                    File.Delete(path);
                }
                failedTest = (job.SkipBlankFrames == true && IsBlank(result,10));
                if (failedTest) job.Seconds += job.IncrementWhenBlank ?? 5;
                tries++;

            } while (failedTest);

            return true;
        }

        /// <summary>
        /// Returns true if the average energy of the image is below the given threshold
        /// </summary>
        /// <param name="image"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private bool IsBlank(byte[] image, byte threshold)
        {
            var ms = new MemoryStream(image);

            using (var b = new Bitmap(ms))
            {
                using (var gray = Grayscale.CommonAlgorithms.BT709.Apply(b))
                {
                    new SobelEdgeDetector().ApplyInPlace(gray);
                    var p = new SimplePosterization(SimplePosterization.PosterizationFillingType.Average);
                    p.PosterizationInterval = 128;
                    p.ApplyInPlace(gray);
                    return gray.GetPixel(0, 0).R < threshold;
                }
                
                   
            }

        }

        public Stream GetFrameStream(Config c,string virtualPath, System.Collections.Specialized.NameValueCollection queryString)
        {
            var job = new FFmpegJob(queryString);

            
            bool bufferToTemp = !File.Exists(HostingEnvironment.MapPath(virtualPath));

            job.SourcePath = !bufferToTemp ? HostingEnvironment.MapPath(virtualPath) : Path.GetTempFileName();
            try
            {
                if (bufferToTemp)
                {
                    using (Stream input = c.Pipeline.GetFile(virtualPath, new System.Collections.Specialized.NameValueCollection()).Open())
                    using (Stream output = File.Create(job.SourcePath))
                    {
                        input.CopyToStream(output);
                    }
                }
                this.Execute(job);

                return job.Result;
            }
            finally
            {
                if (bufferToTemp) File.Delete(job.SourcePath);
            }
        }


        private string RunExecutable(string filename, string arguments, int timeout)
        {

            ProcessStartInfo info = new ProcessStartInfo(filename, arguments);
            info.UseShellExecute = false;
            info.RedirectStandardError = true;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;

            using (Process p = Process.Start(info))
            {
                bool result = p.WaitForExit(timeout);
                if (!result)
                {
                    p.Kill(); //Kill the process if it times out.
                    throw new Exception("FFmpeg failed due to timeout.");
                }
                string messages = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
                if (p.ExitCode != 0)
                    throw new Exception("FFmpeg failed: " + messages);
                return messages;
            }
        }



    }
}
