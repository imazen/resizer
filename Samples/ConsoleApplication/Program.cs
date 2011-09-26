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

namespace ConsoleApplication {
    class Program {

        public static string imageDir = "..\\..\\Samples\\Images\\";// "..\\..\\..\\..\\Samples\\Images\\";
        static void Main(string[] args) {
            Config c = new Config();
            new PrettyGifs().Install(c);

            ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
            w.align = System.Drawing.ContentAlignment.MiddleCenter;
            w.hideIfTooSmall = false;
            w.keepAspectRatio = true;
            w.valuesPercentages = true;
            w.watermarkDir = imageDir; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
            w.bottomRightPadding = new System.Drawing.SizeF(1, 1);
            w.topLeftPadding = new System.Drawing.SizeF(1, 1);
            w.watermarkSize = new System.Drawing.SizeF(1, 1); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
            //Install the plugin
            w.Install(c);



            string s = c.GetDiagnosticsPage();
            c.BuildImage(imageDir + "quality-original.jpg", "grass.gif", "rotate=3&width=600&format=gif&colors=128&watermark=Sun_256.png");

            CompareFreeImageEncoderToDefault();
            CompareFreeImageDecoderToDefault();

            CompareFreeImageToDefault();
            Console.ReadKey();
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


        public static void BenchmarkInMemory(Config c, string source,  ResizeSettings settings) {
            MemoryStream ms;
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read)){
                ms =StreamUtils.CopyStream(fs);
            }
            
            int loops = 20;
            Stopwatch s = new Stopwatch();
            s.Start();
            c.CurrentImageBuilder.Build(ms, settings,false).Dispose();
            s.Stop();
            Console.WriteLine();
            Console.WriteLine("First iteration: " + s.ElapsedMilliseconds.ToString() + "ms");
            s.Reset();
            s.Start();
            for (int i = 0; i < loops; i++) {
                ms.Seek(0, SeekOrigin.Begin);
                c.CurrentImageBuilder.Build(ms, settings,false).Dispose();
            }
            s.Stop();
            Console.WriteLine("Avg. of next " + loops + " iterations: " + (s.ElapsedMilliseconds / loops).ToString() + "ms");
            Console.WriteLine();
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
