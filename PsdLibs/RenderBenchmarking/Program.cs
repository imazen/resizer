using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PsdRenderer;
using System.Diagnostics;
using System.Collections.Specialized;

namespace RenderBenchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            NameValueCollection qs = new NameValueCollection();
            qs.Add("renderer", "GraphicsMill");
            long total = 0;
            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < 50; i++)
            {
                Debug.Write("Loop " + i);
                sw.Reset();
                sw.Start();
                PsdProvider.getStream("~/1001.psd", qs);
                sw.Stop();
                total += sw.ElapsedMilliseconds;
                Console.WriteLine("Loop " + i + " took " + sw.ElapsedMilliseconds + "ms");
            }
            Console.WriteLine("Average time is " + total/50 + "ms");
            Console.ReadKey();
        }
    }
}
