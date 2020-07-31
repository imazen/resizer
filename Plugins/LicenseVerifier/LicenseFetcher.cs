using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Performance;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    class LicenseFetcher
    {
        long LicenseFetchIntervalSeconds { get; } = 60 * 60;
        const long InitialErrorIntervalSeconds = 2;
        const long ErrorMultiplier = 3;

        static readonly WebExceptionStatus[] NetworkFailures = {
            WebExceptionStatus.ConnectFailure, WebExceptionStatus.KeepAliveFailure,
            WebExceptionStatus.NameResolutionFailure, WebExceptionStatus.PipelineFailure,
            WebExceptionStatus.ProxyNameResolutionFailure, WebExceptionStatus.ReceiveFailure,
            WebExceptionStatus.RequestProhibitedByProxy, WebExceptionStatus.SecureChannelFailure,
            WebExceptionStatus.SendFailure, WebExceptionStatus.ServerProtocolViolation, WebExceptionStatus.Timeout,
            WebExceptionStatus.TrustFailure
        };

        readonly ConcurrentDictionary<object, Task> activeTasks = new ConcurrentDictionary<object, Task>();
        readonly string[] baseUrls;

        readonly ILicenseClock clock;
        readonly ImperfectDebounce error;
        readonly Func<HttpClient> getClient;

        readonly Func<IInfoAccumulator> getInfo;

        readonly string id;
        readonly Action<string, IReadOnlyCollection<FetchResult>> licenseResult;

        readonly ImperfectDebounce regular;
        readonly string secret;

        readonly IIssueReceiver sink;

        string LicenseFetchPath => "v1/licenses/latest/" + secret;

        public string CacheKey => id + "_" + Fnv1a32.HashToInt(secret).ToString("x");

        public LicenseFetcher(ILicenseClock clock, Func<HttpClient> getClient,
                              Action<string, IReadOnlyCollection<FetchResult>> licenseResult,
                              Func<IInfoAccumulator> getInfo, IIssueReceiver sink, string licenseId,
                              string licenseSecret, string[] baseUrls, long? licenseFetchIntervalSeconds)
        {
            this.clock = clock;
            LicenseFetchIntervalSeconds = licenseFetchIntervalSeconds ?? LicenseFetchIntervalSeconds;
            regular = new ImperfectDebounce(() => QueueLicenseFetch(false), LicenseFetchIntervalSeconds, clock);
            error = new ImperfectDebounce(() => QueueLicenseFetch(true), 0, clock);
            this.getClient = getClient;
            this.getInfo = getInfo;
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
                try {
                    await FetchLicense(null, fromErrorSchedule);
                    Task temp;
                    activeTasks.TryRemove(key, out temp);
                    foreach (var pair in activeTasks) {
                        if (pair.Value.Status == TaskStatus.RanToCompletion ||
                            pair.Value.Status == TaskStatus.Faulted ||
                            pair.Value.Status == TaskStatus.Canceled) {
                            activeTasks.TryRemove(pair.Key, out temp);
                        }
                    }
                } catch (Exception ex) {
                    sink.AcceptIssue(
                        new Issue("QueueLicenseFetch", "Potential bug", ex.ToString(), IssueSeverity.Error));
                }
            });

            activeTasks.TryAdd(key, task);
        }

        async Task FetchLicense(CancellationToken? cancellationToken, bool fromErrorSchedule)
        {
            var results = new List<FetchResult>();
            var queryString = ConstructQuerystring();

            foreach (var prefix in baseUrls) {
                var baseUrl = prefix + LicenseFetchPath + ".txt";
                Uri url = null;
                using (var tokenSource = new CancellationTokenSource()) {
                    var token = cancellationToken ?? tokenSource.Token;
                    try {
                        url = new Uri(baseUrl + queryString, UriKind.Absolute);

                        var httpResponse = await getClient().GetAsync(url, token);

                        var fetchResult = new FetchResult {
                            HttpCode = (int) httpResponse.StatusCode,
                            FullUrl = url,
                            ShortUrl = baseUrl
                        };

                        if (httpResponse.IsSuccessStatusCode) {
                            var bodyBytes = await httpResponse.Content.ReadAsByteArrayAsync();
                            var bodyStr = System.Text.Encoding.UTF8.GetString(bodyBytes);

                            // Exit task early if canceled (process shutdown?)
                            if (token.IsCancellationRequested) {
                                return;
                            }

                            //Invoke the callback with *only* the successful result
                            if (InvokeResultCallback(bodyStr, new[] {fetchResult})) {
                                ClearErrorDebounce();
                            } else {
                                // We add the error schedule even for callback failures
                                EnsureErrorDebounce();
                            }
                            return; // Network task succeeded
                        }
                        results.Add(fetchResult);
                    } catch (HttpRequestException rex) {
                        //Includes timeouts as taskCanceledException
                        var web = rex.InnerException as WebException;
                        var status = web?.Status;

                        var networkFailure = NetworkFailures.Any(s => s == status);
                        results.Add(new FetchResult {
                            FetchError = (Exception) web ?? rex,
                            FullUrl = url,
                            ShortUrl = baseUrl,
                            FailureKind = status,
                            LikelyNetworkFailure = networkFailure
                        });
                    } catch (TaskCanceledException ex) {
                        results.Add(new FetchResult {
                            FetchError = ex,
                            FullUrl = url,
                            ShortUrl = baseUrl,
                            LikelyNetworkFailure = ex.CancellationToken != token
                        });
                    } catch (Exception e) {
                        results.Add(new FetchResult {FetchError = e, FullUrl = url, ShortUrl = baseUrl});
                    }
                }
            }

            if (fromErrorSchedule) {
                AdjustErrorDebounce();
            } else {
                EnsureErrorDebounce();
            }

            InvokeResultCallback(null, results);
        }

        public IIssue FirewallIssue(string licenseName) => FirewallIssue(licenseName, null);

        public IIssue FirewallIssue(string licenseName, FetchResult r)
        {
            string resultMessage = r != null ? $"\n\nHTTP={r.HttpCode}. Exception status={r.FailureKind}. Exception: {r.FetchError}" : "";
            return new Issue("Check firewall; cannot reach Amazon S3 to validate license " + licenseName,
                "Check https://status.aws.amazon.com, and ensure the following URLs can be reached from this server: " +
                string.Join("\n", baseUrls.Select(s => s + "*")) + resultMessage
                , IssueSeverity.Error);
        }

        void EnsureErrorDebounce()
        {
            if (error.IntervalTicks == 0) {
                error.IntervalTicks = InitialErrorIntervalSeconds * clock.TicksPerSecond;
            }
        }

        void AdjustErrorDebounce()
        {
            if (error.IntervalTicks > 0) {
                error.IntervalTicks *= ErrorMultiplier;
                error.IntervalTicks += (long) Math.Round(new Random().NextDouble() * clock.TicksPerSecond / 2.0);
            }
            if (error.IntervalTicks > LicenseFetchIntervalSeconds * clock.TicksPerSecond) {
                error.IntervalTicks = LicenseFetchIntervalSeconds * clock.TicksPerSecond;
            }
        }

        void ClearErrorDebounce() { error.IntervalTicks = 0; }

        public IEnumerable<Task> GetAsyncTasksSnapshot() => activeTasks.Values;

        string ConstructQuerystring()
        {
            try {
                var query = getInfo();
                query.WithPrepend(true).Add("license_id", id);
                return query.ToQueryString(7000);
            } catch (Exception ex) {
                sink.AcceptIssue(new Issue("LicenseManager", "Failed to collect querystring for license request",
                    ex.ToString(), IssueSeverity.Warning));
            }

            return $"?license_id={id}";
        }

        bool InvokeResultCallback(string body, IReadOnlyCollection<FetchResult> results)
        {
            try {
                licenseResult(body, results);
                return true;
            } catch (Exception ex) {
                sink.AcceptIssue(new Issue("LicenseManager", "Exception thrown in callback for FetchLicense",
                    ex.ToString(), IssueSeverity.Error));
                return false;
            }
        }

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


        // ReSharper disable once InconsistentNaming
        internal class Fnv1a32
        {
            const uint FnvPrime = 16777619;
            const uint FnvOffsetBasis = 2166136261;

            public static uint HashToInt(string s) => HashToInt(System.Text.Encoding.UTF8.GetBytes(s));

            public static uint HashToInt(byte[] array)
            {
                var h = FnvOffsetBasis;
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < array.Length; i++) {
                    unchecked {
                        h ^= array[i];
                        h *= FnvPrime;
                    }
                }
                return h;
            }
        }

        class ImperfectDebounce
        {
            readonly Action callback;
            readonly ILicenseClock clock;
            readonly object fireLock = new object[] { };

            long lastBegun;
            public long IntervalTicks { get; set; }

            public ImperfectDebounce(Action callback, long intervalSeconds, ILicenseClock clock)
            {
                this.callback = callback;
                this.clock = clock;
                IntervalTicks = clock.TicksPerSecond * intervalSeconds;
                lastBegun = -IntervalTicks;
            }

            public void Heartbeat()
            {
                if (IntervalTicks <= 0) {
                    return;
                }
                var now = clock.GetTimestampTicks();
                if (now - lastBegun >= IntervalTicks) {
                    var toFire = false;
                    lock (fireLock) {
                        if (now - lastBegun >= IntervalTicks) {
                            lastBegun = now;
                            toFire = true;
                        }
                    }
                    if (toFire) {
                        callback.Invoke();
                    }
                }
            }
        }
    }
}
