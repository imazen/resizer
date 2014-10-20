using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using System.IO;
using ImageResizer;

namespace PerfTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This portion tests the raw speed of resizing and re-encoding an image. ");
            Console.WriteLine("It elminates factors such as slow hard drives, network connections, and browsers problems.");
            Console.WriteLine("Real-world performance, however, may actually be better, since there are several layers of caching, " +
                "all of which completely eliminate the step we are testing here.");
            Console.WriteLine();

            Console.WriteLine("True testing requires running a stress testing tool like MACT."); 

            using (Bitmap b1 = new Bitmap("../../../../Samples/Images/grass.jpg")){
                TestWith(b1);
            }
            using (Bitmap b2 = new Bitmap("../../../../Samples/Images/quality-original.jpg"))
            {
                TestWith(b2);
            }
            Console.ReadKey();

            Console.WriteLine("Press any key to continue");
        }

        static void TestWith(Bitmap b)
        {
            Console.WriteLine();
            Console.WriteLine("==========");
            Console.WriteLine("Testing with image (" + b.Width + "x" + b.Height + "), " +
                        Math.Round((double)(b.Width * b.Height * 4) / 1000).ToString() + "KB (in memory)");
            Console.WriteLine();
            Console.WriteLine();
            TestQuery("?maxwidth=100&maxheight=100",b,100);

            TestQuery("?width=100&height=100", b, 100);

            TestQuery("?width=100&height=100&crop=auto", b, 100);


            TestQuery("?width=100&format=png", b, 100);

            TestQuery("?width=100&format=gif", b, 100);

            TestQuery("?width=500", b, 100);
            TestQuery("?width=800", b, 100);

        }

        static void TestQuery(string query, Bitmap b, int times)
        {
            
            MemoryStream ms = new MemoryStream(1000000); //1MB memory stream, to allow us to test the speed of the image compression.

            Console.WriteLine("Running " + query + " " + times + " times");
            Stopwatch all = new Stopwatch();
            Stopwatch resizing = new Stopwatch();
            Stopwatch saving = new Stopwatch();
            all.Start();
            for (int i = 0; i < times; i++)
            {
                resizing.Start();
                //Resize the image.
                using (Bitmap result = ImageResizer.ImageBuilder.Current.Build(b,new ResizeSettings(query),false))
                {
                    resizing.Stop();
                    saving.Start();
                    //Reset the virtual file
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.SetLength(0);
                    //Compress the image
                    ImageResizer.ImageBuilder.Current.EncoderProvider.GetEncoder(new ResizeSettings(query),b).Write(result,ms);
                    saving.Stop();
                }
                
            }
            all.Stop();
            Console.WriteLine("Total time: " + all.ElapsedMilliseconds + "ms.\n" + Math.Round(100 * (double)resizing.ElapsedMilliseconds / (double)all.ElapsedMilliseconds, 2) + "% spent on resizing. \n" +
                Math.Round(100 * (double)saving.ElapsedMilliseconds / (double)all.ElapsedMilliseconds, 2) + "% spent on re-encoding the image in memory.\n" +
                Math.Round(100 * ((double)all.ElapsedMilliseconds - (double)saving.ElapsedMilliseconds - 
                (double)resizing.ElapsedMilliseconds) / (double)all.ElapsedMilliseconds, 2) + "% .NET overhead (GAC, timing code, etc).");

            double avg = Math.Round((double)all.ElapsedMilliseconds / (double)times, 3);
            Console.WriteLine("Average processing time per image: " +avg + " milliseconds.");
            Console.WriteLine("Average processing time per image with caching:" + Math.Round(avg / times, 8) + " ms");
            Console.WriteLine();
        }
    }


}
