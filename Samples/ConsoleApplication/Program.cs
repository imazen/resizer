// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.Linq;
using ImageResizer;
using ImageResizer.Configuration;

namespace ConsoleApplication
{
    internal class Program
    {
        public static string imageDir = "..\\..\\Samples\\Images\\";

        private static void Main(string[] args)
        {
            var c = new Config(new ResizerSection("<resizer><plugins><add name=\"Imageflow\"/></plugins></resizer>"));
            c.Plugins.LoadPlugins();
            var s = c.GetDiagnosticsPage();
            Debug.Assert(c.Plugins.AllPlugins.Any((p) => p.ToString().Contains("Imageflow")));

            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif",
                "rotate=90&width=600&format=gif&watermark=Sun_256.png");


            Console.ReadKey();
        }
    }
}