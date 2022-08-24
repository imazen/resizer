using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Performance;
using ImageResizer.Plugins.Licensing;
using static ImageResizer.Plugins.LicenseVerifier.LicenseFetcher;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    ///     A chain of licenses can consist of
    ///     a) 1 or more offline-domain licenses that may or may not enable different feature codes
    ///     b) 1 or more ID licenses, and (optionally) a cached OR remote license for that ID
    /// </summary>
    class LicenseChain : ILicenseChain
    {
        static readonly string[] DefaultLicenseServers = {
            "https://s3-us-west-2.amazonaws.com/licenses.imazen.net/",
            "https://licenses-redirect.imazen.net/",
            "https://licenses.imazen.net/",
            "https://licenses2.imazen.net"
        };

        /// <summary>
        ///     Key is a hash of the license signature
        /// </summary>
        readonly ConcurrentDictionary<string, LicenseBlob> dict = new ConcurrentDictionary<string, LicenseBlob>();

        /// <summary>
        ///     License Servers
        /// </summary>
        string[] licenseServerStack = DefaultLicenseServers;

        int licenseIntervalMinutes = 60 * 60;

        readonly LicenseManagerSingleton parent;

        /// <summary>
        ///     Cache for .Licenses()
        /// </summary>
        List<ILicenseBlob> cache;

        /// <summary>
        ///     The current fetcher. Invalidated when URLs are changed
        /// </summary>
        LicenseFetcher fetcher;


        //public string Explain()
        //{
        //    string.Format("License fetch: last 200 {}, last 404 {}, last timeout {}, last exception {}, )
        //    // Explain history of license fetching
        //}


        long lastBeatCount;

        Uri lastWorkingUri;

        /// <summary>
        ///     The fresh/local (not from cache) remote license
        /// </summary>
        LicenseBlob remoteLicense;

        /// <summary>
        ///     The last time when we got the HTTP response.
        /// </summary>
        public DateTimeOffset? Last200 { get; private set; }

        public DateTimeOffset? LastSuccess { get; private set; }
        public DateTimeOffset? Last404 { get; private set; }
        public DateTimeOffset? LastException { get; private set; }
        public DateTimeOffset? LastTimeout { get; private set; }
        string Secret { get; set; }

        public string CacheKey => Id + "_" + Fnv1a32.HashToInt(Secret).ToString("x");


        // Actually needs an issue receiver? (or should *it* track?) And an HttpClient and Cache
        public LicenseChain(LicenseManagerSingleton parent, string licenseId)
        {
            this.parent = parent;
            Id = licenseId;
            LocalLicenseChange();
        }

        public string Id { get; }
        public bool IsRemote { get; set; }

        public bool Shared { get; set; }

        /// <summary>
        ///     Returns null until a fresh license has been fetched (within process lifetime)
        /// </summary>
        /// <returns></returns>
        public ILicenseBlob FetchedLicense() => remoteLicense;

        public ILicenseBlob CachedLicense()
        {
            if (fetcher == null)
            {
                return null;
            }
            var cached = parent.Cache.Get(fetcher.CacheKey);
            if (cached != null && cached.TryParseInt() == null)
            {
                return parent.TryDeserialize(cached, "disk cache", false);
            }
            return null;
        }

        public IEnumerable<ILicenseBlob> Licenses()
        {
            if (cache == null)
            {
                LocalLicenseChange();
            }
            return cache;
        }

        public string ToPublicString()
        {
            if (Licenses().All(b => !b.Fields.IsPublic()))
            {
                return "(license hidden)\n"; // None of these are public
            }
            var cached = fetcher != null ? parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = b => b == remoteLicense
                ? "(fresh)\n"
                : b.Original == cached
                    ? "(from cache)\n"
                    : "";

            return RedactSecret(
                $"License {Id}{(IsRemote ? " (remote)" : "")}\n{string.Join("\n\n", Licenses().Where(b => b.Fields.IsPublic()).Select(b => freshness(b) + b.ToRedactedString()))}\n");
        }

        void OnFetchResult(string body, IReadOnlyCollection<LicenseFetcher.FetchResult> results)
        {
            if (body != null)
            {
                Last200 = parent.Clock.GetUtcNow();
                var license = parent.TryDeserialize(body, "remote server", false);
                if (license != null)
                {
                    var newId = license.Fields.Id;
                    if (newId == Id)
                    {
                        remoteLicense = license;
                        // Victory! (we're ignoring failed writes/duplicates)
                        parent.Cache.TryPut(fetcher.CacheKey, body);

                        LastSuccess = parent.Clock.GetUtcNow();

                        lastWorkingUri = results.Last().FullUrl;
                    }
                    else
                    {
                        parent.AcceptIssue(new Issue(
                            "Remote license file does not match. Please contact support@imageresizing.net",
                            "Local: " + Id + "  Remote: " + newId, IssueSeverity.Error));
                    }
                }
                // TODO: consider logging a failed deserialization remotely
            }
            else
            {
                var licenseName = Id;

                if (results.All(r => r.HttpCode == 404 || r.HttpCode == 403))
                {
                    parent.AcceptIssue(new Issue("No such license (404/403): " + licenseName,
                        string.Join("\n", results.Select(r => "HTTP 404/403 fetching " + RedactSecret(r.ShortUrl))),
                        IssueSeverity.Error));
                    // No such subscription key.. but don't downgrade it if exists.
                    var cachedString = parent.Cache.Get(fetcher.CacheKey);
                    int temp;
                    if (cachedString == null || !int.TryParse(cachedString, out temp))
                    {
                        parent.Cache.TryPut(fetcher.CacheKey, results.First().HttpCode.ToString());
                    }
                    Last404 = parent.Clock.GetUtcNow();
                }
                else if (results.All(r => r.LikelyNetworkFailure))
                {
                    // Network failure. Make sure the server can access the remote server
                    parent.AcceptIssue(fetcher.FirewallIssue(licenseName, results.FirstOrDefault()));
                    LastTimeout = parent.Clock.GetUtcNow();
                }
                else
                {
                    parent.AcceptIssue(new Issue("Exception(s) occurred fetching license " + licenseName,
                        RedactSecret(string.Join("\n",
                            results.Select(r =>
                                $"{r.HttpCode} {r.FullUrl}  LikelyTimeout: {r.LikelyNetworkFailure} Error: {r.FetchError?.ToString()}"))),
                        IssueSeverity.Error));
                    LastException = parent.Clock.GetUtcNow();
                }
            }
            LocalLicenseChange();
        }

        string RedactSecret(string s) => Secret != null ? s?.Replace(Secret, "[redacted secret]") : s;

        void RecreateFetcher()
        {
            if (!IsRemote)
            {
                return;
            }
            fetcher = new LicenseFetcher(
                parent.Clock,
                () => parent.HttpClient,
                OnFetchResult,
                GetReportPairs,
                parent,
                Id,
                Secret,
                licenseServerStack,
                (long?)licenseIntervalMinutes);

            if (parent.AllowFetching())
            {
                fetcher.Heartbeat();
            }
        }


        string[] GetLicenseServers(ILicenseBlob blob)
        {
            var oldStack = licenseServerStack ?? new string[0];
            var newList = blob.Fields.GetValidLicenseServers().ToArray();
            return newList.Concat(oldStack.Except(newList)).Take(10).ToArray();
        }

        /// <summary>
        ///     Returns false if the blob is null,
        ///     if there were no license servers in the blob,
        ///     or if the servers were identical to what we already have.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        bool TryUpdateLicenseServersInfo(ILicenseBlob blob)
        {
            if (blob == null)
            {
                return false;
            }
            var interval = blob.Fields.CheckLicenseIntervalMinutes();
            var intervalChanged = interval != null && interval != licenseIntervalMinutes;

            var oldStack = licenseServerStack ?? new string[0];
            var newStack = GetLicenseServers(blob);
            var stackChanged = !newStack.SequenceEqual(oldStack);


            if (stackChanged)
            {
                licenseServerStack = newStack;
            }
            if (intervalChanged)
            {
                licenseIntervalMinutes = interval.Value;
            }
            return stackChanged || intervalChanged;
        }

        /// <summary>
        ///     We have a layer of caching by string. This does not need to be fast.
        /// </summary>
        /// <param name="b"></param>
        public void Add(LicenseBlob b)
        {
            // Prevent duplicate signatures
            if (dict.TryAdd(BitConverter.ToString(b.Signature), b))
            {
                //New/unique - ensure fetcher is created
                if (b.Fields.IsRemotePlaceholder())
                {
                    Secret = b.Fields.GetSecret();
                    IsRemote = true;

                    TryUpdateLicenseServersInfo(b);
                    RecreateFetcher();
                }
                LocalLicenseChange();
            }
        }


        List<ILicenseBlob> CollectLicenses()
        {
            return Enumerable.Repeat(FetchedLicense() ?? CachedLicense(), 1)
                             .Concat(dict.Values)
                             .Where(b => b != null)
                             .ToList();
        }


        public IEnumerable<Task> GetAsyncTasksSnapshot() =>
            fetcher?.GetAsyncTasksSnapshot() ?? Enumerable.Empty<Task>();


        void LocalLicenseChange()
        {
            if (TryUpdateLicenseServersInfo(FetchedLicense() ?? CachedLicense()))
            {
                RecreateFetcher();
            }
            cache = CollectLicenses();
            parent.FireLicenseChange();
        }

        IInfoAccumulator GetReportPairs()
        {
            if (!parent.ManagerGuid.HasValue)
            {
                parent.Heartbeat();
            }

            var beatCount = parent.HeartbeatCount;
            var netBeats = beatCount - lastBeatCount;
            lastBeatCount = beatCount;

            var firstHeartbeat = (long)(parent.FirstHeartbeat.GetValueOrDefault() -
                                        new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;

            var q = GlobalPerf.Singleton.GetReportPairs();
            var prepending = q.WithPrepend(true);
            prepending.Add("total_heartbeats", beatCount.ToString());
            prepending.Add("new_heartbeats", netBeats.ToString());
            prepending.Add("first_heartbeat", firstHeartbeat.ToString());
            prepending.Add("manager_id", parent.ManagerGuid?.ToString("D"));
            return q;
        }


        public override string ToString()
        {
            var cached = fetcher != null ? parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = b => b == remoteLicense
                ? "(fresh from license server)\n"
                : b.Original == cached
                    ? "(from cache)\n"
                    : "";
            // TODO: this.Last200, this.Last404, this.LastException, this.LastSuccess, this.LastTimeout
            return RedactSecret(
                $"License {Id} (remote={IsRemote})\n    {string.Join("\n\n", Licenses().Select(b => freshness(b) + b.ToRedactedString())).Replace("\n", "\n    ")}\n");
        }

        public string LastFetchUrl() { return RedactSecret(lastWorkingUri?.ToString()); }
        internal void Heartbeat()
        {
            if (parent.AllowFetching())
            {
                fetcher?.Heartbeat();
            }
        }
    }
}
