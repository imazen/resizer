// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageResizer;
using ImageResizer.Configuration;

namespace ConsoleApplication
{
    internal class Program
    {
        private static string GetRepositoryRoot(string startingDir)
        {
            // 
            var dir = startingDir;
            while (dir != null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir, ".git")))
            {
                dir = System.IO.Directory.GetParent(dir)?.FullName;
            }
            return dir;
        }
        private static void Main(string[] args)
        {
            var imageDir = Path.Combine(GetRepositoryRoot(Environment.CurrentDirectory) + "\\examples\\images\\");
            
            var inputImage = Path.Combine(imageDir, "quality-original.jpg");
            
            var outputImage = Path.Combine(imageDir, "output.png");
            
            var watermarkImage = Path.Combine(imageDir, "Sun_256.png");

            var c = new Config(new ResizerSection("<resizer><plugins><add name=\"Imageflow\"/></plugins><watermarks><image name=\"sun\" path=\"" + watermarkImage + "\" top=\"0\" left=\"0\" imageQuery=\"s.alpha=0.5\"/></watermarks></resizer>"));
            c.Plugins.LoadPlugins();
            var s = c.GetDiagnosticsPage();
            Debug.Assert(c.Plugins.AllPlugins.Any((p) => p.ToString().Contains("Imageflow")));

          
                
            c.BuildImage(inputImage, outputImage,
                "rotate=90&width=600&format=png&watermark=sun");

            Console.WriteLine("Imageflow plugin loaded and image processed successfully.");
      
        }
    }
}