using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Imazen.Profiling
{

    public abstract class BenchmarkRunner
    {
        public string Label { get; set; }
        public int ThrowawayRuns { get; set; }
        public int ThrowawayThreads { get; set; }

        public int SequentialRuns { get; set; }
        public int ParallelRuns { get; set; }
        public int ParallelThreads { get; set; }


        public OpBenchmarkResults Benchmark()
        {

            var r = new OpBenchmarkResults();
            WarmupThreads();
            ResetGC();
            r.PrivateBytesBefore = Process.GetCurrentProcess().PrivateMemorySize64;
            r.ManagedBytesBefore = GC.GetTotalMemory(true);

            r.ThrowawayThreads = this.ThrowawayThreads;
            r.ParallelThreads = this.ParallelThreads;
            r.CoreCount = Environment.ProcessorCount;
            r.SegmentName = "op";

            //Throwaway run for warmup
            r.ThrowawayRuns = TimeOperation(ThrowawayRuns, ThrowawayThreads);

            r.PrivateBytesWarm = Process.GetCurrentProcess().PrivateMemorySize64;
            r.ManagedBytesWarm = GC.GetTotalMemory(false);

            //Time in parallel
            var wallClock = Stopwatch.StartNew();
            r.ParallelRuns = TimeOperation(ParallelRuns, ParallelThreads);
            wallClock.Stop();
            r.ParallelWallTicks = wallClock.ElapsedTicks;

            //Time in serial
            wallClock.Restart();
            r.SequentialRuns = TimeOperation(SequentialRuns, 1);
            wallClock.Stop();
            r.SequentialWallTicks = wallClock.ElapsedTicks;

            r.ParallelUniqueTicks = r.ParallelRuns.DeduplicateTime();

            ResetGC();
            r.ManagedBytesAfter = GC.GetTotalMemory(true);

            r.PrivateBytesAfter = Process.GetCurrentProcess().PrivateMemorySize64;

            return r;

        }

        public abstract IEnumerable<ProfilingResultNode> TimeOperation(int batches, int threadsPerBatch);

        private void WarmupThreads()
        {
            //Warmup threads (maybe - but maybe not - TPL doesn't tell us)
            if (Math.Max(ParallelThreads, ThrowawayThreads) > 1) Parallel.For(0, Math.Max(ParallelThreads, ThrowawayThreads), x => Thread.Sleep(1));
        }

        public static void ResetGC()
        {
            GC.Collect(0, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(0, GCCollectionMode.Forced, true);
            Thread.Sleep(10);
        }


    }
    public class BenchmarkRunner<T,P>: BenchmarkRunner where P: IProfilingAdapter, new() {

        public BenchmarkRunner()
        {

            ThrowawayRuns = 1;
            SequentialRuns = 8;
            ParallelRuns = 2;
            ParallelThreads = 8;
            ProfilerProvider = (rootNodeName, threads) => (P)(new P()).Create(rootNodeName);
        }


        public Func<T> Setup { get; set; }
        public Action<P, T> Operation { get; set; }
        public Action<T> Teardown { get; set; }

        /// <summary>
        /// Lambda to create a Profiler with the given root name and correct thread count (can be used to barrier/isolate segments)
        /// </summary>
        public Func<string,int, P> ProfilerProvider { get; set; }


        public override IEnumerable<ProfilingResultNode> TimeOperation(int batches, int threadsPerBatch)
        {
            return BenchmarkRunner<T, P>.TimeOperation(Setup, ProfilerProvider, Operation, Teardown, batches, threadsPerBatch);
        }
        public static IEnumerable<ProfilingResultNode> TimeOperation(Func<T> setUp, Func<string,int, P> profilerProvider, Action<P, T> op, Action<T> tearDown, int batches, int threadsPerBatch)
        {
            if (batches < 1) return new ProfilingResultNode[] { };
            string rootNodeName = "op";
            var wrappedAction = new Action<Tuple<P, T>>(t =>
            {
                t.Item1.Start(rootNodeName);
                op(t.Item1, t.Item2);
                t.Item1.Stop(rootNodeName);
            });


            var rootNodes = new List<ProfilingNode>(batches * threadsPerBatch);

            for (var i = 0; i < batches; i++)
            {

                var inputs = new int[threadsPerBatch].Select(x => new Tuple<P, T>(profilerProvider(rootNodeName, threadsPerBatch), setUp != null ? setUp() : default(T))).ToList();
                try
                {
                    ResetGC();
                    if (threadsPerBatch == 1)
                    {
                        wrappedAction(inputs.First());
                    }
                    else
                    {
                        Parallel.ForEach(inputs, wrappedAction);
                    }
                }
                finally
                {
                    if (tearDown != null) inputs.ForEach(x => tearDown(x.Item2));
                }
                rootNodes.AddRange(inputs.Select(x => x.Item1.RootNode));


            }
            return rootNodes.Select(n => n.ToProfilingResultNode());
        }


    }
}
