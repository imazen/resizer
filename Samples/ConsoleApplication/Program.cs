using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins.PrettyGifs;
using ImageResizer;
using System.Diagnostics;
using ImageResizer.Plugins.FreeImageBuilder;
using ImageResizer.Util;
using System.IO;
using ImageResizer.Plugins.FreeImageDecoder;
using System.Drawing;
using ImageResizer.Plugins.FreeImageEncoder;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Watermark;

namespace ConsoleApplication {
    class Program {

        public static string imageDir = "..\\..\\Samples\\Images\\";
        static void Main(string[] args) {
            Config c = new Config();
            new PrettyGifs().Install(c);
    

            string s = c.GetDiagnosticsPage();
            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128&watermark=Sun_256.png");


            Console.ReadKey();
        }


    }
}
