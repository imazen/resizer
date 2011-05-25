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
            new PrettyGifs().Install(c);
            c.BuildImage("..\\Images\\quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128");

        }
    }
}
