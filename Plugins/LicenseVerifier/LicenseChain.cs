using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Performance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    /// A chain of licenses can consist of 
    /// a) 1 or more offline-domain licenses that may or may not enable different feature codes
    /// b) 1 or more ID licenses, and (optionally) a cached OR remote license for that ID
    /// </summary>
    class LicenseChain : ILicenseChain
    {

        /// <summary>
        /// The last time when we got the http response.
        /// </summary>
        public DateTimeOffset? Last200 { get; private set; }
        public DateTimeOffset? LastSuccess { get; private set; }
        public DateTimeOffset? Last404 { get; private set; }
        public DateTimeOffset? LastException { get; private set; }
        public DateTimeOffset? LastTimeout { get; private set; }

        /// <summary>
        /// Key is a hash of the license signature
        /// </summary>
        private ConcurrentDictionary<string, LicenseBlob> dict = new ConcurrentDictionary<string, LicenseBlob>();

        /// <summary>
        /// The fresh/local (not from cache) remote license
        /// </summary>
        private LicenseBlob remoteLicense = null;

        /// <summary>
        /// The current fetcher. Invalidated when urls are changed
        /// </summary>
        private LicenseFetcher fetcher;

        private LicenseManagerSingleton parent;

        public string Id { get; private set; }
        private string Secret { get; set; } = null;
        public bool IsRemote { get; set; } = false;

        public bool Shared { get; set; }

        /// <summary>
        /// Cache for .Licenses()
        /// </summary>
        private List<ILicenseBlob> cache;

        static string[] DefaultLicenseServers = new string[] {
            "https://s3-us-west-2.amazonaws.com/licenses.imazen.net/",
            "https://licenses-redirect.imazen.net/",
            "https://licenses.imazen.net/",
            "https://licenses2.imazen.net"
        };

        /// <summary>
        /// License Servers
        /// </summary>
        private string[] licenseServerStack = DefaultLicenseServers;

        // Actually needs an issue reciever? (or should *it* track?) And an HttpClient and Cache
        public LicenseChain(LicenseManagerSingleton parent, string license_id)
        {
            this.parent = parent;
            this.Id = license_id;
            LocalLicenseChange();
        }

        private Uri lastWorkingUri;

        private void OnFetchResult(string body, IEnumerable<LicenseFetcher.FetchResult> results)
        {
            if (body != null)
            {
                Last200 = parent.Clock.GetUtcNow();
                var license = parent.TryDeserialize(body, "remote server", false);
                if (license != null)
                {
                    var newId = license.Fields.Id;
                    if (newId == this.Id)
                    {
                        remoteLicense = license;
                        // Victory! (we're ignoring failed writes/duplicates)
                        parent.Cache.TryPut(fetcher.CacheKey, body);

                        LastSuccess = parent.Clock.GetUtcNow();

                        lastWorkingUri = results.Last().FullUrl;
                    }
                    else
                    {
                        parent.AcceptIssue(new Issue("Remote license file does not match. Please contact support@imageresizing.net", "Local: " + this.Id + "  Remote: " + newId, IssueSeverity.Error));

                    }
                }
                // TODO: consider logging a failed deserialization remotely
            }
            else
            {
                var key = fetcher.CacheKey;
                var licenseName = Id;

                if (results.All(r => r.HttpCode == 404 || r.HttpCode == 403))
                {
                    parent.AcceptIssue(new Issue("No such license (404/403): " + licenseName, String.Join("\n", results.Select(r => "HTTP 404/403 fetching " + RedactSecret(r.ShortUrl))), IssueSeverity.Error));
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
                    parent.AcceptIssue(fetcher.FirewallIssue(licenseName));
                    LastTimeout = parent.Clock.GetUtcNow();
                }
                else
                {
                    parent.AcceptIssue(new Issue("Exception(s) occured fetching license " + licenseName, RedactSecret(string.Join("\n", results.Select(r => String.Format("{0} {1}  LikelyTimeout: {2} Error: {3}", r.HttpCode, r.FullUrl, r.LikelyNetworkFailure, r.FetchError?.ToString())))), IssueSeverity.Error));
                    LastException = parent.Clock.GetUtcNow();
                }
            }
            LocalLicenseChange();
        }

        private string RedactSecret(string s)
        {
            return this.Secret != null ? s.Replace(this.Secret, "[redacted secret]") : s;
        }

        private void RecreateFetcher()
        {
            if (IsRemote)
            {
                fetcher = new LicenseFetcher(
                    parent.Clock,
                    () => parent.Cache,
                    () => parent.HttpClient,
                    OnFetchResult,
                    () => this.GetReportPairs(),
                    parent,
                    this.Id,
                    this.Secret,
                    this.licenseServerStack);
                fetcher.Heartbeat();
            }
        }


        /// <summary>
        /// Returns false if the blob is null, 
        /// if there were no license servers in the blob, 
        /// or if the servers were identical to what we already have.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        private bool TryUpdateLicenseServers(ILicenseBlob blob)
        {
            if (blob == null) return false;
            var oldStack = licenseServerStack ?? Enumerable.Empty<string>();
            var newList = blob.Fields.GetValidLicenseServers().ToArray();
            var newStack = newList.Concat(oldStack.Except(newList)).Take(10).ToArray();
            return !newStack.SequenceEqual(oldStack);
        }

        /// <summary>
        /// We have a layer of caching by string. This does not need to be fast. 
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
                    this.Secret = b.Fields.GetSecret();
                    this.IsRemote = true;

                    TryUpdateLicenseServers(b);
                    RecreateFetcher();
                    if (TryUpdateLicenseServers(CachedLicense()))
                    {
                        RecreateFetcher();
                    }
                }
                LocalLicenseChange();
            }
        }

        /// <summary>
        /// Returns null until a fresh license has been fetched (within process lifetime) 
        /// </summary>
        /// <returns></returns>
        public ILicenseBlob FetchedLicense()
        {
            return remoteLicense;
        }

        public ILicenseBlob CachedLicense()
        {
            if (fetcher != null)
            {
                var cached = this.parent.Cache.Get(fetcher.CacheKey);
                if (cached != null && cached.TryParseInt() == null)
                {
                    return parent.TryDeserialize(cached, "disk cache", false);
                }
            }
            return null;
        }


        private List<ILicenseBlob> CollectLicenses()
        {
            return Enumerable.Repeat(FetchedLicense() ?? CachedLicense(), 1)
                .Concat(dict.Values).Where(b => b != null).ToList();
        }


        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return fetcher?.GetAsyncTasksSnapshot() ?? Enumerable.Empty<Task>();
        }


        private void LocalLicenseChange()
        {
            cache = CollectLicenses();
            parent.FireLicenseChange();
        }

        public IEnumerable<ILicenseBlob> Licenses()
        {
            if (cache == null) LocalLicenseChange();
            return cache;
        }


        //public string Explain()
        //{
        //    string.Format("License fetch: last 200 {}, last 404 {}, last timeout {}, last exception {}, )
        //    // Explain history of license fetching
        //}


        private long lastBeatCount = 0;
        private IInfoAccumulator GetReportPairs()
        {
            if (!parent.ManagerGuid.HasValue)
            {
                parent.Heartbeat();
            }

            var beatCount = this.parent.HeartbeatCount;
            var netBeats = beatCount - lastBeatCount;
            lastBeatCount = beatCount;

            var firstHearbeat = (long)(parent.FirstHeartbeat.Value -
                    new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;

            var q = Configuration.Performance.GlobalPerf.Singleton.GetReportPairs();
            var prepending = q.WithPrepend(true);
            prepending.Add("total_heartbeats", beatCount.ToString());
            prepending.Add("new_heartbeats", netBeats.ToString());
            prepending.Add("first_heartbeat", firstHearbeat.ToString());
            prepending.Add("manager_id", this.parent.ManagerGuid.Value.ToString("D"));
            return q;
        }


        public override string ToString()
        {
            var cached = fetcher != null ? this.parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = (ILicenseBlob b) => b == this.remoteLicense ? "(fresh from license server)\n" + lastWorkingUri.ToString() + "\n" : b.Original == cached ? "(from cache)\n" : "";
            // TODO: this.Last200, this.Last404, this.LastException, this.LastSuccess, this.LastTimeout
            return RedactSecret(string.Format("License {0} (remote={1})\n    {2}\n", this.Id, this.IsRemote, string.Join("\n\n", Licenses().Select(b => freshness(b) + b.ToRedactedString())).Replace("\n", "\n    ")));
        }

        public string ToPublicString()
        {
            if (Licenses().All(b => !b.Fields.IsPublic()))
            {
                return "(license hidden)\n"; // None of these are public
            }
            var cached = fetcher != null ? this.parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = (ILicenseBlob b) => b == this.remoteLicense ? "(fresh)\n" : b.Original == cached ? "(from cache)\n" : "";

            return RedactSecret(string.Format("License {0}{1}\n{2}\n", this.Id,
                this.IsRemote ? " (remote)" : "",
                string.Join("\n\n", Licenses().Where(b => b.Fields.IsPublic()).Select(b => freshness(b) + b.ToRedactedString()))));
        }

        internal void Heartbeat()
        {
            fetcher?.Heartbeat();
        }
    }

   

}
