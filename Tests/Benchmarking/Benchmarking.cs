using Imazen.Profiling;
using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Encoding;
using ImageResizer.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bench
{
    public class Benchmark
    {

        public static void Compare(IEnumerable<BenchmarkRunner> benchmarks)
        {

        }

        public static void PrintConcurrencyResult(OpBenchmarkResults r)
        {
            
            //TODO, make work with segment selection.
            //
            var s = r.StatsForSegment();
            Console.WriteLine("Throwaway run {0}", r.ThrowawayRuns.TimingString());
            Console.WriteLine("{0} parallel;  {5:F}..{6:F}ms ({4}) each;\t Active:{2} Wall:{1} avg={3}",
                   r.ParallelThreads, r.ParallelWallMs, r.ParallelUniqueMs, r.ParallelUniqueMs / r.ParallelThreads, 
                   s.ParallelMs.Avg, s.ParallelMs.Min,s.ParallelMs.Max);

            Console.WriteLine("{0} serial;    {4:F}..{5:F}ms ({3:F}) each;\t Active:{2:F}  Wall:{1:F}",
                    r.SequentialRuns.Count(), r.SequentialWallMs, s.SequentialMs.Sum, s.SequentialMs.Avg, s.SequentialMs.Min, s.SequentialMs.Max);

            Console.WriteLine("{0:F}% concurrent w/ {1} threads {2:F} vcores. Ops avg {3:F}% longer", 
                s.ParallelConcurrencyPercent, s.ParallelThreads,  Environment.ProcessorCount, s.ParallelLatencyPercentAvg);

            Console.WriteLine();

        }

        /// <summary>
        /// This is inherently flawed - the unpredictability and inconsistency of disk and NTFS performance makes these results difficult to read.
        /// </summary>
        public static BenchmarkRunner<ImageJob, JobProfiler> BenchmarkFileToFile(Config c, string source, string dest, Instructions instructions)
        {
            return BenchmarkJob(() => {

                var job = new ImageJob(source, dest, new Instructions(instructions));
                job.DisposeDestinationStream = true;
                job.DisposeSourceObject = true;
                return job;

            },c);
        }



        public static BenchmarkRunner<ImageJob, JobProfiler> BenchmarkJob(Func<ImageJob> jobProvider, Config c)
        {

            var b = new BenchmarkRunner<ImageJob, JobProfiler>();

            //Profile ImageBuild steps
            c.Plugins.GetOrInstall<ProfilingObserverPlugin>();
            b.ProfilerProvider = (s, ix) => new JobProfiler(s);

            b.Setup = jobProvider;
        
            b.Operation = (p,j) => {
                j.Profiler = p;
                c.Build(j);
            };

            b.Teardown = (j) =>
            {
                if (j.Result is IDisposable)
                {
                    ((IDisposable)j.Result).Dispose();
                }
            };
            return b;
        }


       
        public static BenchmarkRunner<ImageJob, JobProfiler> BenchmarkInMemory(Config c, string source, Instructions instructions, bool excludeDecoding = false, bool excludeEncoding = true)
        {
            byte[] data = File.ReadAllBytes(source);
            


            return BenchmarkJob(() => {

                var ms = new MemoryStream(data);
                var dest = excludeEncoding ?  typeof(Bitmap): (object)new MemoryStream(4096);

                var src = excludeDecoding ? (object)c.CurrentImageBuilder.LoadImage(ms, new ResizeSettings(instructions)) : (object)ms;

                var job = new ImageJob(src, dest, new Instructions(instructions));
                job.DisposeDestinationStream = true;
                job.DisposeSourceObject = true;
                return job;

            },c);
        }


        public static BenchmarkRunner<MemoryStream,JobProfiler> BenchmarkDecoderInMemory(Config c, string source, Instructions instructions)
        {

            byte[] data = File.ReadAllBytes(source);
            var b = new BenchmarkRunner<MemoryStream, JobProfiler>();

            b.Setup = () => new MemoryStream(data);

            b.Operation = (p, ms) =>
            {
                using (var bitmap = c.CurrentImageBuilder.LoadImage(ms, new ResizeSettings(instructions))) { }

            };
            return b;
        }

        public static BenchmarkRunner<Tuple<Image,Stream>, JobProfiler> BenchmarkEncoderInMemory(Config c, string source, Instructions instructions)
        {
            byte[] data = File.ReadAllBytes(source);

            var b = new BenchmarkRunner<Tuple<Image, Stream>, JobProfiler>();

            b.Setup = () =>  new Tuple<Image,Stream>(Bitmap.FromStream(new MemoryStream(data)), new MemoryStream(4096 * 4));
            b.Teardown = (t) => t.Item1.Dispose(); 

            b.Operation = (profiler, t) =>
            {
                IEncoder ie = c.CurrentImageBuilder.EncoderProvider.GetEncoder(new ResizeSettings(instructions), t.Item1);
                ie.Write(t.Item1, t.Item2);
            };
            return b;
        }

    }
}
