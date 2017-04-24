using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    public class GlobalPerf
    {
        public static GlobalPerf Singleton { get; } = new GlobalPerf();
    }
    public string GetQuerystring() { return "";  }
}

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

        HttpClient hc;
        IssueSink sink = new IssueSink("LicenseManager");


        public IPersistentStringCache Cache { get; set; } = new PeristentGlobalStringCache();


        public static ILicenseManager Singleton
        {
            get
            {
                return (ILicenseManager)CommonStaticStorage.GetOrAdd("licenseManager", (k) => new LicenseManagerSingleton(ImazenPublicKeys.Production));
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

        private string GetUserAgent()
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            var asb = new StringBuilder();
            object[] attrs;
            try
            {
                AssemblyName assemblyName = new AssemblyName(a.FullName);
                asb.Append(assemblyName.Name);

                attrs = a.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
                if (attrs != null && attrs.Length > 0) asb.Append(" File: " + ((AssemblyFileVersionAttribute)attrs[0]).Version);

                asb.Append(" Assembly: " + assemblyName.Version.ToString().PadRight(15));

                attrs = a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attrs != null && attrs.Length > 0) asb.Append(" Info: " + ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion);

                attrs = a.GetCustomAttributes(typeof(CommitAttribute), false);
                if (attrs != null && attrs.Length > 0) asb.Append("  Commit: " + ((CommitAttribute)attrs[0]).Value);
            }
            catch (Exception)
            {
                asb.Append("(failed to read assembly attributes)");
            }
            return asb.ToString();

        }
        public void SetHttpMessageHandler(HttpMessageHandler handler, bool disposeHandler)
        {
            HttpClient newClient;
            if (handler == null)
            {
                newClient = new HttpClient();
            }else { 

                newClient = new HttpClient(handler, disposeHandler);
            }
            newClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetUserAgent());

            hc = newClient;
            //TODO: set any timeout values, etc.
        }
        public HttpClient Client { get { return hc; } }

        public DateTime? FirstHeartbeat { get; private set; }
        public long HeartbeatCount { get; private set; }
        public Guid? ManagerGuid { get; private set; }

        public void Heartbeat()
        {
            if (FirstHeartbeat == null) FirstHeartbeat = DateTime.UtcNow;
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

        public IEnumerable<RSADecryptPublic> TrustedKeys { get; private set;  }

        internal LicenseManagerSingleton(IEnumerable<RSADecryptPublic> trustedKeys)
        {
            TrustedKeys = trustedKeys;
            SetHttpMessageHandler(null, true);
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
        private bool IsRemote { get; set; } = false;

        public bool Shared { get; set; }

        /// <summary>
        /// Cache for .Licenses()
        /// </summary>
        private List<LicenseBlob> cache;

        private string licenseServersValue = null;
        private string[] licenseServers = null;

        // Actually needs an issue reciever? (or should *it* track?) And an HttpClient and Cache
        public LicenseChain(LicenseManagerSingleton parent, string id)
        {
            this.parent = parent;
            this.Id = id;
            LocalLicenseChange();
        }

        private Uri lastWorkingUri;

        private void OnFetchResult(string body, IEnumerable<LicenseFetcher.FetchResult> results)
        {
            if (body != null)
            {
                Last200 = DateTime.UtcNow;
                var parsed = parent.TryDeserialize(body, "Failed to parse remote license:");
                if (parsed != null)
                {
                    var newId = parsed.GetParsed().Get("Id")?.Trim()?.ToLowerInvariant();

                    if (newId == this.Id)
                    {
                        remoteLicense = parsed;
                        // Victory! (we're ignoring failed writes/duplicates)
                        parent.Cache.TryPut(fetcher.CacheKey, body);

                        LastSuccess = DateTime.UtcNow;

                        lastWorkingUri = results.Last().FullUrl;
                        ConsiderUrlUpdate(parsed);
                    }else
                    {
                        parent.AcceptIssue(new Issue("Remote license file does not match. Please contact ImageResizer support.","Local: " + this.Id + "  Remote: " + newId, IssueSeverity.Error));

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


     
        private void RecreateFetcher()
        {
            if (this.IsRemote && this.Secret != null)
            {
                fetcher = new LicenseFetcher(() => parent.Cache, () => parent.Client, OnFetchResult, () => this.GetQuery(), parent, this.Id, this.Secret, this.licenseServers);
                fetcher.Heartbeat();
            }
        }


        private void ConsiderUrlUpdate(ILicenseBlob blob)
        {
            var newValue = blob.GetParsed().Get("LicenseServers");
            // Dedupe
            if (newValue != null & newValue != this.licenseServersValue)
            {
                // Cryptographically verififed?
                if (new LicenseValidator().Validate(blob, parent.TrustedKeys, null))
                {
                    var filteredNewList = newValue.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Where((s) =>
                    {
                        Uri t;
                        return Uri.TryCreate(s, UriKind.Absolute, out t) && t.Scheme == "https";
                    }).ToArray();

                    if (filteredNewList.Length > 0)
                    {
                        this.licenseServersValue = newValue;
                        this.licenseServers = filteredNewList;
                        RecreateFetcher();
                    }
                }
            }
        }
        
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
                var kind = b.GetParsed().Get("Kind");
                if (secret != null && "id".Equals(kind, StringComparison.OrdinalIgnoreCase))
                {
                    this.Secret = secret;
                    this.IsRemote = true;
                    ConsiderUrlUpdate(b);
                    if (fetcher == null)
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


        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return fetcher?.GetAsyncTasksSnapshot() ?? Enumerable.Empty<Task>();
        }


        private void LocalLicenseChange()
        {
            cache = CollectLicenses();
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
        private string GetQuery()
        {
            if (!parent.ManagerGuid.HasValue)
            {
                parent.Heartbeat();
            }
            
            var beatCount = this.parent.HeartbeatCount;
            var netBeats = beatCount - lastBeatCount;
            lastBeatCount = beatCount;

            var firstHearbeat = (long)(TimeZoneInfo.ConvertTimeToUtc(parent.FirstHeartbeat.Value) -
                    new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
            return "mgr_id=" + this.parent.ManagerGuid.Value.ToString("D") + "&beats_total=" + beatCount + "&beats=" + netBeats +
                "&first_beat=" + firstHearbeat + "&"  + Configuration.Performance.GlobalPerf.Singleton.GetQuerystring();
            
        }
 

        public override string ToString()
        {
            var cached = fetcher != null ? this.parent.Cache.Get(fetcher.CacheKey) : null;
            Func<ILicenseBlob, string> freshness = (ILicenseBlob b) => b == this.remoteLicense ? "(fresh from license server)\n" + this.lastWorkingUri.ToString() + "\n" : b.Original == cached ? "(from cache)\n" : "";
            // TODO: this.Last200, this.Last404, this.LastException, this.LastSuccess, this.LastTimeout
            return string.Format("License {0} (remote={1})\n    {2}\n", this.Id, this.IsRemote, string.Join("\n\n",  Licenses().Select(b =>  freshness(b) +  b.ToRedactedString())).Replace("\n", "\n    "));
        }
        internal void Heartbeat()
        {
            fetcher?.Heartbeat();
        }
    }

    internal static class LicenseBlobExtensions
    {
        internal static string ToRedactedString(this ILicenseBlob b)
        {
            return string.Join("\n", b.GetParsed().GetPairs().Select(pair => "secret".Equals(pair.Key, StringComparison.OrdinalIgnoreCase) ? string.Format("{0}: ****redacted****", pair.Key) : string.Format("{0}: {1}", pair.Key, pair.Value)));
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
        string[] baseUrls;

       
        public LicenseFetcher(Func<IPersistentStringCache> getCurrentCache, Func<HttpClient> getClient, Action<string, IEnumerable<FetchResult>> licenseResult, Func<string> getQuerystring, IIssueReceiver sink, string licenseId, string licenseSecret, string[] baseUrls)
        {
            regular = new Debounce(() => QueueLicenseFetch(false), regularInterval);
            error = new Debounce(() => QueueLicenseFetch(true), 0);
            this.getCurrentCache = getCurrentCache;
            this.getClient = getClient;
            this.getQuerystring = getQuerystring;
            this.licenseResult = licenseResult;
            this.baseUrls = baseUrls ?? DefaultBaseURLs;
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
                return id + "_" + Fnv1a32.HashToInt(secret).ToString("x");
            }
        }

        public void Heartbeat()
        {
            regular.Heartbeat();
            error.Heartbeat();
        }

        void EnsureErrorDebounce()
        {
            if (error.IntervalTicks == 0)
            {
                error.IntervalTicks = initialErrorInterval * Stopwatch.Frequency;
            }
        }

        void AdjustErrorDebounce()
        {
            if (error.IntervalTicks > 0)
            {
                error.IntervalTicks *= errorMultiplier;
                error.IntervalTicks += (long)Math.Round(new Random().NextDouble() * (double)Stopwatch.Frequency / 2.0);
            }
            if (error.IntervalTicks > regularInterval * Stopwatch.Frequency)
            {
                error.IntervalTicks = initialErrorInterval * Stopwatch.Frequency;
            }
        }

        void ClearErrorDebounce()
        {
            error.IntervalTicks = 0;
        }

        static string[] DefaultBaseURLs = new string[] { "https://s3-us-west-2.amazonaws.com/imazen-licenses/v1/licenses/latest/" };


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

            
            var query = "?id=" + this.id + "&" + getQuerystring();
            foreach (string prefix in this.baseUrls)
            {

                var baseUrl = prefix + LicenseFetchPath + ".txt";
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
                                return; // Process is shutting down (most likely)
                            }
                            var bodyStr = System.Text.Encoding.UTF8.GetString(bytes);
                            try
                            {
                                licenseResult(bodyStr, new[] { new FetchResult { HttpCode = code, FullUrl = url, ShortUrl = baseUrl } });
                                ClearErrorDebounce();
                            }
                            catch (Exception ex)
                            {
                                sink.AcceptIssue(new Issue("LicenseManager", "Exception thrown in callback for FetchLicense", ex.ToString(), IssueSeverity.Error));
                                EnsureErrorDebounce();
                            }

                            return; // DONE, http worked - processing may not have
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

            if (fromErrorSchedule)
            {
                AdjustErrorDebounce();
            }
            else
            {
                EnsureErrorDebounce();
            }

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
            return new Issue("Check firewall; cannot reach Amazon S3 to validate license " + licenseName, "Check https://status.aws.amazon.com, and ensure the following URLs can be reached from this server: " + String.Join("\n", this.baseUrls.Select(s => s + "*")), IssueSeverity.Error);
        }

    }
    public sealed class Fnv1a32 : HashAlgorithm
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

        private uint hash;

        public Fnv1a32()
        {
            this.Reset();
        }

        public override void Initialize()
        {
            this.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (var i = ibStart; i < cbSize; i++)
            {
                unchecked
                {
                    this.hash ^= array[i];
                    this.hash *= FnvPrime;
                }
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(this.hash);
        }

        private void Reset()
        {
            this.hash = FnvOffsetBasis;
        }
    }

}
