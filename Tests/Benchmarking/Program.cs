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
using ImageResizer.Encoding;
using ImageResizer.ExtensionMethods;

namespace Bench {
    class Program {

        public static string imageDir = "..\\..\\Samples\\Images\\";// "..\\..\\..\\..\\Samples\\Images\\";
        static void Main(string[] args) {
            Config c = new Config();
            new PrettyGifs().Install(c);
            WatermarkPlugin w = (WatermarkPlugin)new WatermarkPlugin().Install(c);
            w.OtherImages.Path = imageDir;

            Console.WindowWidth = 200;
            

            //TODO: make watermark system work outside of the web folder.
            string s = c.GetDiagnosticsPage();
            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128&watermark=Sun_256.png");

            CompareFreeImageDecoderToDefault();

            CompareFreeImageEncoderToDefault();
           

            CompareFreeImageToDefault();

            EvaluateSpeedPlugin();
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
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "fountain-small.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };

            string dest = "dest.jpg";

            Config c = new Config();
            Config f = new Config();
            new FreeImageBuilderPlugin().Install(f);

            Config h = new Config();
            new FreeImageDecoderPlugin().Install(h);
            new FreeImageEncoderPlugin().Install(h);

            Console.WriteLine();

            Dictionary<string, Config> pipelines = new Dictionary<string, Config>();
            pipelines.Add("Default", c);
            pipelines.Add("FreeImageBuilder", f);
            pipelines.Add("Hybrid", h);

            ResizeSettings[] queries = new ResizeSettings[] { new ResizeSettings("maxwidth=200&maxheight=200&decoder=freeimage&builder=freeimage"), new ResizeSettings("&decoder=freeimage&builder=freeimage") };

            foreach (string file in images) {
                foreach (ResizeSettings s in queries) {
                    Console.WriteLine();
                    Console.WriteLine("Running in-memory test with " + file + s.ToString());
                    foreach (string pipe in pipelines.Keys) {
                        Console.Write(pipe.PadRight(25) + ": ");
                        BenchmarkInMemory(pipelines[pipe],file, s);
                    }
                }
            }

            foreach (string file in images) {
                foreach (ResizeSettings s in queries) {
                    Console.WriteLine();
                    Console.WriteLine("Running filesystem (flawed) test with " + file + s.ToString());
                    foreach (string pipe in pipelines.Keys) {
                        Console.Write(pipe.PadRight(25) + ": ");
                        BenchmarkFileToFile(pipelines[pipe], file,dest, s);
                    }
                }
            }


       }



        public static void CompareFreeImageDecoderToDefault() {
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "fountain-small.jpg", 
                imageDir + "\\extra\\cherry-blossoms.jpg",
                imageDir + "\\extra\\mountain.jpg",
                 imageDir + "\\extra\\dewdrops.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };


            Config c = new Config();
            new FreeImageDecoderPlugin().Install(c);

            Console.WriteLine();
            foreach(string s in images){
                Console.WriteLine("Comparing FreeImage and standard decoders for " + s);
                Console.Write("Default: ".PadRight(25));
                BenchmarkDecoderInMemory(c, s, new ResizeSettings());
                Console.Write("FreeImage: ".PadRight(25));
                BenchmarkDecoderInMemory(c, s, new ResizeSettings("&decoder=freeimage"));
            }
            Console.WriteLine();
        }

        public static void CompareFreeImageEncoderToDefault() {
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "fountain-small.jpg", 
                imageDir + "\\extra\\cherry-blossoms.jpg",
                imageDir + "\\extra\\mountain.jpg",
                 imageDir + "\\extra\\dewdrops.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };


            Config c = new Config();
            Config p = new Config();
            new PrettyGifs().Install(p);
            Config f = new Config();
            new FreeImageEncoderPlugin().Install(f);

            ResizeSettings[] queries = new ResizeSettings[] { new ResizeSettings("format=jpg"), new ResizeSettings("format=png"), new ResizeSettings("format=gif") };


            Console.WriteLine();
            foreach (string s in images) {
                foreach (ResizeSettings query in queries) {
                    Console.WriteLine("Comparing FreeImage and standard encoders for " + s + query.ToString());
                    Console.Write("Default: ".PadRight(25));
                    BenchmarkEncoderInMemory(c, c.CurrentImageBuilder.LoadImage(s, query), query);
                    Console.Write("PrettyGifs: ".PadRight(25));
                    BenchmarkEncoderInMemory(p, p.CurrentImageBuilder.LoadImage(s, query), query);
                    Console.Write("FreeImage: ".PadRight(25));
                    BenchmarkEncoderInMemory(f, f.CurrentImageBuilder.LoadImage(s, query), query);
                }
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
            Console.Write("First: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                c.CurrentImageBuilder.Build(source, dest, settings);
            }
            s.Stop();
            Console.Write(" Avg(" + loops + "):" + (s.ElapsedMilliseconds / loops).ToString() + "ms");
            Console.WriteLine();
        }

        public static long BenchmarkInMemory(Config c, string source, ResizeSettings settings) {
            return BenchmarkInMemory(c, source, settings, true);
        }
        public static long BenchmarkInMemory(Config c, string source,  ResizeSettings settings, bool outputResults, bool excludeDecoding = false, bool excludeEncoding = true, int loops = 20) {
            MemoryStream ms;
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read)){
                ms = StreamExtensions.CopyToMemoryStream(fs);
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
            if (outputResults) Console.Write("First:" + s.ElapsedMilliseconds.ToString() + "ms");
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
            if (outputResults) Console.Write(" Avg(" + loops + "):" + (s.ElapsedMilliseconds / loops).ToString() + "ms");
            if (outputResults) Console.WriteLine();
            return (s.ElapsedMilliseconds / loops);
        }

        public static void BenchmarkDecoderInMemory(Config c, string source, ResizeSettings settings) {
            MemoryStream ms;
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read)) {
                ms = StreamExtensions.CopyToMemoryStream(fs);
            }

            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.LoadImage(ms,settings).Dispose();
            s.Stop();
            Console.Write("First: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                c.CurrentImageBuilder.LoadImage(ms, settings).Dispose();
            }
            s.Stop();
            Console.WriteLine(" Avg(" + loops + "): " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
        }

        public static void BenchmarkEncoderInMemory(Config c, Image img, ResizeSettings settings) {
            MemoryStream ms = new MemoryStream();

            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.EncoderProvider.GetEncoder(settings, img).Write(img, ms);
            s.Stop();
            Console.Write("First: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                ms.SetLength(0);
                IEncoder ie = c.CurrentImageBuilder.EncoderProvider.GetEncoder(settings, img);
                ie.Write(img, ms);
            }
            s.Stop();
            Console.WriteLine("Avg(" + loops + "): " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
        }
    }
}
