using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    ///     A license manager can serve as a per-process (per app-domain, at least) hub for license fetching
    /// </summary>
    class LicenseManagerSingleton : ILicenseManager, IIssueReceiver
    {
        /// <summary>
        ///     Connects all variants of each license to the relevant chain
        /// </summary>
        readonly ConcurrentDictionary<string, LicenseChain> aliases =
            new ConcurrentDictionary<string, LicenseChain>(StringComparer.Ordinal);

        /// <summary>
        ///     By license id/domain, lowercaseinvariant.
        /// </summary>
        readonly ConcurrentDictionary<string, LicenseChain> chains =
            new ConcurrentDictionary<string, LicenseChain>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        ///     The backing sink
        /// </summary>
        readonly IssueSink sink = new IssueSink("LicenseManager");

        /// <summary>
        ///     The set of all chains
        /// </summary>
        List<ILicenseChain> allCache = new List<ILicenseChain>();

        /// <summary>
        ///     The set of shared chains
        /// </summary>
        List<ILicenseChain> sharedCache = new List<ILicenseChain>();

        /// <summary>
        ///     The HttpClient all fetchers use
        /// </summary>
        public HttpClient HttpClient { get; private set; }

        /// <summary>
        ///     Source for timestamp information
        /// </summary>
        public ILicenseClock Clock { get; }


        public long HeartbeatCount { get; private set; }

        /// <summary>
        /// Heartbeats remaining to skip before fetching
        /// </summary>
        private long SkipHeartbeats { get; set; } = 0;
        /// <summary>
        /// How many initial heartbeats to skip if the last modified date of the disk cached license is recent (under 60m old)
        /// </summary>
        public long SkipHeartbeatsIfDiskCacheIsFresh { get;  set; } = 10;

        /// <summary>
        /// License chains consult this method before firing heartbeat events on fetchers
        /// </summary>
        /// <returns></returns>
        public bool AllowFetching()
        {
            if (SkipHeartbeats > 0)
            {
                return false;
            }
            if (HeartbeatCount > SkipHeartbeatsIfDiskCacheIsFresh && 
                SkipHeartbeats < 1)
            {
                return true;
            }
            if (SkipHeartbeatsIfDiskCacheIsFresh < 1)
            {
                return true; 
            }
            var fetcherCount = chains.Values.Count(c => c.IsRemote);
            if (fetcherCount > 0)
            {
                var now = DateTime.UtcNow;
                var oldestWrite = chains.Values.Where(c => c.IsRemote).Select(c => Cache.GetWriteTimeUtc(c.CacheKey)).Min();
                if (oldestWrite.HasValue && now.Subtract(oldestWrite.Value) < TimeSpan.FromMinutes(60))
                {
                    SkipHeartbeats = SkipHeartbeatsIfDiskCacheIsFresh;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public Guid? ManagerGuid { get; private set; }

        /// <summary>
        ///     Trusted public keys
        /// </summary>
        public IReadOnlyCollection<RSADecryptPublic> TrustedKeys { get; }

        public static ILicenseManager Singleton
        {
            get {
                return (ILicenseManager) CommonStaticStorage.GetOrAdd("licenseManager",
                    k => new LicenseManagerSingleton(ImazenPublicKeys.Production, new RealClock()));
            }
        }


        internal LicenseManagerSingleton(IReadOnlyCollection<RSADecryptPublic> trustedKeys, ILicenseClock clock): this(trustedKeys,clock, new PersistentGlobalStringCache()) { }
        internal LicenseManagerSingleton(IReadOnlyCollection<RSADecryptPublic> trustedKeys, ILicenseClock clock, IPersistentStringCache cache)
        {
            TrustedKeys = trustedKeys;
            Clock = clock;
            SetHttpMessageHandler(null, true);
            Cache = cache;
        }


        public void AcceptIssue(IIssue i) { ((IIssueReceiver) sink).AcceptIssue(i); }


        /// <summary>
        ///     The persistent cache for licenses
        /// </summary>
        public IPersistentStringCache Cache { get; set; } 

        public DateTimeOffset? FirstHeartbeat { get; private set; }


        public void Heartbeat()
        {
            if (FirstHeartbeat == null)
            {
                FirstHeartbeat = Clock.GetUtcNow();
            }
            if (ManagerGuid == null)
            {
                ManagerGuid = Guid.NewGuid();
            }
 
            HeartbeatCount++;

            if (SkipHeartbeats > 0)
            {
                SkipHeartbeats--;
            }

            foreach (var chain in chains.Values)
            {
                chain.Heartbeat();
            }
        }

        public void MonitorHeartbeat(Config c)
        {
            c.Pipeline.Heartbeat -= Pipeline_Heartbeat;
            c.Pipeline.Heartbeat += Pipeline_Heartbeat;
            Pipeline_Heartbeat(c.Pipeline, c);
        }

        public void MonitorLicenses(Config c)
        {
            c.Plugins.LicensePluginsChange -= Plugins_LicensePluginsChange;
            c.Plugins.LicensePluginsChange += Plugins_LicensePluginsChange;
            Plugins_LicensePluginsChange(null, c);
        }

        /// <summary>
        ///     Registers the license and (if relevant) signs it up for periodic updates from S3. Can also make existing private
        ///     licenses shared.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="access"></param>
        public ILicenseChain GetOrAdd(string license, LicenseAccess access)
        {
            var chain = aliases.GetOrAdd(license, GetChainFor);
            // We may want to share a previously unshared license
            if (chain != null && access.HasFlag(LicenseAccess.ProcessShareonly) && !chain.Shared) {
                chain.Shared = true;
                FireLicenseChange();
            }
            return chain;
        }

        public IReadOnlyCollection<ILicenseChain> GetSharedLicenses() => sharedCache;

        public IReadOnlyCollection<ILicenseChain> GetAllLicenses() => allCache;

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Cache as IIssueProvider;
            return cache == null ? sink.GetIssues() : sink.GetIssues().Concat(cache.GetIssues());
        }

        /// <summary>
        ///     Adds a weak-referenced handler to the LicenseChange event. Since this is (essentially) a static event,
        ///     weak references are important to allow listeners (and Config instances) to be garbage collected.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public LicenseManagerEvent AddLicenseChangeHandler<TTarget>(TTarget target,
                                                                    Action<TTarget, ILicenseManager> action)
        {
            var weakTarget = new WeakReference(target, false);
            LicenseManagerEvent handler = null;
            handler = mgr =>
            {
                var t = (TTarget) weakTarget.Target;
                if (t != null) {
                    action(t, this);
                } else {
                    LicenseChange -= handler;
                }
            };
            LicenseChange += handler;
            return handler;
        }

        /// <summary>
        ///     Removes the event handler created by AddLicenseChangeHandler
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public void RemoveLicenseChangeHandler(LicenseManagerEvent handler)
        {
            LicenseChange -= handler;
        }

        /// <summary>
        ///     When there is a material change or addition to a license chain (whether private or shared)
        /// </summary>
        event LicenseManagerEvent LicenseChange;

        void Pipeline_Heartbeat(IPipelineConfig sender, Config c) { Heartbeat(); }

        void Plugins_LicensePluginsChange(object sender, Config c)
        {
            foreach (var licenseString in c.Plugins.GetAll<ILicenseProvider>().SelectMany(p => p.GetLicenses())) {
                GetOrAdd(licenseString, c.Plugins.LicenseScope);
            }
            Heartbeat();
        }

        LicenseChain GetChainFor(string license)
        {
            var blob = TryDeserialize(license, "configuration", true);
            if (blob == null) {
                return null;
            }

            var chain = chains.GetOrAdd(blob.Fields.Id, k => new LicenseChain(this, k));
            chain.Add(blob);

            FireLicenseChange(); //Can only be triggered for new aliases anyway; we don't really need to debounce on signature
            return chain;
        }

        public void FireLicenseChange()
        {
            allCache = chains.Values.Cast<ILicenseChain>().ToList();
            sharedCache = allCache.Where(c => c.Shared).ToList();

            LicenseChange?.Invoke(this);
        }

        public void SetHttpMessageHandler(HttpMessageHandler handler, bool disposeHandler)
        {
            var newClient = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler);
            HttpClient = newClient;
        }


        /// <summary>
        ///     Returns a snapshot of
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Task> GetAsyncTasksSnapshot()
        {
            return chains.Values.SelectMany(chain => chain.GetAsyncTasksSnapshot());
        }

        /// <summary>
        ///     Returns the number of tasks that were waited for. Does not wait for new tasks that are scheduled during execution.
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
            try {
                blob = LicenseBlob.Deserialize(license);
            } catch (Exception ex) {
                AcceptIssue(new Issue("Failed to parse license (from " + licenseSource + "):",
                    WebConfigLicenseReader.TryRedact(license) + "\n" + ex, IssueSeverity.Error));
                return null;
            }
            if (!blob.VerifySignature(TrustedKeys, null)) {
                sink.AcceptIssue(new Issue(
                    "License " + blob.Fields.Id + " (from " + licenseSource +
                    ") has been corrupted or has not been signed with a matching private key.", IssueSeverity.Error));
                return null;
            }
            if (locallySourced && blob.Fields.MustBeFetched()) {
                sink.AcceptIssue(new Issue(
                    "This license cannot be installed directly; it must be fetched from a license server",
                    WebConfigLicenseReader.TryRedact(license), IssueSeverity.Error));
                return null;
            }
            return blob;
        }
    }
}
