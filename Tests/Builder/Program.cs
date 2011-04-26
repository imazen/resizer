using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageResizer.ReleaseBuilder {
    class Program {
        static void Main(string[] args) {
            Build b = new Build();

            b.RemoveUselessFiles();
        }
    }
}
