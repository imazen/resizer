using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace BitmapConstructorSpeed {
    class Program {
        static void Main(string[] args) {
            Stopwatch s = new Stopwatch();
            string path = "..\\..\\..\\..\\Samples\\Images\\quality-original.jpg";
            path = Path.GetFullPath(path);
            int count =100;
            //prep ntfs
            using(Bitmap b = new Bitmap(new Bitmap(path),new Size(20,20))){};

            s.Start();
            for (int i = 0; i < count; i++) {
                using (Bitmap a = new Bitmap(path)) {
                    a.RotateFlip(RotateFlipType.Rotate180FlipXY);
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the constructor took " + s.ElapsedMilliseconds);
            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Bitmap a = new Bitmap(stream)) {
                        a.RotateFlip(RotateFlipType.Rotate180FlipXY);
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the a stream took " + s.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}
