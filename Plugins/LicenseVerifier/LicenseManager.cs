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

    class LicenseManagerSingleton : ILicenseManager, IIssueReceiver
    {

        /// <summary>
        /// Connects all variants of each license to the relevant chain
        /// </summary>
        ConcurrentDictionary<string, LicenseChain> aliases = new ConcurrentDictionary<string, LicenseChain>(StringComparer.Ordinal);

        /// <summary>
        /// By license id/domain, lowercaseinvariant. 
        /// </summary>
        ConcurrentDictionary<string, LicenseChain> chains = new ConcurrentDictionary<string, LicenseChain>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The set of shared chains
        /// </summary>
        private List<ILicenseChain> sharedCache = new List<ILicenseChain>();

        /// <summary>
        /// When there is a material change or addition to a license chain (whether private or shared)
        /// </summary>
        private event LicenseManagerEvent LicenseChange;


        /// <summary>
        /// The backing sink 
        /// </summary>
        IssueSink sink = new IssueSink("LicenseManager");


        /// <summary>
        /// The persistent cache for licenses 
        /// </summary>
        public IPersistentStringCache Cache { get; set; } = new PeristentGlobalStringCache();

        /// <summary>
        /// The HttpClient all fetchers use
        /// </summary>
        public HttpClient HttpClient { get; private set; }

        /// <summary>
        /// Source for timestamp information
        /// </summary>
        public ILicenseClock Clock { get; private set; }

        public DateTimeOffset? FirstHeartbeat { get; private set; }


        public long HeartbeatCount { get; private set; }
        public Guid? ManagerGuid { get; private set; }

        /// <summary>
        /// Trusted public keys
        /// </summary>
        public IEnumerable<RSADecryptPublic> TrustedKeys { get; private set; }


        internal LicenseManagerSingleton(IEnumerable<RSADecryptPublic> trustedKeys, ILicenseClock clock)
        {
            TrustedKeys = trustedKeys;
            Clock = clock;
            SetHttpMessageHandler(null, true);
        }

        public static ILicenseManager Singleton
        {
            get
            {
                return (ILicenseManager)CommonStaticStorage.GetOrAdd("licenseManager", (k) => new LicenseManagerSingleton(ImazenPublicKeys.Production, new RealClock()));
            }
        }

        public void Heartbeat()
        {
            if (FirstHeartbeat == null) FirstHeartbeat = Clock.GetUtcNow();
            if (ManagerGuid == null) ManagerGuid = Guid.NewGuid();
            HeartbeatCount++;
            foreach (var chain in chains.Values)
            {
                chain.Heartbeat();
            }
        }

        public void AcceptIssue(IIssue i)
        {
            ((IIssueReceiver)sink).AcceptIssue(i);
        }

        public void MonitorHeartbeat(Config c)
        {
            c.Pipeline.Heartbeat -= Pipeline_Heartbeat;
            c.Pipeline.Heartbeat += Pipeline_Heartbeat;
            Pipeline_Heartbeat(c.Pipeline, c);
        }

        private void Pipeline_Heartbeat(IPipelineConfig sender, Config c)
        {
            Heartbeat();
        }

        public void MonitorLicenses(Config c)
        {
            c.Plugins.LicensePluginsChange -= Plugins_LicensePluginsChange;
            c.Plugins.LicensePluginsChange += Plugins_LicensePluginsChange;
            Plugins_LicensePluginsChange(null, c);
        }

        private void Plugins_LicensePluginsChange(object sender, Config c)
        {
            foreach (string licenseString in c.Plugins.GetAll<ILicenseProvider>().SelectMany(p => p.GetLicenses()))
            {
                GetOrAdd(licenseString, c.Plugins.LicenseScope);
            }
            Heartbeat();
        }

        /// <summary>
        /// Registers the license and (if relevant) signs it up for periodic updates from S3. Can also make existing private licenses shared.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="access"></param>
        public ILicenseChain GetOrAdd(string license, LicenseAccess access)
        {
            var chain = aliases.GetOrAdd(license, (s) => GetChainFor(s));
            if (chain != null && (access.HasFlag(LicenseAccess.ProcessShareonly)) && !chain.Shared)
            {
                chain.Shared = true;
                FireLicenseChange();
            }
            return chain;
        }

        LicenseChain GetChainFor(string license)
        {
            var blob = TryDeserialize(license, "configuration", true);
            if (blob == null) return null;

            var chain = chains.GetOrAdd(blob.Fields().Id, (k) => new LicenseChain(this, k));
            chain.Add(blob);

            FireLicenseChange(); //Can only be triggered for new aliases anyway; we don't really need to debounce on signature
            return chain;
        }

        public void FireLicenseChange()
        {
            sharedCache = chains.Values.Where((c) => c.Shared).Cast<ILicenseChain>().ToList();
            LicenseChange?.Invoke(this);
        }

        public IEnumerable<ILicenseChain> GetSharedLicenses()
        {
            return sharedCache;
        }

        public IEnumerable<ILicenseChain> GetAllLicenses()
        {
            return chains.Values.Cast<ILicenseChain>();
        }

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Cache as IIssueProvider;
            return cache == null ? sink.GetIssues() : sink.GetIssues().Concat(cache.GetIssues());
        }


        public void SetHttpMessageHandler(HttpMessageHandler handler, bool disposeHandler)
        {
            HttpClient newClient;
            if (handler == null)
            {
                newClient = new HttpClient();
            }
            else
            {
                newClient = new HttpClient(handler, disposeHandler);
            }
            //newClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
            //    GlobalPerf.Singleton.GetUserAgent(Assembly.GetAssembly(this.GetType())));

            HttpClient = newClient;
        }
   

        /// <summary>
        /// Returns a snapshot of 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return chains.Values.SelectMany((chain) => chain.GetAsyncTasksSnapshot());
        }

        /// <summary>
        /// Returns the number of tasks that were waited for. Does not wait for new tasks that are scheduled during execution.
        /// </summary>
        /// <returns></returns>
        public int WaitForTasks()
        {
            var tasks = GetAsyncTasksSnapshot().ToArray();
            Task.WaitAll(tasks);
            return tasks.Length;
        }

        public LicenseBlob TryDeserialize(string license, string licenseSource, bool locallySourced)
        {
            LicenseBlob blob;
            try
            {
                blob = LicenseBlob.Deserialize(license);
            }
            catch (Exception ex)
            {
                AcceptIssue(new Issue("Failed to parse license (from " + licenseSource + "):", 
                    WebConfigLicenseReader.TryRedact(license) + "\n" + ex.ToString(), IssueSeverity.Error));
                return null;
            }
            if (!blob.VerifySignature(this.TrustedKeys, null))
            {
                sink.AcceptIssue(new Issue("License " + blob.Fields().Id + " (from " + licenseSource + ") has been corrupted or has not been signed with a matching private key.", IssueSeverity.Error));
                return null;
            }
            if (locallySourced && blob.Fields().MustBeFetched())
            {
                sink.AcceptIssue(new Issue("This license cannot be installed directly; it must be fetched from a license server", 
                    WebConfigLicenseReader.TryRedact(license), IssueSeverity.Error));
                return null;
            }
            return blob;
        }

        /// <summary>
        /// Adds a weak-referenced handler to the LicenseChange event. Since this is (essentially) a static event,
        /// weak references are important to allow listeners (and Config instances) to be garbage collected.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public LicenseManagerEvent AddLicenseChangeHandler<TTarget>(TTarget target, Action<TTarget, ILicenseManager> action)
        {
            WeakReference weakTarget = new WeakReference(target, false);
            LicenseManagerEvent handler = null;
            handler = (mgr) =>
            {
                TTarget t = (TTarget)weakTarget.Target;
                if (t != null)
                    action(t, this);
                else
                    LicenseChange -= handler;
            };
            LicenseChange += handler;
            return handler;
        }

        /// <summary>
        /// Removes the event handler created by AddLicenseChangeHandler
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public void RemoveLicenseChangeHandler(LicenseManagerEvent handler)
        {
            LicenseChange -= handler;
        }

    }


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
                    var newId = license.Fields().Id;
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
            return s.Replace(this.Secret, "[redacted secret]");
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
            var newList = blob.Fields().GetValidLicenseServers().ToArray();
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
            if (dict.TryAdd(BitConverter.ToString(b.Signature()), b))
            {
                //New/unique - ensure fetcher is created
                if (b.Fields().IsRemotePlaceholder())
                {
                    this.Secret = b.Fields().GetSecret();
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
                    new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0,TimeSpan.Zero)).TotalSeconds;

            var q = Configuration.Performance.GlobalPerf.Singleton.GetReportPairs();
            var prepending = q.WithPrepend(true);
            prepending.Add("total_heartbeats",beatCount.ToString());
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
            return RedactSecret(string.Format("License {0} (remote={1})\n    {2}\n", this.Id, this.IsRemote, string.Join("\n\n",  Licenses().Select(b =>  freshness(b) +  b.ToRedactedString())).Replace("\n", "\n    ")));
        }

        public string ToPublicString()
        {
            if (Licenses().All(b => !b.Fields().IsPublic()))
            {
                return "(license hidden)\n"; // None of these are public
            }
            var cached = fetcher != null ? this.parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = (ILicenseBlob b) => b == this.remoteLicense ? "(fresh)\n" : b.Original == cached ? "(from cache)\n" : "";

            return RedactSecret(string.Format("License {0}{1}\n{2}\n", this.Id, 
                this.IsRemote ? " (remote)" : "", 
                string.Join("\n\n", Licenses().Where(b => b.Fields().IsPublic()).Select(b => freshness(b) + b.ToRedactedString()))));
        }

        internal void Heartbeat()
        {
            fetcher?.Heartbeat();
        }
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
            IntervalTicks = clock.TicksPerSecond  * intervalSeconds;
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
            var task = Task.Run(async () => {
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
                            }else {
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


    class RealClock : ILicenseClock
    {
        public long TicksPerSecond { get; } = Stopwatch.Frequency;
        public long GetTimestampTicks()
        {
            return Stopwatch.GetTimestamp();
        }
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
        public DateTimeOffset? GetBuildDate()
        {
            try
            {
                return this.GetType().Assembly.GetCustomAttributes(typeof(BuildDateAttribute), false)?.Select(a => ((BuildDateAttribute)a).ValueDate).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
        public DateTimeOffset? GetAssemblyWriteDate()
        {
            var path = this.GetType().Assembly.Location;
            try
            {
                return System.IO.File.Exists(path) ? new DateTimeOffset?(System.IO.File.GetLastWriteTimeUtc(this.GetType().Assembly.Location)) : null;
            }
            catch
            {
                return null;
            }
        }
    }

}
