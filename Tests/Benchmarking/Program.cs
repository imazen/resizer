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

namespace Bench {
    class Program {

        public static string imageDir = "..\\..\\Samples\\Images\\";// "..\\..\\..\\..\\Samples\\Images\\";
        static void Main(string[] args) {
            Config c = new Config();
            new PrettyGifs().Install(c);
            WatermarkPlugin w = (WatermarkPlugin)new WatermarkPlugin().Install(c);
            w.OtherImages.Path = imageDir;
           


            string s = c.GetDiagnosticsPage();
            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128&watermark=Sun_256.png");

            EvaluateSpeedPlugin();

            CompareFreeImageEncoderToDefault();
            CompareFreeImageDecoderToDefault();

            CompareFreeImageToDefault();
            Console.ReadKey();
        }


        public static void EvaluateSpeedPlugin() {
            
            string[] images = new string[] { "2758U_02.jpg","2758U_02.tif","27520_96.jpg","27520_96.tif","27586_33.jpg","27586_33.tif" };
            for (int i = 0; i < images.Length; i++)
                images[i] = imageDir + "private\\sierra\\" + images[i];

            Config c = new Config();
            new SpeedOrQuality().Install(c);

            Console.WriteLine();
            Console.WriteLine("Evaluating benefits of SpeedOrQuality plugin at various settings");
            foreach (string s in images) {
                Console.WriteLine("In-memory resize of " + ImageInfo(s));
                for (int rez = 100; rez < 800; rez += 200) {
                    
                    for (int i = 0; i < 4; i++) {
                        
                        ResizeSettings set = new ResizeSettings();
                        set.MaxWidth = rez;
                        set.MaxHeight = rez;
                        set["speed"] = i.ToString();
                        Console.WriteLine("At speed=" + i + ", " + rez + "x" + rez + ", avg resize time=" + BenchmarkInMemory(c, s, set, false, true, true, 10) + ", avg total=" + BenchmarkInMemory(c, s, set, false, false, false, 5));

                    }
                    Console.WriteLine();
                }
            }

       
        }

        public static string ImageInfo(string file) {
            using (Bitmap b = new Bitmap(file)) {
                return file + " (" + b.Width + "x" + b.Height + ") (" + b.PixelFormat.ToString() + ")";
            }
        }

        public static void CompareFreeImageToDefault(){
            string jpeg = imageDir + "quality-original.jpg";
            string dest = "dest.jpg";

            Config c = new Config();
            new FreeImageBuilderPlugin().Install(c);

            Console.WriteLine();
            Console.WriteLine("Running in-memory test");
            Console.WriteLine("Testing default pipeline");
            BenchmarkInMemory(c, jpeg, new ResizeSettings("maxwidth=200&maxheight=200"));
            Console.WriteLine("Testing FreeImage pipeline");
            BenchmarkInMemory(c, jpeg, new ResizeSettings("maxwidth=200&maxheight=200&freeimage=true"));

            Console.WriteLine("Running filesystem test");
            Console.WriteLine("Testing default pipeline");
            BenchmarkFileToFile(c, jpeg, dest, new ResizeSettings("maxwidth=200&maxheight=200"));
            Console.WriteLine("Testing FreeImage pipeline");
            BenchmarkFileToFile(c, jpeg, dest, new ResizeSettings("maxwidth=200&maxheight=200&freeimage=true"));

       }



        public static void CompareFreeImageDecoderToDefault() {
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };

            Config c = new Config();
            new FreeImageDecoderPlugin().Install(c);

            Console.WriteLine();
            foreach(string s in images){
                Console.WriteLine("Comparing FreeImage and standard decoders for " + s);
                Console.WriteLine("Testing default pipeline");
                BenchmarkDecoderInMemory(c, s, new ResizeSettings());
                Console.WriteLine("Testing FreeImage pipeline");
                BenchmarkDecoderInMemory(c, s, new ResizeSettings());
            }
            Console.WriteLine();
        }

        public static void CompareFreeImageEncoderToDefault() {
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };

            Config c = new Config();
            new FreeImageEncoderPlugin().Install(c);

            ResizeSettings settings = new ResizeSettings("format=jpg");
            Console.WriteLine();
            foreach (string s in images) {
                Console.WriteLine("Comparing FreeImage and standard encoders for " + s);
                Console.WriteLine("Testing default pipeline");
                BenchmarkEncoderInMemory(c, c.CurrentImageBuilder.LoadImage(s, settings), settings);
                Console.WriteLine("Testing FreeImage pipeline");
                BenchmarkEncoderInMemory(c, c.CurrentImageBuilder.LoadImage(s, settings), settings);
            }
            Console.WriteLine();
        }



        /// <summary>
        /// This is inherently flawed - the unpredictability and inconsistency of disk and NTFS performance makes these results difficult to read.
        /// </summary>
        public static void BenchmarkFileToFile(Config c, string source, string dest, ResizeSettings settings) {
            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.Build(source, dest, settings);
            s.Stop();
            Console.WriteLine();
            Console.WriteLine("First iteration: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                c.CurrentImageBuilder.Build(source, dest, settings);
            }
            s.Stop();
            Console.WriteLine("Avg. of next " + loops + " iterations: " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
            Console.WriteLine();
        }

        public static long BenchmarkInMemory(Config c, string source, ResizeSettings settings) {
            return BenchmarkInMemory(c, source, settings, true);
        }
        public static long BenchmarkInMemory(Config c, string source,  ResizeSettings settings, bool outputResults, bool excludeDecoding = false, bool excludeEncoding = true, int loops = 20) {
            MemoryStream ms;
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read)){
                ms =StreamUtils.CopyStream(fs);
            }
            MemoryStream dest = new MemoryStream(4096);
            ms.Seek(0, SeekOrigin.Begin);
            object src = excludeDecoding ? (object)c.CurrentImageBuilder.LoadImage(ms, settings) : (object)ms;
            Stopwatch s = new Stopwatch();
            s.Start();
            ms.Seek(0, SeekOrigin.Begin);
            c.CurrentImageBuilder.Build(ms, dest, settings, false);
            dest.Seek(0, SeekOrigin.Begin);
            dest.SetLength(0);
            s.Stop();
            if (outputResults) Console.WriteLine();
            if (outputResults) Console.WriteLine("First iteration: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                if (excludeEncoding)
                    c.CurrentImageBuilder.Build(src, settings, false).Dispose();
                else
                    c.CurrentImageBuilder.Build(src, dest, settings, false);
            }
            s.Stop();
            if (outputResults) Console.WriteLine("Avg. of next " + loops + " iterations: " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
            if (outputResults) Console.WriteLine();
            return (s.ElapsedMilliseconds / loops);
        }

        public static void BenchmarkDecoderInMemory(Config c, string source, ResizeSettings settings) {
            MemoryStream ms;
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read)) {
                ms = StreamUtils.CopyStream(fs);
            }

            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.LoadImage(ms,settings).Dispose();
            s.Stop();
            Console.WriteLine("First iteration: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                c.CurrentImageBuilder.LoadImage(ms, settings).Dispose();
            }
            s.Stop();
            Console.WriteLine("Avg. of next " + loops + " iterations: " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
        }

        public static void BenchmarkEncoderInMemory(Config c, Image img, ResizeSettings settings) {
            MemoryStream ms = new MemoryStream();

            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.EncoderProvider.GetEncoder(settings, img).Write(img, ms);
            s.Stop();
            Console.WriteLine("First iteration: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                ms.SetLength(0);
                c.CurrentImageBuilder.EncoderProvider.GetEncoder(settings, img).Write(img, ms);
            }
            s.Stop();
            Console.WriteLine("Avg. of next " + loops + " iterations: " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
        }
    }
}
