// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ImageResizer.Configuration.Xml;
using Imazen.Common.Issues;
using PublicApiGenerator;
using Xunit;

namespace ImageResizer.TestAPISurface
{
    public class GenerateAPITexts
    {

        private string GetApiTextDir()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string solutionDir = null;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "ImageResizer.sln")))
                {
                    solutionDir = dir;
                    break;
                }
                var parent =  Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }

            return solutionDir == null ? null : Path.Combine(solutionDir, "tests", "api-surface");
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
                typeof(Imazen.Common.Issues.Issue),
                typeof(ImageResizer.Storage.Blob),
                typeof(ImageResizer.Plugins.S3Reader2.S3Reader2),
                typeof(ImageResizer.Plugins.AzureReader2.AzureReader2Plugin),
                typeof(ImageResizer.Plugins.HybridCache.HybridCachePlugin),
                typeof(ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin),
                typeof(ImageResizer.Plugins.Imageflow.ImageflowBackendPlugin),

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