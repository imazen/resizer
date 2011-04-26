using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageResizer.ReleaseBuilder {
    class Program {
        static void Main(string[] args) {
            Console.WindowHeight = Console.LargestWindowHeight / 2;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Console.SetBufferSize(Console.WindowWidth, 16000);
            Build b = new Build();
            //b.BuildAll();
            b.RemoveUselessFiles();


            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }
    }
}
