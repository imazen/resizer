using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Performance;
using ImageResizer.Plugins.Basic;
using ImageResizer.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    class LicenseFetcher
    {
        internal class FetchResult
        {
            public int? HttpCode { get; set; }
            public string ShortUrl { get; set; }
            public Uri FullUrl { get; set; }
            public Exception FetchError { get; set; }
            public Exception ParsingError { get; set; }
            public bool LikelyNetworkFailure { get; set; }
            public WebExceptionStatus? FailureKind { get; set; }
        }

        string id;
        string secret;

        Func<IInfoAccumulator> getQuerystring;
        Func<IPersistentStringCache> getCurrentCache;
        Func<HttpClient> getClient;
        Action<string, IEnumerable<FetchResult>> licenseResult;

        ImperfectDebounce regular;
        ImperfectDebounce error;

        IIssueReceiver sink;

        const long licenseFetchIntervalSeconds = 60 * 60;
        const long initialErrorIntervalSeconds = 2;
        const long errorMultiplier = 3;
        string[] baseUrls;

        static WebExceptionStatus[] networkFailures = new[] { WebExceptionStatus.ConnectFailure, WebExceptionStatus.KeepAliveFailure, WebExceptionStatus.NameResolutionFailure, WebExceptionStatus.PipelineFailure, WebExceptionStatus.ProxyNameResolutionFailure, WebExceptionStatus.ReceiveFailure, WebExceptionStatus.RequestProhibitedByProxy, WebExceptionStatus.SecureChannelFailure, WebExceptionStatus.SendFailure, WebExceptionStatus.ServerProtocolViolation, WebExceptionStatus.Timeout, WebExceptionStatus.TrustFailure };

        ConcurrentDictionary<object, Task> activeTasks = new ConcurrentDictionary<object, Task>();

        ILicenseClock clock;

        public LicenseFetcher(ILicenseClock clock, Func<IPersistentStringCache> getCurrentCache, Func<HttpClient> getClient, Action<string, IEnumerable<FetchResult>> licenseResult, Func<IInfoAccumulator> getQuerystring, IIssueReceiver sink, string licenseId, string licenseSecret, string[] baseUrls)
        {
            this.clock = clock;
            regular = new ImperfectDebounce(() => QueueLicenseFetch(false), licenseFetchIntervalSeconds, clock);
            error = new ImperfectDebounce(() => QueueLicenseFetch(true), 0, clock);
            this.getCurrentCache = getCurrentCache;
            this.getClient = getClient;
            this.getQuerystring = getQuerystring;
            this.licenseResult = licenseResult;
            this.baseUrls = baseUrls;
            id = licenseId;
            secret = licenseSecret;
            this.sink = sink;
        }

        public void Heartbeat()
        {
            regular.Heartbeat();
            error.Heartbeat();
        }

        void QueueLicenseFetch(bool fromErrorSchedule)
        {
            var key = new object();
            var task = Task.Run(async () =>
            {
                try
                {
                    await FetchLicense(null, fromErrorSchedule);
                    Task temp;
                    activeTasks.TryRemove(key, out temp);
                    foreach (var pair in activeTasks)
                    {
                        if (pair.Value.Status == TaskStatus.RanToCompletion ||
                            pair.Value.Status == TaskStatus.Faulted ||
                             pair.Value.Status == TaskStatus.Canceled)
                        {
                            activeTasks.TryRemove(pair.Key, out temp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    sink.AcceptIssue(new Issue("QueueLicenseFetch", "Potential bug", ex.ToString(), IssueSeverity.Error));
                }
            });

            activeTasks.TryAdd(key, task);
        }

        async Task FetchLicense(CancellationToken? cancellationToken, bool fromErrorSchedule)
        {
            var results = new List<FetchResult>();
            var queryString = ConstructQuerystring();

            foreach (string prefix in this.baseUrls)
            {
                var baseUrl = prefix + LicenseFetchPath + ".txt";
                Uri url = null;
                using (var tokenSource = new CancellationTokenSource())
                {
                    var token = cancellationToken ?? tokenSource.Token;
                    try
                    {
                        url = new Uri(baseUrl + queryString, UriKind.Absolute);

                        var httpResponse = await this.getClient().GetAsync(url, token);

                        var fetchResult = new FetchResult { HttpCode = ((int)httpResponse.StatusCode), FullUrl = url, ShortUrl = baseUrl };

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            var bodyBytes = await httpResponse.Content.ReadAsByteArrayAsync();
                            var bodyStr = System.Text.Encoding.UTF8.GetString(bodyBytes);

                            // Exit task early if canceled (process shutdown?)
                            if (token.IsCancellationRequested) return;

                            //Invoke the callback with *only* the successful result
                            if (InvokeResultCallback(bodyStr, new[] { fetchResult }))
                            {
                                ClearErrorDebounce();
                            }
                            else
                            {
                                // We add the error schedule even for callback failures
                                EnsureErrorDebounce();
                            }
                            return; // Network task succeeded
                        }
                        else
                        {
                            results.Add(fetchResult);
                        }
                    }
                    catch (HttpRequestException rex)
                    {
                        //Includes timeouts as taskCanceledException
                        var web = (rex.InnerException as WebException);
                        WebExceptionStatus? status = web?.Status;

                        var networkFailure = networkFailures.Any((s) => s == status);
                        results.Add(new FetchResult { FetchError = (Exception)web ?? rex, FullUrl = url, ShortUrl = baseUrl, FailureKind = status, LikelyNetworkFailure = networkFailure });
                    }
                    catch (TaskCanceledException ex)
                    {
                        results.Add(new FetchResult { FetchError = ex, FullUrl = url, ShortUrl = baseUrl, LikelyNetworkFailure = (ex.CancellationToken != token) });
                    }
                    catch (Exception e)
                    {
                        results.Add(new FetchResult { FetchError = e, FullUrl = url, ShortUrl = baseUrl });
                    }
                }

            }

            if (fromErrorSchedule)
            {
                AdjustErrorDebounce();
            }
            else
            {
                EnsureErrorDebounce();
            }

            InvokeResultCallback(null, results);
        }

        public IIssue FirewallIssue(string licenseName)
        {
            return new Issue("Check firewall; cannot reach Amazon S3 to validate license " + licenseName, "Check https://status.aws.amazon.com, and ensure the following URLs can be reached from this server: " + String.Join("\n", this.baseUrls.Select(s => s + "*")), IssueSeverity.Error);
        }

        private string LicenseFetchPath
        {
            get { return "v1/licenses/latest/" + secret; }

        }

        public string CacheKey
        {
            get
            {
                return id + "_" + Fnv1a32.HashToInt(secret).ToString("x");
            }
        }

        void EnsureErrorDebounce()
        {
            if (error.IntervalTicks == 0)
            {
                error.IntervalTicks = initialErrorIntervalSeconds * clock.TicksPerSecond;
            }
        }

        void AdjustErrorDebounce()
        {
            if (error.IntervalTicks > 0)
            {
                error.IntervalTicks *= errorMultiplier;
                error.IntervalTicks += (long)Math.Round(new Random().NextDouble() * (double)clock.TicksPerSecond / 2.0);
            }
            if (error.IntervalTicks > licenseFetchIntervalSeconds * clock.TicksPerSecond)
            {
                error.IntervalTicks = initialErrorIntervalSeconds * clock.TicksPerSecond;
            }
        }

        void ClearErrorDebounce()
        {
            error.IntervalTicks = 0;
        }

        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return activeTasks.Values;
        }

        string ConstructQuerystring()
        {
            IInfoAccumulator query;
            try
            {
                query = getQuerystring();
                query.WithPrepend(true).Add("license_id", this.id);
                return query.ToQueryString(3000);
            }
            catch (Exception ex)
            {
                sink.AcceptIssue(new Issue("LicenseManager", "Failed to collect querystring for license request", ex.ToString(), IssueSeverity.Warning));
            }

            return string.Format("?license_id={0}", this.id);
        }

        bool InvokeResultCallback(string body, IEnumerable<FetchResult> results)
        {
            try
            {
                licenseResult(body, results);
                return true;
            }
            catch (Exception ex)
            {
                sink.AcceptIssue(new Issue("LicenseManager", "Exception thrown in callback for FetchLicense", ex.ToString(), IssueSeverity.Error));
                return false;
            }
        }



        internal class Fnv1a32
        {
            public static uint HashToInt(string s)
            {
                return HashToInt(System.Text.Encoding.UTF8.GetBytes(s));
            }
            public static uint HashToInt(byte[] array)
            {
                var h = FnvOffsetBasis;
                for (var i = 0; i < array.Length; i++)
                {
                    unchecked
                    {
                        h ^= array[i];
                        h *= FnvPrime;
                    }
                }
                return h;
            }
            private const uint FnvPrime = unchecked(16777619);
            private const uint FnvOffsetBasis = unchecked(2166136261);
        }

        class ImperfectDebounce
        {
            Action callback;
            ILicenseClock clock;
            public long IntervalTicks { get; set; }

            long lastBegun = 0;
            object fireLock = new object[] { };

            public ImperfectDebounce(Action callback, long intervalSeconds, ILicenseClock clock)
            {
                this.callback = callback;
                this.clock = clock;
                IntervalTicks = clock.TicksPerSecond * intervalSeconds;
            }

            public void Heartbeat()
            {
                if (IntervalTicks > 0)
                {
                    var now = clock.GetTimestampTicks();
                    if (now - lastBegun >= IntervalTicks)
                    {
                        bool toFire = false;
                        lock (fireLock)
                        {
                            if (now - lastBegun >= IntervalTicks)
                            {
                                lastBegun = now;
                                toFire = true;
                            }
                        }
                        if (toFire) callback.Invoke();
                    }
                }
            }
        }
    }
}
