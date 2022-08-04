// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Linq;
using ImageResizer.Configuration;
using ImageResizer;
using System.Diagnostics;

namespace ConsoleApplication {
    class Program {

        public static string imageDir = "..\\..\\Samples\\Images\\";
        static void Main(string[] args) {
            Config c = new Config(); //new Config(new ResizerSection("<resizer><plugins><add name=\"PrettyGifs\"/></plugins></resizer>"));
            c.Plugins.LoadPlugins();
            string s = c.GetDiagnosticsPage();
            Debug.Assert(c.Plugins.AllPlugins.Any((p) => p.ToString().EndsWith("PrettyGifs")));
            
            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128&watermark=Sun_256.png");


            Console.ReadKey();
        }


    }
}
