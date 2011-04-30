using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace ImageResizer.ReleaseBuilder {
    class Program {
        static void Main(string[] args) {
            Console.WindowHeight = Console.LargestWindowHeight / 2;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Console.SetBufferSize(Console.WindowWidth, 5000);

            string lastValue = "Resizer3-alpha-3";
            Console.WriteLine("Please enter the friendly archive base name (like Resizer3-alpha-3):");
            Console.Write("(" + lastValue + ")");
            string s = Console.ReadLine();

            if (string.IsNullOrEmpty(s.Trim())) s = lastValue; //use default

            Build b = new Build(s.Trim());
            b.Run();

            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }
    }
}
