using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins;
using ImageResizer.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;


namespace ImageResizer.Configuration.Performance
{

    // https://github.com/jawa-the-hutt/lz-string-csharp/blob/master/src/LZString.cs
    // https://github.com/maiwald/lz-string-ruby
    public class GlobalPerf
    {
        public static GlobalPerf Singleton { get; } = new GlobalPerf();
        Lazy<ProcessInfo> Process = new Lazy<ProcessInfo>();
        IssueSink sink = new IssueSink("GlobalPerf");
        Lazy<HardwareInfo> Hardware;
        Lazy<PluginInfo> Plugins = new Lazy<PluginInfo>();

        System.Web.HttpModuleCollection httpModules = null;

        NamedInterval[] Intervals = new[] {
            new NamedInterval { Unit="second", Name="Per Second", TicksDuration =  Stopwatch.Frequency},
            new NamedInterval { Unit="minute", Name="Per Minute", TicksDuration =  Stopwatch.Frequency * 60},
            new NamedInterval { Unit="15_mins", Name="Per 15 Minutes", TicksDuration =  Stopwatch.Frequency * 60 * 15},
            new NamedInterval { Unit="hour", Name="Per Hour", TicksDuration =  Stopwatch.Frequency * 60 * 60},
        };

        ConcurrentDictionary<string, MultiIntervalStats> rates = new ConcurrentDictionary<string, MultiIntervalStats>();

        MultiIntervalStats blobReadEvents;
        MultiIntervalStats blobReadBytes;
        MultiIntervalStats jobs;
        MultiIntervalStats decodedPixels;
        MultiIntervalStats encodedPixels;

        ConcurrentDictionary<string, IPercentileProviderSink> percentiles = new ConcurrentDictionary<string, IPercentileProviderSink>();

        IPercentileProviderSink job_times;
        IPercentileProviderSink decode_times;
        IPercentileProviderSink encode_times;
        IPercentileProviderSink job_other_time;
        IPercentileProviderSink blob_read_times;
        IPercentileProviderSink collect_info_times;

        DictionaryCounter<string> counters = new DictionaryCounter<string>("counter_update_failed");

        int[] Percentiles = new[] { 5, 25, 50, 75, 95, 100 };


        IPercentileProviderSink sourceWidths;
        IPercentileProviderSink sourceHeights;
        IPercentileProviderSink outputWidths;
        IPercentileProviderSink outputHeights;
        IPercentileProviderSink sourceMegapixels;
        IPercentileProviderSink outputMegapixels;
        IPercentileProviderSink scalingRatios;
        IPercentileProviderSink sourceAspectRatios;
        IPercentileProviderSink outputAspectRatios;

        GlobalPerf()
        {
            Hardware = new Lazy<HardwareInfo>(() => new HardwareInfo(sink));
            blobReadBytes = rates.GetOrAdd("blob_read_bytes", new MultiIntervalStats(Intervals));
            blobReadEvents = rates.GetOrAdd("blob_reads", new MultiIntervalStats(Intervals));
            jobs = rates.GetOrAdd("jobs_completed", new MultiIntervalStats(Intervals));
            decodedPixels = rates.GetOrAdd("decoded_pixels", new MultiIntervalStats(Intervals));
            encodedPixels = rates.GetOrAdd("encoded_pixels", new MultiIntervalStats(Intervals));

            job_times = percentiles.GetOrAdd("job_times", new TimingsSink());
            decode_times = percentiles.GetOrAdd("decode_times", new TimingsSink());
            encode_times = percentiles.GetOrAdd("encode_times", new TimingsSink());
            job_other_time = percentiles.GetOrAdd("job_other_time", new TimingsSink());
            blob_read_times = percentiles.GetOrAdd("blob_read_times", new TimingsSink());
            collect_info_times = percentiles.GetOrAdd("collect_info_times", new TimingsSink());

            sourceMegapixels = percentiles.GetOrAdd("source_pixels", new PixelCountSink());
            outputMegapixels = percentiles.GetOrAdd("output_pixels", new PixelCountSink());
            sourceWidths = percentiles.GetOrAdd("source_width", new ResolutionsSink());
            sourceHeights = percentiles.GetOrAdd("source_height", new ResolutionsSink());
            outputWidths = percentiles.GetOrAdd("output_width", new ResolutionsSink());
            outputHeights = percentiles.GetOrAdd("output_height", new ResolutionsSink());

            scalingRatios = percentiles.GetOrAdd("scaling_ratio", new FlatSink(1000));
            sourceAspectRatios = percentiles.GetOrAdd("source_aspect_ratio", new FlatSink(1000));
            outputAspectRatios = percentiles.GetOrAdd("output_aspect_ratio", new FlatSink(1000));

        }



        internal void JobComplete(ImageBuilder builder, ImageJob job)
        {

            var timestamp = Stopwatch.GetTimestamp();
            var s_w = job.SourceWidth.GetValueOrDefault(0);
            var s_h = job.SourceHeight.GetValueOrDefault(0);
            var f_w = job.FinalWidth.GetValueOrDefault(0);
            var f_h = job.FinalHeight.GetValueOrDefault(0);


            if (job.SourceWidth.HasValue && job.SourceHeight.HasValue)
            {
                var prefix = "source_multiple_";
                if (s_w % 4 == 0 && s_h % 4 == 0)
                {
                    counters.Increment(prefix + "4x4");
                }
                if (s_w % 8 == 0 && s_h % 8 == 0)
                {
                    counters.Increment(prefix + "8x8");
                }
                if (s_w % 8 == 0)
                {
                    counters.Increment(prefix + "8x");
                }
                if (s_h % 8 == 0)
                {
                    counters.Increment(prefix + "x8");
                }
                if (s_w % 16 == 0 && s_h % 16 == 0)
                {
                    counters.Increment(prefix + "16x16");
                }

            }

    

            //(builder.SettingsModifier as PipelineConfig).GetImageBuilder

            var readPixels = job.SourceWidth.GetValueOrDefault(0) * job.SourceHeight.GetValueOrDefault(0);
            var wrotePixels = job.FinalWidth.GetValueOrDefault(0) * job.FinalHeight.GetValueOrDefault(0);

            if (readPixels > 0)
            {
                sourceMegapixels.Report(readPixels);

                sourceWidths.Report(s_w);
                sourceHeights.Report(s_h);

                sourceAspectRatios.Report(s_w * 100 / s_h);
            }
            if (wrotePixels > 0)
            {
                outputMegapixels.Report(wrotePixels);


                outputWidths.Report(f_w);
                outputHeights.Report(f_h);
                outputAspectRatios.Report(f_w * 100 / f_h);
            }
            if (readPixels > 0 && wrotePixels > 0)
            {
                scalingRatios.Report(s_w * 100 / f_w);
                scalingRatios.Report(s_h * 100 / f_h);
            }

            jobs.Record(timestamp, 1);
            decodedPixels.Record(timestamp, readPixels);
            encodedPixels.Record(timestamp, wrotePixels);

           
            job_times.Report(job.TotalTicks);
            decode_times.Report(job.DecodeTicks);
            encode_times.Report(job.EncodeTicks);
            job_other_time.Report(job.TotalTicks - job.DecodeTicks - job.EncodeTicks);

            if (job.SourcePathData != null)
            {
                var ext = PathUtils.GetExtension(job.SourcePathData).ToLowerInvariant().TrimStart('.');
                counters.Increment("source_file_ext_" + ext);
            }
            
            var plugins = builder.EncoderProvider as PluginConfig;
            if (plugins != null) Plugins.Value.Notify(plugins);

            PostJobQuery(job.Instructions);

            if (System.Web.HttpContext.Current?.Request != null)
            {
                NoticeDomains(System.Web.HttpContext.Current.Request);
            }
            if (httpModules == null)
            {
                httpModules = System.Web.HttpContext.Current?.ApplicationInstance?.Modules;
            }
        }


        ConcurrentDictionary<string, DictionaryCounter<string>> uniques 
            = new ConcurrentDictionary<string, DictionaryCounter<string>>(StringComparer.Ordinal);
        private long CountLimitedUniqueValuesIgnoreCase(string category, string value, int limit, string otherBucketValue)
        {
            return uniques.GetOrAdd(category, (k) =>
                new DictionaryCounter<string>(limit, otherBucketValue, StringComparer.OrdinalIgnoreCase))
                .Increment(value == null ? "null" : value);

        }
        private IEnumerable<string> GetPopularUniqueValues(string category, int limit)
        {
            DictionaryCounter<string> v;
            return uniques.TryGetValue(category, out v) ?
                    v.GetCounts()
                    .Where(pair => pair.Value > 0)
                    .OrderByDescending(pair => pair.Value)
                    .Take(limit).Select(pair => pair.Key) :
                    Enumerable.Empty<string>();
        }

        private void NoticeDomains(System.Web.HttpRequest request)
        {
            var image_domain = request?.Url?.DnsSafeHost;
            var page_domain = request?.UrlReferrer?.DnsSafeHost;
            if (image_domain != null) {
                CountLimitedUniqueValuesIgnoreCase("image_domains", image_domain, 45, "_other_");
            }
            if (page_domain != null)
            {
                CountLimitedUniqueValuesIgnoreCase("page_domains", page_domain, 45, "_other_");
            }
        }

        private void PostJobQuery(Instructions q)
        {
            foreach (var key in q.AllKeys)
            {
                if (key != null)
                {
                    CountLimitedUniqueValuesIgnoreCase("job_query_keys", key, 100, "_other_");
                }
            }
        }

        internal void PreRewriteQuery(NameValueCollection q)
        {
            foreach (var key in q.AllKeys)
            {
                if (key != null)
                {
                    CountLimitedUniqueValuesIgnoreCase("original_query_keys", key, 100, "_other_");
                }
            }
        }

        internal void QueryRewrittenWithDirective(string rewrittenVirtualPath)
        {
            
        }

        public static void BlobRead(Config c, long ticks, long bytes)
        {
            Singleton.blobReadEvents.Record(Stopwatch.GetTimestamp(), 1);
            Singleton.blobReadBytes.Record(Stopwatch.GetTimestamp(), bytes);
            Singleton.blob_read_times.Report(ticks);
        }

        public IInfoAccumulator GetReportPairs()
        {
            var q = new QueryAccumulator().Object;
            var timeThis = Stopwatch.StartNew();
            // Increment when we break the schema
            q.Add("reporting_version", 2);

            Process.Value.SetModules(httpModules);
            Process.Value.Add(q);
            Hardware.Value.Add(q);
            Plugins.Value.Add(q);

            foreach(var plugin in Config.Current.Plugins.GetAll<IPluginInfo>())
            {
                foreach(var pair in plugin.GetInfoPairs())
                {
                    q.Add(pair.Key, pair.Value);
                }
            }

            //Add counters
            foreach(var pair in counters.GetCounts()){
                q.Add(pair.Key, pair.Value.ToString());
            }

            //Add rates
            foreach(var rate in rates)
            {
                q.Add(rate.Key + "_total", rate.Value.RecordedTotal);
                foreach (var pair in rate.Value.GetStats())
                {
                    var basekey = rate.Key + "_per_" + pair.Interval.Unit;
                    q.Add(basekey + "_max", pair.Max);
                }
            }

            //Add percentiles
            foreach(var d in percentiles)
            {
                var values = d.Value.GetPercentiles(Percentiles.Select(p => p / 100.0f));
                q.Add(values.Zip(Percentiles, 
                    (result, percent) => 
                        new KeyValuePair<string, string>(
                            d.Key + "_" + percent.ToString() + "th", result.ToString())));
                
            }


            q.Add("image_domains",
               string.Join(", ", GetPopularUniqueValues("image_domains", 8)));
            q.Add("page_domains",
                string.Join(", ", GetPopularUniqueValues("page_domains", 8)));

            var originalKeys = GetPopularUniqueValues("original_query_keys", 40).ToArray();

            q.Add("query_keys",
                string.Join(", ", originalKeys));
            q.Add("extra_job_query_keys",
                string.Join(", ", GetPopularUniqueValues("job_query_keys", 40).Except(originalKeys).Take(2)));

            timeThis.Stop();
            collect_info_times.Report(timeThis.ElapsedTicks);
            return q;
        }

        public void TrackRate(string event_category_key, long count)
        {
            rates.GetOrAdd(event_category_key, (k) => new MultiIntervalStats(Intervals)).Record(Stopwatch.GetTimestamp(), count);
        }

        public void IncrementCounter(string key)
        {
           counters.Increment(key);
        }
    }

}
