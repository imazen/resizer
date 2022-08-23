// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ImageResizer.Configuration.Xml;
using PublicApiGenerator;
using Xunit;

namespace ImageResizer.TestAPISurface
{
    public class GenerateAPITexts
    {

        private string GetApiTextDir()
        {
            string codeBase = null;
            try
            {
                codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            }
            catch { }

            var searchLocations = new string[] { Assembly.GetExecutingAssembly().Location, typeof(ImageResizer.BoxEdges).Assembly.Location, codeBase };
            foreach(var location in searchLocations)
            {
                var attempt = location != null ? FindSolutionDir(Path.GetDirectoryName(location), "AppVeyor.sln") : null;
                if (attempt != null) return Path.Combine(attempt, "tests", "api-surface");
            }
            return null;
        }

        private string FindSolutionDir(string startDir, string filename)
        {
            var dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, filename)))
                {
                    return dir;
                }
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) return null;
                dir = parent;
            }
            return null;
        }


        [Fact]
        public void GenerateAPISurfaceText()
        {
            var dir = GetApiTextDir();
            if (dir == null) return; // We can do nothing
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var types = new Type[]
            {
                typeof(ImageResizer.ImageBuilder),
                typeof(ImageResizer.Storage.Blob),
                typeof(ImageResizer.Plugins.S3Reader2.S3Reader2),
                typeof(ImageResizer.Plugins.AzureReader2.AzureReader2Plugin),
                typeof(ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin),

            };

            foreach (var t in types)
            {
                var assembly = t.Assembly;
                var assemblyName = assembly.GetName().Name;
                var apiText = assembly.GeneratePublicApi(new ApiGeneratorOptions());

                var fileName = Path.Combine(dir, assemblyName + ".txt");
                File.WriteAllText(fileName, apiText);
            }
        }
    }
}