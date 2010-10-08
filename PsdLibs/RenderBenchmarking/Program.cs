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
            for (int i = 0; i < 50; i++)
            {
                Debug.Write("Loop " + i);
                PsdProvider.getStream("~/1001.psd", new NameValueCollection());
            }
        }
    }
}
