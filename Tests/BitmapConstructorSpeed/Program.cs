using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using ImageResizer.Configuration;
using ImageResizer.Plugins.CrazyFast;
using ImageResizer;

namespace BitmapConstructorSpeed {
    class Program {
        static void Main(string[] args) {
           
            Stopwatch s = new Stopwatch();
            string path = "..\\..\\..\\..\\Samples\\Images\\quality-original.jpg";
            path = Path.GetFullPath(path);
            int count =100;
            //prep ntfs
            using(Bitmap b = new Bitmap(new Bitmap(path),new Size(20,20))){};

            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Image a = Image.FromStream(stream, true, false) as Bitmap) {
                        using(new Bitmap(a)){}
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the a FromStream took " + s.ElapsedMilliseconds);

            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Image a = FastBitmapLoader(stream,true)) {
                        using (new Bitmap(a)) { }
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the FastBitmapLoader took " + s.ElapsedMilliseconds);



            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Image a = ImageBuilder.Current.LoadImage(stream, new ResizeSettings())) {
                        using (new Bitmap(a)) { }
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the LoadImage took " + s.ElapsedMilliseconds);




            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                ImageBuilder.Current.Build(path, new ResizeSettings("width=100"));
            }
            s.Stop();
            Console.WriteLine(count + " iterations using Build(width=100) took " + s.ElapsedMilliseconds);


            new CrazyFastPlugin().Install(Config.Current);

            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Image a = ImageBuilder.Current.LoadImage(stream, new ResizeSettings())) {
                        using (new Bitmap(a)) { }
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the LoadImage with CrazyFastPlugin took " + s.ElapsedMilliseconds);



            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                ImageBuilder.Current.Build(path, new ResizeSettings("width=100"));
            }
            s.Stop();
            Console.WriteLine(count + " iterations using Build(width=100) with CrazyFastPlugin took " + s.ElapsedMilliseconds);





            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Bitmap a = new Bitmap(path)) {
                    using (new Bitmap(a)) { }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the constructor took " + s.ElapsedMilliseconds);
            s.Reset();
            s.Start();
            for (int i = 0; i < count; i++) {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Bitmap a = new Bitmap(stream)) {
                        using (new Bitmap(a)) { }
                    }
                }
            }
            s.Stop();
            Console.WriteLine(count + " iterations using the stream constructor took " + s.ElapsedMilliseconds);

            Console.ReadKey();
        }


        public static Bitmap FastBitmapLoader(Stream s, bool useIcm) {
            IntPtr zero = IntPtr.Zero;
            int num = 0;
            if (useIcm) {
                Assembly drawing = Assembly.GetAssembly(typeof(Bitmap));
                MethodInfo mi =
                    drawing.GetType("System.Drawing.SafeNativeMethods+Gdip").GetMethod("GdipCreateBitmapFromStreamICM", BindingFlags.Static | BindingFlags.NonPublic);

                object gpstream = drawing.GetType("System.Drawing.Internal.GPStream").GetConstructors
            (BindingFlags.Instance | BindingFlags.NonPublic)[0].Invoke(new object[] { s });

                object[] args = new object[] { gpstream, zero };
                num = (int)mi.Invoke(null, args);
                zero = (IntPtr)args[1];
            } else {
                //num = SafeNativeMethods.Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out zero);
            }
            if (num != 0) {
                //throw SafeNativeMethods.Gdip.StatusException(num);
            }

            MethodInfo fromGDI = typeof(Bitmap).GetMethod("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static);


            return fromGDI.Invoke(null, new object[] { zero }) as Bitmap;
        }

    }
}
