using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace ImageResizer.ReleaseBuilder {
    class Program {
        [STAThread]
        static void Main(string[] args) {

            Build b = new Build();
            b.Run();

            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }
    }
}
