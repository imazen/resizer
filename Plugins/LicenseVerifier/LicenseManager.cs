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
    /// <summary>
    /// A license manager can serve as a per-process (per app-domain, at least) hub for license fetching
    /// </summary>
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
}
