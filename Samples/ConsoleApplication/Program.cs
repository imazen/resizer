using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins.PrettyGifs;

namespace ConsoleApplication {
    class Program {
        static void Main(string[] args) {
            Config c = new Config();
            c.Plugins.Get<ImageResizer.Plugins.Basic.SizeLimiting>().Uninstall(c);
            new PrettyGifs().Install(c);
            string s = c.GetDiagnosticsPage();
            c.BuildImage("..\\..\\Samples\\Images\\quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128");

        }
    }
}
