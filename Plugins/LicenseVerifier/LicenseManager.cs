using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.LicenseVerifier
{

    public delegate void LicenseManagerEvent(ILicenseManager mgr);

    /// <summary>
    /// When multiple paid plugins and license keys are involved, this interface allows the deduplication of effort and centralized license access.
    /// </summary>
    public interface ILicenseManager : IIssueProvider
    {
        /// <summary>
        /// Persistent cache 
        /// </summary>
        IPersistentStringCache Cache { get; set; }

        /// <summary>
        ///  Must be called often to fetch remote licenses appropriately. Not resource intensive; call for every image request.
        /// </summary>
        void Heartbeat();

        /// <summary>
        /// The license manager will add a handler to notice license changes on the config. It will also process current licenses on the config.
        /// </summary>
        /// <param name="c"></param>
        void Monitor(Config c);

        /// <summary>
        /// Register a license key (if it isn't already), and return the inital chain (or null, if the license is invalid)
        /// </summary>
        /// <param name="license"></param>
        ILicenseChain Add(string license, LicenseAccess access);

        /// <summary>
        /// Returns all shared license chains (a chain is shared if any relevant license is marked shared)
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseChain> GetSharedLicenses();

        /// <summary>
        /// Returns all license chains, both shared and private (for diagnostics/reporting)
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseChain> GetAllLicenses();

        /// <summary>
        /// Fired when a license has been updated or added.
        /// </summary>
        event LicenseManagerEvent LicenseChange;
    }

    public interface ILicenseChain
    {
        /// <summary>
        /// Plan ID or domain name (lowercase invariant)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Whether license chain is shared app-wide
        /// </summary>
        bool Shared { get; }

        /// <summary>
        /// Can return fresh, cached, and inline licenes
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseBlob> Licenses();

        /// <summary>
        /// Returns null until a fresh license has been fetched (within process lifetime) 
        /// </summary>
        /// <returns></returns>
        ILicenseBlob GetFreshRemoteLicense();

        ///// <summary>
        ///// Returns null until a fresh license has been fetched (within process lifetime) 
        ///// </summary>
        ///// <returns></returns>
        //ILicenseBlob GetCachedLicense();
    }

    /// <summary>
    /// Provides license UTF-8 bytes and signature
    /// </summary>
    public interface ILicenseBlob
    {
        byte[] GetSignature();
        byte[] GetDataUTF8();
        string Original { get; }
        ILicenseDetails GetParsed();
    }

    public interface ILicenseDetails
    {
        IReadOnlyDictionary<string, string> GetPairs();
        string Get(string key);
        DateTime? Issued { get; }
        DateTime? Expires { get; }
        IEnumerable<string> GetFeatures();
    }


    class LicenseManagerSingleton : ILicenseManager, IIssueReceiver
    {

        /// <summary>
        /// Connects all variants of each license to the relevant chain
        /// </summary>
        ConcurrentDictionary<string, LicenseChain> aliases = new ConcurrentDictionary<string, LicenseChain>(StringComparer.Ordinal);
        /// <summary>
        /// By license id/domain, lowercaseinvariant
        /// </summary>
        ConcurrentDictionary<string, LicenseChain> chains = new ConcurrentDictionary<string, LicenseChain>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The set of shared chains
        /// </summary>
        private List<ILicenseChain> sharedCache = new List<ILicenseChain>();
        /// <summary>
        /// When there is a material change or addition to a license chain (whether private or shared)
        /// </summary>
        public event LicenseManagerEvent LicenseChange;

        HttpClient hc = new HttpClient();
        IssueSink sink = new IssueSink("LicenseManager");


        public IPersistentStringCache Cache { get; set; } = new PeristentGlobalStringCache();


        public static ILicenseManager Singleton
        {
            get
            {
                return (ILicenseManager)CommonStaticStorage.GetOrAdd("licenseManager", (k) => new LicenseManagerSingleton());
            }
        }

        public void Monitor(Config c)
        {
            c.Plugins.LicensingChange -= Plugins_LicensingChange;
            c.Plugins.LicensingChange += Plugins_LicensingChange;
            ScanConfig(c);
            Heartbeat();
        }

        private void Plugins_LicensingChange(object sender, Config forConfig)
        {
            ScanConfig(forConfig);
            Heartbeat();
        }

        private void ScanConfig(Config c)
        {
            foreach (string l in c.Plugins.GetAll<ILicenseProvider>().SelectMany(p => p.GetLicenses()))
            {
                Add(l, c.Plugins.LicenseScope);
            }
        }
        /// <summary>
        /// Registers the license and (if relevant) signs it up for periodic updates from S3. Can also make existing private licenses shared.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="access"></param>
        public ILicenseChain Add(string license, LicenseAccess access)
        {
            var chain = aliases.GetOrAdd(license, (s) => GetChainFor(s));
            if (chain != null && (access.HasFlag(LicenseAccess.ProcessShareonly)) && !chain.Shared)
            {
                chain.Shared = true;
                OnLicenseChange();
            }
            return chain;
        }

        LicenseChain GetChainFor(string license)
        {
            var blob = TryDeserialize(license, "Failed to parse license:");
            if (blob == null) return null;

            var parsed = blob.GetParsed();

            var id = parsed.Get("Id") ?? parsed.Get("Domain");

            if (string.IsNullOrWhiteSpace(id))
            {
                //Bad licenses are logged to the app-wide sink
                sink.AcceptIssue(new Issue("The provided license is invalid because it has no ID or Domain.", license, IssueSeverity.ConfigurationError));
                return null;
            }

            var key = id.Trim().ToLowerInvariant();

            var chain = chains.GetOrAdd(key, (k) => new LicenseChain(this, k));
            chain.Add(blob);

            OnLicenseChange(); //Can only be triggered for new aliases anyway; we don't really need to debounce on signature
            return chain;
        }

        private void OnLicenseChange()
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
            var cache = Cache as PeristentGlobalStringCache;

            if (cache == null)
            {
                return sink.GetIssues();
            }else
            {
                return sink.GetIssues().Concat(cache.GetIssues());
            }
        }
        public void SetHttpMessageHandler(HttpMessageHandler handler, bool disposeHandler)
        {
            hc = new HttpClient(handler, disposeHandler);
            //TODO: set any timeout values, etc.
        }
        public HttpClient Client { get { return hc; } }

        public void Heartbeat()
        {
            foreach (var chain in chains.Values)
            {
                chain.Heartbeat();
            }
        }

        public void AcceptIssue(IIssue i)
        {
            ((IIssueReceiver)sink).AcceptIssue(i);
        }

        internal LicenseManagerSingleton()
        {
        }

        /// <summary>
        /// Returns a snapshot of 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return chains.Values.SelectMany((chain) => chain.GetAsyncTasksSnapshot());
        }

        public void WaitForTasks()
        {
            Task.WaitAll(this.GetAsyncTasksSnapshot().ToArray());
        }

        // Let config instances decide whether to connect to the app-domain-wide license manager (default), or if they want to only use licenses they expose. 
        // Either way, the singleton will allow lookup by user string. 

        // When a new user string is made available, it will be immediately queued for fetching via FireOnce


        //Config instances will have individual validation, but most will be precomputed. 

        public LicenseBlob TryDeserialize(string s, string errorSubject)
        {
            try
            {
                return LicenseBlob.Deserialize(s);
            }
            catch (Exception ex)
            {
                AcceptIssue(new Issue(errorSubject, s + "\n" + ex.ToString(), IssueSeverity.Error));
            }
            return null;
        }

    }


    class LicenseChain : ILicenseChain
    {

        /// <summary>
        /// The last time when we got the http response.
        /// </summary>
        public DateTime? Last200 { get; private set; }
        public DateTime? LastSuccess { get; private set; }
        public DateTime? Last404 { get; private set; }
        public DateTime? LastException { get; private set; }
        public DateTime? LastTimeout { get; private set; }



        private void OnFetchResult(string body, IEnumerable<LicenseFetcher.FetchResult> results)
        {
            if (body != null)
            {
                Last200 = DateTime.UtcNow;
                var parsed = parent.TryDeserialize(body, "Failed to parse remote license:");
                if (parsed != null)
                {
                    remoteLicense = parsed;
                    // Victory! (we're ignoring failed writes/duplicates)
                    parent.Cache.TryPut(fetcher.CacheKey, body);

                    LastSuccess = DateTime.UtcNow;
                }
            }
            else
            {
                var key = fetcher.CacheKey;
                var licenseName = Id;

                if (results.All(r => r.HttpCode == 404 || r.HttpCode == 403))
                {
                    // No such subscription key.. but don't downgrade it if exists.
                    var old = parent.Cache.Get(fetcher.CacheKey);
                    int a;
                    if (old != null && int.TryParse(old, out a))
                    {
                        parent.AcceptIssue(new Issue("Remote license file missing: " + licenseName, String.Join("\n", results.Select(r => "HTTP 404/403 fetching " + r.ShortUrl)), IssueSeverity.Error));
                    }
                    else
                    {
                        parent.Cache.TryPut(fetcher.CacheKey, results.First().HttpCode.ToString());
                        parent.AcceptIssue(new Issue("No such license exists: " + licenseName, String.Join("\n", results.Select(r => "HTTP 404/403 fetching " + r.ShortUrl)), IssueSeverity.Error));
                    }
                    Last404 = DateTime.UtcNow;
                }
                else if (results.All(r => r.LikelyNetworkFailure))
                {
                    // Network failure. Make sure the server can access the remote server
                    parent.AcceptIssue(fetcher.FirewallIssue(licenseName));
                    LastTimeout = DateTime.UtcNow;
                }
                else
                {
                    parent.AcceptIssue(new Issue("Exception(s) occured fetching license " + licenseName, String.Join("\n", results.Select(r => String.Format("{0} {1}  LikelyTimeout: {2} Error: {3}", r.HttpCode, r.FullUrl, r.LikelyNetworkFailure, r.FetchError?.ToString()))), IssueSeverity.Error));
                    LastException = DateTime.UtcNow;
                }
            }
            LocalLicenseChange();
        }

        // Actually needs an issue reciever? (or should *it* track?) And an HttpClient and Cache
        public LicenseChain(LicenseManagerSingleton parent, string id)
        {
            this.parent = parent;
            this.Id = id;
            LocalLicenseChange();
        }

        private ConcurrentDictionary<string, LicenseBlob> dict = new ConcurrentDictionary<string, LicenseBlob>();

        private LicenseBlob remoteLicense = null;

        /// <summary>
        /// We have a layer of caching by string, this does not need to be fast. 
        /// 
        /// </summary>
        /// <param name="b"></param>
        public void Add(LicenseBlob b)
        {
            // Prevent duplicates
            var key = BitConverter.ToString(b.GetSignature());
            if (dict.TryAdd(key, b))
            {
                //New/unique - ensure fetcher is created
                var secret = b.GetParsed().Get("Secret");
                if (secret != null && fetcher == null)
                {
                    fetcher = new LicenseFetcher(() => parent.Cache, () => parent.Client, OnFetchResult, () => "", parent, Id, secret);
                    fetcher.Heartbeat();
                }
                LocalLicenseChange();
            }
        }

        /// <summary>
        /// Returns null until a fresh license has been fetched (within process lifetime) 
        /// </summary>
        /// <returns></returns>
        public ILicenseBlob GetFreshRemoteLicense()
        {
            return remoteLicense;
        }

        private List<LicenseBlob> CollectLicenses()
        {
            //Aggregate from cache, dict, and  .remoteLicense
            var list = new List<LicenseBlob>(2 + dict.Count);
            if (fetcher != null)
            {
                var cached = this.parent.Cache.Get(fetcher.CacheKey);
                if (remoteLicense != null)
                {
                    list.Add(remoteLicense);
                }
                if (cached != remoteLicense?.Original)
                {
                    int r;
                    if (!int.TryParse(cached, out r))
                    {
                        var blob = parent.TryDeserialize(cached, "Failed to parse cached license:");
                        if (blob != null) list.Add(blob);
                    }
                }
            }
            list.AddRange(dict.Values);
            return list;
        }
        private void LocalLicenseChange()
        {
            cache = CollectLicenses();
        }

        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return fetcher?.GetAsyncTasksSnapshot() ?? Enumerable.Empty<Task>();
        }

        private LicenseFetcher fetcher;

        private LicenseManagerSingleton parent;

        public string Id { get; private set; }

        public bool Shared { get; set; }

        private List<LicenseBlob> cache;
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

        internal void Heartbeat()
        {
            fetcher?.Heartbeat();
        }
    }



    class Debounce
    {
        public Debounce(Action a, long intervalSeconds)
        {
            this.a = a;
            SetIntervalSeconds(intervalSeconds);
        }
        Action a;

        public void SetIntervalSeconds(long seconds)
        {
            IntervalTicks = Stopwatch.Frequency * seconds;
        }

        public long IntervalTicks { get; set; }

        long lastBegun = 0;

        object fireLock = new object[] { };
        public void Heartbeat()
        {
            if (IntervalTicks > 0)
            {
                var now = Stopwatch.GetTimestamp();
                if (now - lastBegun >= IntervalTicks)
                {
                    lock (fireLock)
                    {
                        if (now - lastBegun >= IntervalTicks)
                        {
                            lastBegun = now;
                            a.Invoke();
                        }
                    }
                }
            }
        }
    }


    class LicenseFetcher
    {
        string id;
        string secret;

        Func<string> getQuerystring;
        Func<IPersistentStringCache> getCurrentCache;
        Func<HttpClient> getClient;
        Action<string, IEnumerable<FetchResult>> licenseResult;

        Debounce regular;
        Debounce error;

        IIssueReceiver sink;

        const long regularInterval = 60 * 60;
        const long initialErrorInterval = 2;
        const long errorMultiplier = 3;

        public LicenseFetcher(Func<IPersistentStringCache> getCurrentCache, Func<HttpClient> getClient, Action<string, IEnumerable<FetchResult>> licenseResult, Func<string> getQuerystring, IIssueReceiver sink, string licenseId, string licenseSecret)
        {
            regular = new Debounce(() => QueueLicenseFetch(false), regularInterval);
            error = new Debounce(() => QueueLicenseFetch(true), 0);
            this.getCurrentCache = getCurrentCache;
            this.getClient = getClient;
            this.getQuerystring = getQuerystring;
            this.licenseResult = licenseResult;
            id = licenseId;
            secret = licenseSecret;
            this.sink = sink;
        }

        private string LicenseFetchPath
        {
            get { return secret; }

        }

        public string CacheKey
        {
            get
            {
                return LicenseFetchPath;
            }
        }

        public void Heartbeat()
        {
            regular.Heartbeat();
            error.Heartbeat();
        }

        void AdjustErrorDebounce()
        {
            if (error.IntervalTicks == 0)
            {
                error.IntervalTicks = regularInterval;
            }
            else if (error.IntervalTicks > 0)
            {
                error.IntervalTicks *= errorMultiplier;
            }
            if (error.IntervalTicks > regularInterval)
            {
                error.IntervalTicks = 0;
            }
        }
        void ClearErrorDebounce()
        {
            error.IntervalTicks = 0;
        }

        static string[] BaseURLs = new string[] { "https://s3-us-west-2.amazonaws.com/imazen-licenses/v1/licenses/latest/" };
        static string[] suffixes = new string[] { ".txt" };


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

        static WebExceptionStatus[] networkFailures = new[] { WebExceptionStatus.ConnectFailure, WebExceptionStatus.KeepAliveFailure, WebExceptionStatus.NameResolutionFailure, WebExceptionStatus.PipelineFailure, WebExceptionStatus.ProxyNameResolutionFailure, WebExceptionStatus.ReceiveFailure, WebExceptionStatus.RequestProhibitedByProxy, WebExceptionStatus.SecureChannelFailure, WebExceptionStatus.SendFailure, WebExceptionStatus.ServerProtocolViolation, WebExceptionStatus.Timeout, WebExceptionStatus.TrustFailure };

        ConcurrentDictionary<object, Task> activeTasks = new ConcurrentDictionary<object, Task>();

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

        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return activeTasks.Values;
        }

        async Task FetchLicense(CancellationToken? cancellationToken, bool fromErrorSchedule)
        {
            var results = new List<FetchResult>();
            var query = getQuerystring();
            foreach (string prefix in BaseURLs)
            {
                foreach (string suffix in suffixes)
                {
                    var baseUrl = prefix + LicenseFetchPath + suffix;
                    Uri url = null;
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var token = cancellationToken ?? tokenSource.Token;
                        try
                        {
                            url = new Uri(baseUrl + query, UriKind.Absolute);
                            var response = await this.getClient().GetAsync(url, token);
                            int code = ((int)response.StatusCode);
                            if (response.IsSuccessStatusCode)
                            {
                                var bytes = await response.Content.ReadAsByteArrayAsync();
                                if (token.IsCancellationRequested)
                                {
                                    return;
                                }
                                var bodyStr = System.Text.Encoding.UTF8.GetString(bytes);
                                try
                                {
                                    licenseResult(bodyStr, new[] { new FetchResult { HttpCode = code, FullUrl = url, ShortUrl = baseUrl } });
                                }
                                catch (Exception ex)
                                {
                                    sink.AcceptIssue(new Issue("LicenseManager", "Exception thrown in callback for FetchLicense", ex.ToString(), IssueSeverity.Error));
                                }
                                ClearErrorDebounce();

                                return;
                            }
                            else
                            {
                                results.Add(new FetchResult { HttpCode = code, FullUrl = url, ShortUrl = baseUrl });
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
            }
            if (fromErrorSchedule) ClearErrorDebounce();

            try
            {
                licenseResult(null, results);
            }
            catch (Exception ex)
            {
                sink.AcceptIssue(new Issue("LicenseManager", "Exception thrown in callback for FetchLicense", ex.ToString(), IssueSeverity.Error));
            }
        }

        public IIssue FirewallIssue(string licenseName)
        {
            return new Issue("Check firewall; cannot reach Amazon S3 to validate license " + licenseName, "Check https://status.aws.amazon.com, and ensure the following URLs can be reached from this server: " + String.Join("\n", BaseURLs.Select(s => s + "*")), IssueSeverity.Error);
        }

    }

}
