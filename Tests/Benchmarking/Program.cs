using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer;
using System.Diagnostics;
using ImageResizer.Util;
using System.IO;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using ImageResizer.Encoding;
using ImageResizer.ExtensionMethods;
using System.Drawing.Imaging;
using ImageResizer.Resizing;
using System.Collections.Specialized;
using System.Threading;
using Bench.Profiling;
using Bench.Benchmarking;
using Bench.ImageResizerProfiling;

namespace Bench
{
    class Program
    {

        public static string imageDir = "..\\..\\Samples\\Images\\";// "..\\..\\..\\..\\Samples\\Images\\";

        public static int ConsoleWidth = 200;
        static void Main(string[] args)
        {
            Config c = new Config();

            Console.WindowWidth = ConsoleWidth;



            //parallelism test (thread count sequence)
            //Benchmark relative to costly baseline operation
            //Upload results to S3?
            //compare N actions
            //source data 
            //action buffered? action profiled?
            //IO read qty?

            CompareFreeImageToDefault();

            Console.ReadKey();
        }

        public BenchmarkRunner<int, JobProfiler> EstablishBaseline()
        {
            var r = new Bench.Benchmarking.BenchmarkRunner<int, JobProfiler>();
            r.Operation = new Action<JobProfiler, int>((p, _) => new CostlyBaseline().DoWork());
            r.ParallelThreads = 8;
            r.SequentialRuns = 4;
            r.ParallelRuns = 2;
            return r;
        }



   


        public static void EvaluateSpeedPlugin()
        {
            var settings = new BenchmarkingSettings();
            settings.Images = new ImageProvider().AddLocalImages(imageDir, "quality-original.jpg", "fountain-small.jpg", "sample.tif", "private\\98613_17.tif");
            
            var sizes = new List<Instructions>();
            for (int rez = 100; rez < 800; rez += 200){
                sizes.Add(new Instructions(){Width = rez});
            }
            settings.SharedInstructions = sizes;

            var configs = new List<Tuple<Config, Instructions, string>>();
            configs.Add(new Tuple<Config,Instructions,string>(ConfigWithPlugins(),null,"Default"));

            for (int i = 1; i < 4; i++)
            {
                configs.Add(new Tuple<Config, Instructions, string>(ConfigWithPlugins("SpeedOrQuality"), 
                    new Instructions("speed=" + i.ToString()), "SpeedOrQuality plugin"));

            }

            settings.ExcludeDecoding = true;
            settings.ExcludeEncoding = true;
            settings.ExcludeIO = true;

            Compare(settings, configs);
        }

        public class BenchmarkingSettings
        {
            public BenchmarkingSettings()
            {
                ThrowawayRuns = 1;
                SequentialRuns = 8;
                ParallelRuns = 2;
                ParallelThreads = 8;
                ExcludeDecoding = false;
                ExcludeEncoding = true;
                ExcludeBuilding = false;
                ExcludeIO = true;
                ShowProfileTree = true;
            }
            public int ThrowawayRuns { get; set; }
            public int ThrowawayThreads { get; set; }

            public int SequentialRuns { get; set; }
            public int ParallelRuns { get; set; }
            public int ParallelThreads { get; set; }

            public ImageProvider Images { get; set; }

            public IEnumerable<Instructions> SharedInstructions { get; set; }

            public bool ExcludeDecoding { get; set; }
            public bool ExcludeBuilding { get; set; }
            public bool ExcludeEncoding { get; set; }

            public bool ExcludeIO { get; set; }

            public bool ShowProfileTree { get; set; }
        }

        public static void Compare(BenchmarkingSettings settings, IEnumerable<Tuple<string, Instructions>> plugins){
            Compare(settings, plugins.Select(t =>
                {
                    return new Tuple<Config, Instructions, string>(ConfigWithPlugins(t.Item1), t.Item2, t.Item1);
                }));
        }
        public static void Compare(BenchmarkingSettings settings,
            IEnumerable<Tuple<Config, Instructions, string>> configsAndLabels)
        {
            foreach (var pair in settings.Images.GetImagesAndDescriptions())
            {
                Console.WriteLine();
                Console.WriteLine("Using {0} runs, {1} parallel runs on {2} threads. Data: {3}", settings.SequentialRuns,settings.ParallelRuns,settings.ParallelThreads,pair.Item2);

                var widths = CalcColumnWidths(ConsoleWidth, 4, -2, -2, -2, -4);

                Console.WriteLine(Distribute(widths, "Config", "Sequential", "Parallel", "Percent concurrent", "Instructions"));
                foreach (var instructions in settings.SharedInstructions)
                {
                    foreach (var triple in configsAndLabels)
                    {
                        var combined = triple.Item2 == null ? instructions : new Instructions(triple.Item2.MergeDefaults(instructions));
                        var runner =Benchmark.BenchmarkInMemory(triple.Item1,pair.Item1,combined,settings.ExcludeDecoding,settings.ExcludeEncoding);
                        runner.ParallelRuns = settings.ParallelRuns;
                        runner.ParallelThreads = settings.ParallelThreads;
                        runner.SequentialRuns = settings.SequentialRuns;
                        runner.Label = triple.Item3;
                        runner.ThrowawayThreads = settings.ThrowawayThreads;
                        runner.ThrowawayRuns = settings.ThrowawayRuns;

                        var results = runner.Benchmark();

                        Action<SegmentStats> printStats = (s) =>
                        {
                            var statStrs = new List<string>(GetStats(triple.Item3, s));
                            statStrs.Add(combined.ToString());
                            Console.WriteLine(Distribute(widths, statStrs.ToArray()));
                        };

                        printStats(results.StatsForSegment());

                        if (settings.ShowProfileTree)
                        {
                            var f = new ConcurrencyResultFormatter();
                            Console.WriteLine(f.PrintCallTree(results.FindSet()));
                        }
                        
                    }
                }
            }
        }

        public static string[] GetStats(string label, SegmentStats stats){
            return new string[]{
                label + " - " + stats.SegmentName,
                stats.SequentialMs.ToString(), stats.ParallelMs.ToString(),
                stats.ParallelConcurrencyPercent.ToString("F") + "% t=" + stats.ParallelThreads.ToString()
            };
        }

        public static int[] CalcColumnWidths(int space, params double[] weights)
        {
            var totalWeight = weights.Sum(d => Math.Abs(d));
            return weights.Select(w => (int)(w / totalWeight * space)).ToArray();
        }
        public static string Distribute( int[] colWidths, params string[] values){
            if (colWidths.Count() != values.Count()) throw new ArgumentOutOfRangeException("values", "values and colWidths must have the same number of elements");
            return string.Join("", values.Select((s, ix) => colWidths[ix] > 0 ? s.PadRight(colWidths[ix]) : s.PadLeft(-colWidths[ix])));
        }
        

        public static Config ConfigWithPlugins(params string[] pluginNames)
        {
            var c = new Config();
            foreach (var n in pluginNames)
                if (c.Plugins.AddPluginByName(n, AutoLoadNative()) == null)
                    throw new ArgumentOutOfRangeException("Failed to load plugin " +n);
            return c;
        }

        public static void CompareFreeImageToDefault()
        {
            var settings = new BenchmarkingSettings();
            settings.Images = new ImageProvider().AddLocalImages(imageDir, "quality-original.jpg", "fountain-small.jpg", "sample.tif", "private\\98613_17.tif");
            settings.SharedInstructions = new Instructions[]{new Instructions(
                "maxwidth=200&maxheight=200"), 
                new Instructions()};

          
            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins(),null,"Default"),
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins("FreeImageBuilder"),new Instructions("builder=freeimage"),"FreeImageBuilder"),
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins("FreeImageDecoder","FreeImageEncoder"),new Instructions("decoder=freeimage&encoder=freeimage"),"Hybrid (FreeImage encoder/decoder, GDI+ resizer)")};
            Compare(settings,configs);

            settings.ExcludeIO = true;

            Compare(settings, configs);

        }



        private static NameValueCollection AutoLoadNative()
        {
            var n = new NameValueCollection();
            n["downloadNativeDependencies"] = "true";
            return n;
        }
        public static void CompareFreeImageEncoderToDefault()
        {
            string[] images = new string[] { imageDir + "quality-original.jpg", imageDir + "fountain-small.jpg", 
                imageDir + "\\extra\\cherry-blossoms.jpg",
                imageDir + "\\extra\\mountain.jpg",
                 imageDir + "\\extra\\dewdrops.jpg", imageDir + "sample.tif", imageDir + "\\private\\98613_17.tif" };


            Config c = new Config();
            Config p = new Config();
            if (p.Plugins.AddPluginByName("PrettyGifs", AutoLoadNative()) == null)
                throw new ArgumentOutOfRangeException("Failed to load PrettyGifs plugin");

   
            Config f = new Config();
            if (f.Plugins.AddPluginByName("FreeImageEncoder", AutoLoadNative()) == null)
                throw new ArgumentOutOfRangeException("Failed to load FreeImageEncoder plugin");


            Instructions[] queries = new Instructions[] { new Instructions("format=jpg"), new Instructions("format=png"), new Instructions("format=gif") };


            Console.WriteLine();
            foreach (string s in images)
            {
                foreach (var query in queries)
                {
                    Console.WriteLine("Comparing FreeImage and standard encoders for " + s + query.ToString());
                    Console.Write("Default: ".PadRight(25));
                    Benchmark.BenchmarkEncoderInMemory(c, s, query);
                    Console.Write("PrettyGifs: ".PadRight(25));
                    Benchmark.BenchmarkEncoderInMemory(p, s, query);
                    Console.Write("FreeImage: ".PadRight(25));
                    Benchmark.BenchmarkEncoderInMemory(f, s, query);
                }
            }
            Console.WriteLine();
        }



    }
}
