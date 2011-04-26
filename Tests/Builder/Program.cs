using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageResizer.ReleaseBuilder {
    class Program {
        static void Main(string[] args) {
            Console.WindowHeight = Console.LargestWindowHeight / 2;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Console.SetBufferSize(Console.WindowWidth, 5000);

            Console.WriteLine("Please enter the friendly archive base name (like Resizer3-alpha-3):");
            string s = Console.ReadLine();


            Build b = new Build(s.Trim());
            b.Run();

            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }
    }
}
