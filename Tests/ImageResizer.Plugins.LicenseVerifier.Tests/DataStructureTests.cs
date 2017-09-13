using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ImageResizer.Configuration.Performance;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{
    public class DataStructureTests
    {
        public DataStructureTests(ITestOutputHelper output) { this.output = output; }

        ITestOutputHelper output;

        class TimeProvider
        {
            public long Timestamp { get; set; }

            public long GetTimestamp() => Timestamp;
        }

        [Fact]
        public void CircularBufferTest()
        {
            // This buffer stores 3 +1 buckets at a time, allowing simultaneous writes to 3 buckets
            // The unwritable 4th is flushed when a new bucket is entered. 
            var b = new CircularTimeBuffer(10, 3);

            // Bucket 11
            Assert.True(b.Record(111, 1));
            // Bucket 12
            Assert.True(b.Record(121, 1));
            Assert.True(b.Record(122, 1));
            // Bucket 13
            Assert.True(b.Record(131, 1));
            Assert.True(b.Record(139, 2));
            // Bucket 14
            Assert.True(b.Record(141, 3));
            Assert.True(b.Record(141, 1));
            // Bucket 16
            Assert.True(b.Record(151, 5));
            // Bucket 16
            Assert.True(b.Record(161, 0));
            // Bucket 17, 18, 19 (skipped, zero)
            // Bucket 20 - This causes Buckets 11-16 to dequeue, leaving 17 pending and 18, 19, 20 active.
            Assert.True(b.Record(201, 0));

            // Skipped buckets (zeroes) are always the last to dequeue, 
            // but we need to record a value that is 4 intervals after the last to cause all to dequeue 
            Assert.Equal(new[] {1L, 2, 3, 4, 5, 0}, b.DequeueValues());

            // Bucekts 21..30 are skipped

            // Buckets 31,32,33
            Assert.True(b.Record(311, 1));
            Assert.True(b.Record(321, 2));
            Assert.True(b.Record(331, 3));

            var zeroes = b.DequeueValues();
            Assert.Equal(13, zeroes.Count());
            Assert.True(zeroes.All(v => v == 0));

            //We should have nothing to dequeue, with 30..33 in the buffer
            Assert.Equal(0, b.DequeueValues().Count());

            // Skip buckets 34..55, dequeuing 30..52
            Assert.True(b.Record(561, 3));

            var results = b.DequeueValues();
            Assert.Equal(23, results.Count());
            Assert.Equal(new[] {0L, 1, 2, 3}, results.Take(4));
        }
        
        [Fact]
        public void MultiIntervalTest()
        {
            NamedInterval[] Intervals  = {
                new NamedInterval { Unit = "second", Name = "Per Second", TicksDuration = Stopwatch.Frequency },
                    new NamedInterval { Unit = "minute", Name = "Per Minute", TicksDuration = Stopwatch.Frequency * 60 },
                    new NamedInterval { Unit = "15_mins", Name = "Per 15 Minutes", TicksDuration = Stopwatch.Frequency * 60 * 15 },
                    new NamedInterval { Unit = "hour", Name = "Per Hour", TicksDuration = Stopwatch.Frequency * 60 * 60 },
                };
            var s = new MultiIntervalStats(Intervals);
            for (long i = Stopwatch.Frequency * -1000; i < Stopwatch.Frequency * 5000; i += Stopwatch.Frequency / 2)
            {
                s.Record(i, 1);
            }
        }

        [Fact]
        public void TestAddMulModHash()
        {
            var h = new AddMulModHash(new MersenneTwister(1215125));

            var list = Enumerable.Range(0, 80000).Select(i => h.ComputeHash((uint) i)).ToList();
            var set = new HashSet<uint>(list);
            Assert.Equal(set.Count(), list.Count());
        }

        [Fact]
        public void TestAddMulModHashBucketCollisions()
        {
            var h = new AddMulModHash(new MersenneTwister(1215125));

            var maxCollisions = Enumerable.Range(0, 80000)
                                          .Select(i => h.ComputeHash((uint) i) % 1000)
                                          .GroupBy(i => i, i => 1)
                                          .Max(g => g.Count());
            Assert.True(maxCollisions < 100);
        }

        [Fact]
        public void TestClampDuration()
        {
            var clamp = DurationClamping.Default600Seconds();
            Assert.Equal(100, clamp.ClampMicroseconds(100));
            Assert.Equal(200, clamp.ClampMicroseconds(110));
            Assert.Equal(1000, clamp.ClampMicroseconds(1000));
            Assert.Equal(1100, clamp.ClampMicroseconds(1100));
        }

        [Fact]
        public void TestDurationClamping()
        {
            var d = DurationClamping.Default600Seconds();

            Assert.Equal(d.SegmentsPossibleValuesCount().Sum(), d.PossibleValues().Count());

            Assert.Equal(100, d.Clamp(90));
            Assert.Equal(0, d.Clamp(0));
            Assert.Equal(600, d.Clamp(600));
            Assert.Equal(48000, d.Clamp(48000));
        }

        [Fact]
        public void TestMinSketch()
        {
            var rand = new MersenneTwister(1095807143);
            uint slots = 5113;
            uint algs = 5;
            //uint slots = 1279;
            //uint algs = 3;

            var inputs = Enumerable.Range(0, (int) slots).Select(ix => (uint) rand.NextUint32()).Distinct().ToList();

            var uniqueInputs = inputs.Count();


            var medianOvercountExpected = (double) uniqueInputs / slots;
            var peakOvercountPercentageAllowed = 1.03;
            var actualCount = 1;
            var allowedOvercount = Math.Max(actualCount,
                actualCount * medianOvercountExpected * peakOvercountPercentageAllowed);


            var s = new CountMinSketch<AddMulModHash>(slots, algs, AddMulModHash.DeterministicDefault());

            foreach (var i in inputs) {
                s.InterlockedAdd(i, actualCount);
            }

            var errors = new List<KeyValuePair<uint, long>>(100);
            for (uint i = 0; i < uniqueInputs; i++) {
                var found = s.Estimate(i);
                if (found > allowedOvercount) {
                    errors.Add(new KeyValuePair<uint, long>(i, found));
                }
            }
            Assert.Equal(9, errors.Count());
        }

        [Fact]
        public void TestPercentileCalculations()
        {
            var values = new[] {0L, 1L, 2L, 3L, 4L, 5L};
            Assert.Equal(0, values.GetPercentile(0));
            Assert.Equal(5, values.GetPercentile(1));
            Assert.Equal(2, values.GetPercentile(0.5f));

            Assert.Equal(0, values.GetPercentile(0.1f));
            Assert.Equal(1, values.GetPercentile(0.25f));
            Assert.Equal(1, values.GetPercentile(0.33f));
            Assert.Equal(2, values.GetPercentile(0.4f));
            Assert.Equal(3, values.GetPercentile(0.6f));
            Assert.Equal(4, values.GetPercentile(0.75f));
            Assert.Equal(5, values.GetPercentile(0.85f));
            Assert.Equal(5, values.GetPercentile(0.95f));
        }

        [Fact]
        public void TestRates()
        {
            var time = new TimeProvider();

            var r = new MultiIntervalStats(
                new[] {
                    new NamedInterval {
                        Name = "per second",
                        TicksDuration = Stopwatch.Frequency,
                        Unit = "second"
                    }
                }, time.GetTimestamp);

            time.Timestamp = Stopwatch.Frequency * 2;
            Assert.True(r.Record(Stopwatch.Frequency + 10, 10));
            Assert.True(r.Record(Stopwatch.Frequency + 30, 10));
            Assert.True(r.Record(Stopwatch.Frequency + 10, 10));

            time.Timestamp = Stopwatch.Frequency * 3;

            Assert.True(r.Record(Stopwatch.Frequency * 2 + 1, 20));
            Assert.True(r.Record(Stopwatch.Frequency * 3 + 1, 10));

            time.Timestamp = Stopwatch.Frequency * 10;
            Assert.True(r.Record(Stopwatch.Frequency * 9 + 1, 10));

            Assert.Equal(70, r.RecordedTotal);

            var stat = r.GetStats().First();
            Assert.Equal(0, stat.Min); //we skipped buckets
            // Assert.Equal(14, stat.Avg); //TODO: fix, unstable results, should not occur!
            Assert.Equal(30, stat.Max);
        }

        [Fact]
        public void TestSigRounding()
        {
            var v = new SignificantDigitsClamping();


            Assert.Equal(0, v.Clamp(0));
            Assert.Equal(19, v.Clamp(19));
            Assert.Equal(240, v.Clamp(239));
            Assert.Equal(2300, v.Clamp(2345));
            //Assert.Equal(2400, v.Clamp(2345));
        }

        [Fact]
        public void TestSigRoundingBrute()
        {
            for (var digits = 1; digits < 5; digits++) {
                var clamp = new SignificantDigitsClamping {SignificantDigits = digits};

                Assert.True(
                    Enumerable.Range(0, (int) Math.Pow(10, clamp.SignificantDigits)).All(v => v == clamp.Clamp(v)));

                var count = (int) Math.Pow(10, clamp.SignificantDigits);
                Assert.Equal(Enumerable.Repeat(clamp.Clamp(26173000000), count),
                    Enumerable.Range(1, count).Select(v => clamp.Clamp(v + 26173000000)));
                Assert.Equal(Enumerable.Repeat(clamp.Clamp(33333000000), count),
                    Enumerable.Range(1, count).Select(v => clamp.Clamp(v + 33333000000)));
            }
        }

        [Fact]
        public void TestTimeSink()
        {
            var s = new TimingsSink();
            s.ReportMicroseconds(100);
            s.ReportMicroseconds(110);
            s.ReportMicroseconds(1000);
            s.ReportMicroseconds(1100);
            Assert.Equal(new[] {100L, 200L, 1000L, 1100L}, s.GetAllValues());

            Assert.Equal(1100, s.GetPercentile(1.0f));
            Assert.Equal(1100, s.GetPercentile(0.76f));
            Assert.Equal(1050, s.GetPercentile(0.75f));

            Assert.Equal(600, s.GetPercentile(0.5f));

            Assert.Equal(150, s.GetPercentile(0.25f));

            Assert.Equal(150, s.GetPercentile(0.11f));

            Assert.Equal(100, s.GetPercentile(0.0f));
        }


        [Fact]
        public void TestInterlocked()
        {
            long v = 1;
            Assert.Equal(1, Interlocked.CompareExchange(ref v, 3, 0));
            Assert.Equal(1, Interlocked.CompareExchange(ref v, 0, 0));
            Assert.Equal(1, Interlocked.CompareExchange(ref v, 0, 1));

            Assert.Equal(0, Interlocked.CompareExchange(ref v, 0, 0));

            Assert.Equal(0, Interlocked.CompareExchange(ref v, 4, 0));

            Assert.Equal(4, Interlocked.CompareExchange(ref v, 0, 4));
        }
        [Fact]
        public void TestInterlockedMax()
        {
            long v = 0;
            var tasks = Enumerable.Repeat(0, 30).Select((j) => Task.Run(() =>
             {
                 for (var i = 0; i < 100000; i++)
                 {
                     Utilities.InterlockedMax(ref v, v + 1);
                 }
             })).ToArray();

            Task.WaitAll(tasks);
        }

        [Fact]
        public void TestInterlockedMin()
        {
            long v = 0;
            var tasks = Enumerable.Repeat(0, 30).Select((j) => Task.Run(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    Utilities.InterlockedMin(ref v, v - 1);
                }
            })).ToArray();

            Task.WaitAll(tasks);
        }
    }
}
