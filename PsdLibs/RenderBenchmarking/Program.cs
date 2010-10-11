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
            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < 50; i++)
            {
                Debug.Write("Loop " + i);
                sw.Reset();
                sw.Start();
                PsdProvider.getStream("~/1001.psd", new NameValueCollection());
                sw.Stop();
                Console.WriteLine("Loop " + i + " took " + sw.ElapsedMilliseconds + "ms");
            }
        }
    }
}
