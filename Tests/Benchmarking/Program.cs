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
using Imazen.Profiling;
using System.Reflection;

namespace Bench
{
    class Program
    {

        public static string imageDir = "..\\..\\Samples\\Images\\";// "..\\..\\..\\..\\Samples\\Images\\";

        public static int ConsoleWidth = 200;
        static void Main(string[] args)
        {
            SearchNearbyForPlugins(); 

            Config c = new Config();

            Console.WindowWidth = ConsoleWidth;



            //parallelism test (thread count sequence)
            //Benchmark relative to costly baseline operation
            //Upload results to S3?
            //compare N actions
            //source data 
            //action buffered? action profiled?
            //IO read qty?


            ///CompareGdToDefault();

            //CompareGdToDefault("bit");

            //CompareGdToDefault("bit");
            //CompareGdToDefault("encode");

            //CompareFastScaling("bit");

            CompareFastScalingToDefaultHQ("bit");

            Console.Write("Done\n");
            Console.ReadKey();
        }

        public BenchmarkRunner<int, JobProfiler> EstablishBaseline()
        {
            var r = new Imazen.Profiling.BenchmarkRunner<int, JobProfiler>();
            r.Operation = new Action<JobProfiler, int>((p, _) => new CostlyBaseline().DoWork());
            r.ParallelThreads = 8;
            r.SequentialRuns = 4;
            r.ParallelRuns = 2;
            return r;
        }

        public static void SearchNearbyForPlugins()
        {
            
            var searchFolders = 
               new []{Assembly.GetExecutingAssembly().Location, 
                Assembly.GetAssembly(typeof(ImageResizer.ImageJob)).Location}
                .SelectMany(s => new []{Path.GetDirectoryName(s), Path.GetDirectoryName(Path.GetDirectoryName(s))})
                .SelectMany(s =>
                    new []{ Path.Combine(s, Environment.Is64BitProcess ? "x64" : "x86"),
                        Path.Combine(s, Environment.Is64BitProcess ? "x64" : "Win32"),
                        s}
                ).Distinct().Where(s => Directory.Exists(s)).ToArray();
            
            
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
            { 
                var dllName = new AssemblyName(args.Name).Name + ".dll";
                var searchLocations = searchFolders.Select(dir => Path.Combine(dir, dllName));
                var existsAt = searchLocations.Where(p => File.Exists(p)).ToArray();
                if (existsAt.Length < 1)
                {
                    throw new FileNotFoundException(dllName);
                }
                return Assembly.LoadFrom(existsAt.First());

            };
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
                SegmentNameFilter = "op";
                UseBarrierAroundSegment = false;
                ExclusiveTimeSignificantMs = 5;
            }
            public int ThrowawayRuns { get; set; }
            public int ThrowawayThreads { get; set; }

            public int SequentialRuns { get; set; }
            public int ParallelRuns { get; set; }
            public int ParallelThreads { get; set; }

            /// <summary>
            /// Segments with less exclusive time than this will not be rendered
            /// </summary>
            /// 
            public double ExclusiveTimeSignificantMs { get; set; }
            public ImageProvider Images { get; set; }

            public IEnumerable<Instructions> SharedInstructions { get; set; }

            public bool ExcludeDecoding { get; set; }
            public bool ExcludeBuilding { get; set; }
            public bool ExcludeEncoding { get; set; }

            public bool ExcludeIO { get; set; }

            public bool ShowProfileTree { get; set; }

            public string SegmentNameFilter { get; set; }

            public bool UseBarrierAroundSegment { get; set; }
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
            settings.Images.PrepareImagesAsync().Wait();
            foreach (var pair in settings.Images.GetImagesAndDescriptions())
            {
                Console.WriteLine();
                String isolation = (settings.SegmentNameFilter != "op" && !settings.UseBarrierAroundSegment) ? 
                                "Measuring '" + settings.SegmentNameFilter + "' without memory barrier; multi-threaded results invalid." 
                                : (settings.UseBarrierAroundSegment ? "Segment '" +  settings.SegmentNameFilter + "' w/ mem barrier." 
                                : "Segment '" + settings.SegmentNameFilter +"'.");


                Console.WriteLine("Using {0} seq. runs, {1} || on {2} threads({5}). {4} Input: {3}", 
                    settings.SequentialRuns,settings.ParallelRuns,settings.ParallelThreads,pair.Item2, isolation,
                    Environment.Is64BitProcess ? "64-bit" : "32-bit");

                var widths = CalcColumnWidths(ConsoleWidth, 4, -2, -2, -2, -4);

                Console.WriteLine(Distribute(widths, "Config", "Sequential", "Parallel", "Percent concurrent", "Instructions"));
                foreach (var instructions in settings.SharedInstructions)
                {

                    var comparableResults = configsAndLabels.Select(triple =>
                    {
                        var combined = triple.Item2 == null ? instructions : new Instructions(triple.Item2.MergeDefaults(instructions));
                        var runner = Benchmark.BenchmarkInMemory(triple.Item1, pair.Item1, combined, settings.ExcludeDecoding, settings.ExcludeEncoding);
                        runner.ParallelRuns = settings.ParallelRuns;
                        runner.ParallelThreads = settings.ParallelThreads;
                        runner.SequentialRuns = settings.SequentialRuns;
                        runner.Label = triple.Item3;
                        runner.ThrowawayThreads = settings.ThrowawayThreads;
                        runner.ThrowawayRuns = settings.ThrowawayRuns;

                        if (settings.UseBarrierAroundSegment)
                            runner.ProfilerProvider = (s,t) => new JobProfiler(s).JoinThreadsAroundSegment(settings.SegmentNameFilter,t);
                        else
                            runner.ProfilerProvider = (s, t) => new JobProfiler(s);

                        var results = runner.Benchmark();

                        Action<IConcurrencyResults> printStats = r =>
                        {
                            var statStrs = new List<string>(GetStats(triple.Item3, r.GetStats()));
                            statStrs.Add(combined.ToString());
                            Console.WriteLine(Distribute(widths, statStrs.ToArray()));
                        };
                        var set = results.FindSet(settings.SegmentNameFilter);
                        printStats(set);

                        if (settings.ShowProfileTree)
                        {
                            var f = new ConcurrencyResultFormatter();
                            f.ExclusiveTimeSignificantMs = settings.ExclusiveTimeSignificantMs;
                            Console.WriteLine(f.PrintCallTree(set));
                        }
                        return new Tuple<string, IConcurrencyResults>(triple.Item3, set);
                    }).ToArray();

                   

                    var seqList = comparableResults.OrderBy(r => r.Item2.FastestSequentialMs()).
                                                    Select((r, i) => string.Format("{0}. {1} {3:F2}X slower {2}",
                                                        i + 1, r.Item1, r.Item2.GetStats().SequentialMs.ToString(0.05), 
                                r.Item2.FastestSequentialMs() / comparableResults.Min(c => c.Item2.FastestSequentialMs())));


                    Console.WriteLine("Sequential: \n" + string.Join("\n", seqList));

                    var parList = comparableResults.OrderBy(r => r.Item2.ParallelRealMs()).
                                                    Select((r, i) => string.Format("{0}. {1} {3:F2}X less throughput {2:F2}ms total. {4:F}% concurrent",
                                                        i + 1, r.Item1, r.Item2.GetStats().ParallelRealMs, 
                                r.Item2.ParallelRealMs() / comparableResults.Min(c => c.Item2.ParallelRealMs()), r.Item2.GetStats().ParallelConcurrencyPercent));

                    Console.WriteLine("Paralell: \n" + string.Join("\n", parList));
                    Console.WriteLine("\n");

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

        public static void CheckGdMemoryUse()
        {
            var imageSrc = new ImageProvider().AddBlankImages(new Tuple<int, int, string>[] { new Tuple<int, int, string>(4000, 4000, "jpg") });
            imageSrc.PrepareImagesAsync().Wait();
            var runner = Benchmark.BenchmarkInMemory(ConfigWithPlugins("GdBuilder"), imageSrc.GetImages().First(),
                new Instructions("builder=gd&width=400"), false, false);
            runner.Label = "GD";
            CheckMemoryUse(runner, 2);
        }

        public static void CheckMemoryUse(BenchmarkRunner<ImageJob,JobProfiler> runner, int runMultiplier)
        {
            runner.ParallelRuns = runMultiplier;
            runner.ParallelThreads = 8;
            runner.SequentialRuns = runMultiplier * 8;
            runner.ThrowawayThreads = 8;
            runner.ThrowawayRuns = runMultiplier * 2;


            Console.Write("Running {0} warmup (t={1}), {2} parallel (t={3}), {4} sequenetial\n", runner.ThrowawayRuns * runner.ThrowawayThreads, runner.ThrowawayThreads,
                runner.ParallelRuns * runner.ParallelThreads, runner.ParallelThreads,
                runner.SequentialRuns);
            var results = runner.Benchmark();

            Console.Write("Private bytes before: {0:F2}MB warm: {1:F2}MB after {2:F2}MB\n", results.PrivateBytesBefore / 1000000.0, results.PrivateBytesWarm / 1000000.0, results.PrivateBytesAfter / 1000000.0);
            Console.Write("Managed bytes before: {0:F2}MB warm: {1:F2}MB after {2:F2}MB\n", results.ManagedBytesBefore / 1000000.0, results.ManagedBytesWarm / 1000000.0, results.ManagedBytesAfter / 1000000.0);
        
        }

        public static void CompareGdToDefault(string segment = "op")
        {
            var settings = new BenchmarkingSettings();
            settings.Images = new ImageProvider();
            settings.Images.AddLocalImages(imageDir, "fountain-small.jpg");
                //.AddBlankImages(new Tuple<int, int, string>[] { new Tuple<int, int, string>(2200, 2200, "jpg") });
                //.AddLocalImages(imageDir, "quality-original.jpg", "fountain-small.jpg");
            settings.SharedInstructions = new Instructions[]{new Instructions(
                "width=800&scale=both")};
            settings.ExcludeEncoding = false;
            settings.ExcludeDecoding = false;
            settings.ExcludeBuilding = false;
            settings.ExcludeIO = true;
            settings.ParallelRuns = 2;
            settings.SegmentNameFilter = segment;
            settings.ParallelThreads = 8;
            settings.SequentialRuns = 16;
            settings.ThrowawayRuns = 2;
            settings.ThrowawayThreads = 8;
            settings.UseBarrierAroundSegment = true;

            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins(),null,"Default"),
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins("GdBuilder"),new Instructions("builder=gd"),"GdBuilder")};

            Compare(settings, configs.Reverse());


        }

        public static BenchmarkingSettings BenchmarkingDefaults()
        {
            var settings = new BenchmarkingSettings();
            settings.Images = new ImageProvider(); 
            settings.ExcludeEncoding = false;
            settings.ExcludeDecoding = false;
            settings.ExcludeBuilding = false;
            settings.ExcludeIO = true;
            settings.ParallelRuns = 2;

            settings.ParallelThreads = 8;
            settings.SequentialRuns = 16;
            settings.ThrowawayRuns = 2;
            settings.ThrowawayThreads = 8;
            settings.UseBarrierAroundSegment = true;
            return settings;
        }
        public static BenchmarkingSettings ScalingComparisonDefault()
        {
            var settings = BenchmarkingDefaults();
            settings.Images.AddBlankImages(new Tuple<int, int, string>[] { new Tuple<int, int, string>(3264, 2448, "jpg"), new Tuple<int, int, string>(1200, 900, "png") });
            settings.SharedInstructions = new Instructions[]{new Instructions(
                "width=800&scale=both") , new Instructions("width=200&scale=both")};
            return settings;
        }

        public static void CheckFastScalingMemoryUse()
        {
            var imageSrc = new ImageProvider().AddBlankImages(new Tuple<int, int, string>[] { new Tuple<int, int, string>(4000, 4000, "jpg") });
            imageSrc.PrepareImagesAsync().Wait();
            var runner = Benchmark.BenchmarkInMemory(ConfigWithPlugins("FastScaling"), imageSrc.GetImages().First(),
                new Instructions("builder=gd&width=400"), false, false);
            runner.Label = "FastScaling";
            CheckMemoryUse(runner, 2);
        }


        public static void CompareFastScalingToDefault(string segment = "op")
        {
            var settings = BenchmarkingDefaults();
            settings.Images.AddBlankImages(
                new Tuple<int, int, string>[] { new Tuple<int, int, string>(4000, 3000, "jpg"), new Tuple<int, int, string>(1600, 800, "png") });
            settings.SharedInstructions = new Instructions[]{new Instructions(
                "width=800&scale=both") , new Instructions("width=200&scale=both")};
     
            settings.SegmentNameFilter = segment;
            settings.ExclusiveTimeSignificantMs = 1;
            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins(),null,"System.Drawing"),
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins("ImageResizer.Plugins.FastScaling.FastScalingPlugin, ImageResizer.Plugins.FastScaling"),new Instructions("fastscale=true;turbo=true;min_scaled_weighted=0"),"FastScaling")};

            Compare(settings, configs.Reverse());


        }

        public static void CompareFastScalingToDefaultHQ(string segment = "op")
        {
            var settings = BenchmarkingDefaults();
            settings.Images.AddBlankImages(
                new Tuple<int, int, string>[] { new Tuple<int, int, string>(4000, 3000, "jpg"), new Tuple<int, int, string>(1600, 800, "png") });
            settings.SharedInstructions = new Instructions[]{new Instructions(
                "width=800&scale=both") , new Instructions("width=200&scale=both")};

            settings.SegmentNameFilter = segment;
            settings.ExclusiveTimeSignificantMs = 1;
            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins(),null,"System.Drawing"),
                    new Tuple<Config, Instructions, string>(ConfigWithPlugins("ImageResizer.Plugins.FastScaling.FastScalingPlugin, ImageResizer.Plugins.FastScaling"),new Instructions("fastscale=true;window=1.4"),"FastScaling with superior quality")};

            Compare(settings, configs.Reverse());


        }



        public static void CompareFastScaling(string segment = "op")
        {
            var settings = ScalingComparisonDefault();
            settings.SegmentNameFilter = segment;
            var c = ConfigWithPlugins("ImageResizer.Plugins.FastScaling.FastScalingPlugin, ImageResizer.Plugins.FastScaling");
            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(new Config(),null,"System.Drawing"),
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true"),"FastScaling Bicubic - no skipped source pixels"),
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true&window=1.4"),"FastScaling Bicubic w/ 6x window size (GDI HQ Bicubic prefilter equivalent)"),
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true&turbo=true"),"FastScaling+Halving"),
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true&f=7&window=1.4"),"FastScaling6X Lanczos")};

            Compare(settings, configs.Reverse());
        }

        public static void CompareFastScalingWindows(string segment = "op")
        {
            var settings = ScalingComparisonDefault();
            settings.SegmentNameFilter = segment;
            var c = ConfigWithPlugins("ImageResizer.Plugins.FastScaling.FastScalingPlugin, ImageResizer.Plugins.FastScaling");
            var configs = new Tuple<Config, Instructions, string>[]{
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true&window=1.3"),"FastScaling with large window (1.3)"),
                    new Tuple<Config, Instructions, string>(c,new Instructions("&fastscale=true&window=0.5"),"FastScaling with standard window (0.5)")};

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
