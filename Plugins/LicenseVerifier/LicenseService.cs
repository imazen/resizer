// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Numerics;
using ImageResizer.Configuration.Issues;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    /// Transient issues are stored within the class; permanent issues are stored in the ctor provided sink
    /// </summary>
    class LicenseComputation : IssueSink, IDiagnosticsProvider
    {
        /// <summary>
        /// If a placeholder license doesn't specify NetworkGraceMinutes, we use this value.
        /// </summary>
        const int DefaultNetworkGraceMinutes = 3;

        Config c;
        ILicenseManager mgr;
        IClock clock;
        IEnumerable<RSADecryptPublic> trustedKeys;
        IIssueReceiver permanentIssues;

        DomainLookup domainLookup;
        IList<ILicenseChain> chains;

        bool AllDomainsLicensed = false;

        bool EverythingDenied = false;

        IDictionary<string, bool> knownDomainStatus = null;

        ConcurrentDictionary<string, bool> unknownDomains = new ConcurrentDictionary<string, bool>();

        public DateTimeOffset? ComputationExpires { get; private set; }

        public LicenseComputation(Config c, IEnumerable<RSADecryptPublic> trustedKeys, IIssueReceiver permanentIssueSink, ILicenseManager mgr, IClock clock): base("LicenseComputation")
        {
            this.c = c;
            this.trustedKeys = trustedKeys;
            this.permanentIssues = permanentIssueSink;
            this.clock = clock;
            this.mgr = mgr;
            if (mgr.FirstHeartbeat == null)
            {
                throw new ArgumentException("ILicenseManager.Heartbeat() must be called before LicenseComputation.new");
            }

            // What features are installed on this instance?
            // For a license to be OK, it must have one of each of this nested list;
            IEnumerable<IEnumerable<string>> pluginFeaturesUsed = c.Plugins.GetAll<ILicensedPlugin>().Select(p => p.LicenseFeatureCodes).ToList();

            // Create or fetch all relevant license chains; ignore the empty/invalid ones, they're logged to the manager instance
            chains = c.Plugins.GetAll<ILicenseProvider>()
                .SelectMany(p => p.GetLicenses())
                .Select((str) => mgr.GetOrAdd(str, c.Plugins.LicenseScope))
                .Where((x) => x != null && x.Licenses().Count() > 0)
                .Concat(c.Plugins.LicenseScope.HasFlag(LicenseAccess.ProcessReadonly) ? mgr.GetSharedLicenses() : Enumerable.Empty<ILicenseChain>())
                .Distinct()
                .ToList();

            // This computation (at minimum) expires when we cross an expires or issued date
            // We'll update for grace periods separately
            ComputationExpires = chains.SelectMany(chain => chain.Licenses())
                .SelectMany(b => new[] { b.Fields().Expires, b.Fields().Issued })
                .Where(date => date != null).OrderBy(d => d).FirstOrDefault(d => d > clock.GetUtcNow());

            // Set up our domain map/normalize/search manager
            domainLookup = new DomainLookup(c, permanentIssueSink, chains);


            // Check for tampering via interfaces
            if (chains.Any(chain => chain.Licenses().Any(b => !b.Revalidate(trustedKeys))))
            {
                EverythingDenied = true;
                permanentIssueSink.AcceptIssue(new Issue("Licenses failed to revalidate; please contact support@imageresizing.net", IssueSeverity.Error));
            }

            // Perform the final computations
            ComputeStatus(pluginFeaturesUsed);
        }

        bool IsLicenseExpired(ILicenseDetails details, IClock clock){
            return details.Expires != null && details.Expires < clock.GetUtcNow();
        }
        bool HasLicenseBegun(ILicenseDetails details, IClock clock){
            return details.Issued != null && details.Issued > clock.GetUtcNow();
        }
        bool IsLicenseRevoked(ILicenseDetails details){
            return "false".Equals(details.Get("Valid"), StringComparison.OrdinalIgnoreCase);
        }
        public DateTimeOffset? GetBuildDate(){
            return clock.GetBuildDate() ?? clock.GetAssemblyWriteDate();
        }
        public bool IsBuildDateNewer(DateTimeOffset? value)
        {
            var buildDate = GetBuildDate();
            return buildDate != null &&
                   value != null &&
                buildDate > value;
        }

        bool IsLicenseValid(ILicenseBlob b)
        {
            var details = b.Fields();
            if (IsLicenseExpired(details, clock)){
                permanentIssues.AcceptIssue(new Issue("License " + details.Id +  " has expired.",  b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }

            if (HasLicenseBegun(details, clock)){
                permanentIssues.AcceptIssue(new Issue("License " + details.Id +  " was issued in the future; check system clock ",  b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }

            if (IsBuildDateNewer(details.SubscriptionExpirationDate)
            {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id +
                        " covers ImageResizer versions prior to " + details.SubscriptionExpirationDate.Value.ToString("D") +
                                                      ", but you are using a build dated " + GetBuildDate().Value.ToString("D"), b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }
            if (IsLicenseRevoked(details))
            {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " is no longer active", b.ToRedactedString(), IssueSeverity.Warning));
            }
            return true;
        }

        bool IsPendingLicense(ILicenseChain chain)
        {
            return chain.IsRemote && chain.Licenses().All(b => b.Fields().IsRemotePlaceholder());
        }

        void ProcessPendingLicense(ILicenseChain chain)
        {
            // If the placeholder license fails its own constraints, don't add a grace period
            if (chain.Licenses().All(b => !IsLicenseValid(b)))
            {
                return;
            }

            int? graceMinutes = chain.Licenses().Where(b => IsLicenseValid(b))
                                           .Select(b => b.Fields().Get("NetworkGraceMinutes").TryParseInt())
                                           .OrderByDescending(v => v).FirstOrDefault() ?? DefaultNetworkGraceMinutes;


            // Success will automatically replace this instance. Warn immediately.
            var expires = mgr.FirstHeartbeat.Value.AddMinutes(graceMinutes.Value);
            var thirtySeconds = mgr.FirstHeartbeat.Value.AddSeconds(30);
            if (expires < clock.GetUtcNow())
            {
                this.AcceptIssue(new Issue("Grace period of " + graceMinutes.Value + "m expired for license " + chain.Id,
                            string.Format("License {0} was not found in the disk cache and could not be retrieved from the remote server within {1} minutes.", chain.Id, graceMinutes.Value), IssueSeverity.Error));
            }
            else if (thirtySeconds < clock.GetUtcNow())
            {
                AllDomainsLicensed = true;
                ComputationExpires = expires;

                this.AcceptIssue(new Issue("Grace period of " + graceMinutes.Value + "m will expire for license " + chain.Id + " at " + expires.ToString("HH:mm") + " on " + expires.ToString("D"),
                            string.Format("License {0} was not found in the disk cache and could not be retrieved from the remote server.", chain.Id, graceMinutes.Value), IssueSeverity.Error));
            }
            else
            {

                AllDomainsLicensed = true;
                ComputationExpires = thirtySeconds;

                this.AcceptIssue(new Issue("Fetching license " + chain.Id + " (not found in disk cache).",
                    "Network grace period expires in " + graceMinutes.Value + " minutes", IssueSeverity.Warning));
            }

        }


        void ComputeStatus(IEnumerable<IEnumerable<string>> requireOneFromEach)
        {
            this.AllDomainsLicensed = false;

            foreach (var chain in chains.Where(chain => IsPendingLicense(chain)))
            {
                ProcessPendingLicense(chain);
            }

            var validLicenses = chains.Where(chain => !IsPendingLicense(chain))
                .SelectMany(chain => chain.Licenses())
                .Where(b => IsLicenseValid(b));

            AllDomainsLicensed = AllDomainsLicensed || validLicenses.Any(b => b.Fields().GetAllDomains().Count() == 0);

            knownDomainStatus = validLicenses.SelectMany(
                b => b.Fields().GetAllDomains()
                        .SelectMany(domain => b.Fields().GetFeatures()
                                .Select(feature => new KeyValuePair<string, string>(domain, feature))))
                    .GroupBy(pair => pair.Key, pair => pair.Value, (k, v) => new KeyValuePair<string, IEnumerable<string>>(k, v))
                    .Select(pair => new KeyValuePair<string, bool>(pair.Key, requireOneFromEach.All(set => set.Intersect(pair.Value, StringComparer.OrdinalIgnoreCase).Count() > 0)))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        }


        public bool LicensedForAll()
        {
            if (EverythingDenied){
                return false;
            } else {
                return AllDomainsLicensed;
            }
        }
        public bool LicensedForRequestUrl(Uri url)
        {
            if (EverythingDenied) {
                return false;
            } else if (AllDomainsLicensed) {
                return true;
            } else if (domainLookup.KnownDomainCount > 0) {
                var host = url.DnsSafeHost;
                var knownDomain = domainLookup.FindKnownDomain(host);
                if (knownDomain != null)
                {
                    return knownDomainStatus[knownDomain];
                }
                else
                {
                    return unknownDomains.TryAdd(domainLookup.TrimLowerInvariant(host), false);
                }
            } else {
                return false;
            }
        }

        public string ProvideDiagnostics()
        {
            var sb = new StringBuilder();
            sb.Append("\n----------------\nLicense status for active features: ");
            if (EverythingDenied)
            {
                sb.AppendLine("contact support");
            }else if (AllDomainsLicensed)
            {
                sb.AppendLine("Valid for all domains");
            }
            else if (knownDomainStatus.Count > 0)
            {
                sb.AppendFormat("Valid for {0} domains, invalid for {1} domains, not covered for {3} domains:\n", 
                    knownDomainStatus.Count(pair => pair.Value),
                    knownDomainStatus.Count(pair => !pair.Value),
                    unknownDomains.Count);
                sb.AppendFormat("Valid: {0}\nInvalid: {1}\nNot covered: {2}\n",
                    string.Join(", ", knownDomainStatus.Where(pair => pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", knownDomainStatus.Where(pair => !pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", unknownDomains.Select(pair => pair.Key)));
            } else
            {
                sb.AppendLine("No valid licenses");
            }
            sb.AppendLine();
            sb.Append(domainLookup.ExplainNormalizations());
            if (chains.Count > 0)
            {
                sb.AppendLine("Licenses for this Config instance:\n");
                sb.AppendLine(string.Join("\n", chains.Select(c => c.ToString())));
            }
            var others = mgr.GetAllLicenses().Except(chains);
            if (others.Count() > 0)
            {
                sb.AppendLine("Licenses only used by other Config instances in this procces:\n");
                sb.AppendLine(string.Join("\n", others.Select(c => c.ToString())));
            }

            var mgrs = mgr as LicenseManagerSingleton;
            if (mgrs != null)
            {
                sb.AppendFormat("{0} heartbeat events. First {1} ago.\n", mgrs.HeartbeatCount, mgrs.FirstHeartbeat == null ? "(never)" : clock.GetUtcNow().Subtract(mgrs.FirstHeartbeat.Value).ToString());
            }
            sb.AppendLine("\n----------------\n");
            return sb.ToString();
        }
    }

    class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider
    {
        private ILicenseManager mgr;
        private Config c = null;
        private LicenseComputation cache = null;
        IClock Clock { get; set;  } = new RealClock();

        public LicenseEnforcer() : this(LicenseManagerSingleton.Singleton) { }
        public LicenseEnforcer(ILicenseManager mgr) { this.mgr = mgr; }

        private LicenseComputation Result
        {
            get
            {
                if (cache?.ComputationExpires != null && cache.ComputationExpires.Value < Clock.GetUtcNow())
                {
                    cache = null;
                }
                return cache = cache ?? new LicenseComputation(this.c, ImazenPublicKeys.All, c.configurationSectionIssues, this.mgr, Clock);
            }
        }

        protected override RequestedAction PreFlushChanges(ImageState s)
        {
            if (s.destBitmap != null && ShouldDisplayDot(c, s))
            {
                int w = s.destBitmap.Width, dot_w = 3, h = s.destBitmap.Height, dot_h = 3;
                //Don't duplicate writes.
                if (s.destBitmap.GetPixel(w - 1, h - 1) != Color.Red)
                {
                    if (w > dot_w && h > dot_h)
                    {
                        for (int y = 0; y < dot_h; y++)
                            for (int x = 0; x < dot_w; x++)
                                s.destBitmap.SetPixel(w - 1 - x, h - 1 - y, Color.Red);
                    }
                }
            }
            return RequestedAction.None;
        }


        private bool ShouldDisplayDot(Config c, ImageState s)
        {
            // For now, we only add dots during an active HTTP request. 
            if (c == null || c.configurationSectionIssues == null || System.Web.HttpContext.Current == null) return false;

            return Result.LicensedForRequestUrl(System.Web.HttpContext.Current?.Request?.Url);
        }

        public IPlugin Install(Config c)
        {
            this.c = c;

            // Ensure the LicenseManager can respond to heartbeats and license/licensee plugin additions
            mgr.MonitorLicenses(c);
            mgr.MonitorHeartbeat(c);

            // Ensure our cache is appropriately invalidated
            cache = null;
            mgr.AddLicenseChangeHandler(this, (me, manager) => me.cache = null);

            // And repopulated, so that errors show up.
            if (Result == null) throw new ApplicationException("Failed to populate license result");

            c.Plugins.add_plugin(this);

            // And don't forget a cache-breaker
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            return this;
        }

        private void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e)
        {
            // Cachebreaker
            if (e.QueryString["red_dot"] != "true" && ShouldDisplayDot(this.c, null))
            {
                e.QueryString["red_dot"] = "true";
            }
        }
        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        public string ProvideDiagnostics()
        {
            return Result.ProvideDiagnostics();
        }

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Result;
            return cache == null ? mgr.GetIssues() : mgr.GetIssues().Concat(cache.GetIssues());
        }
    }

    class DomainLookup
    {
        // For runtime use
        ConcurrentDictionary<string, string> lookupTable;
        long lookupTableSize = 0;
        const long lookupTableLimit = 8000;
        List <KeyValuePair<string, string>> suffixSearchList;
        public int KnownDomainCount { get { return suffixSearchList.Count; } }

        // For diagnostic use
        Dictionary<string, string> customMappings;
        Dictionary<string, IEnumerable<ILicenseChain>> chainsByDomain;
        public long LookupTableSize { get { return lookupTableSize; } }

        public DomainLookup(Config c, IIssueReceiver sink, IEnumerable<ILicenseChain> licenseChains)
        {
            // What domains are mentioned in which licenses?
            chainsByDomain = GetChainsByDomain(licenseChains);

            var knownDomains = chainsByDomain.Keys;

            // What custom mappings has the user set up?
            customMappings = GetDomainMappings(c, sink, knownDomains);

            // Start with identity mappings and the mappings for the normalized domains.
            lookupTable = new ConcurrentDictionary<string, string>(
                customMappings.Concat(
                    knownDomains.Select(v => new KeyValuePair<string,string>(v,v)))
                    , StringComparer.Ordinal);

            lookupTableSize = lookupTable.Count;

            // Set up a list for suffix searching
            suffixSearchList = knownDomains.Select(known =>
            {
                var d = known.TrimStart('.');
                d = d.StartsWith("www.") ? d.Substring(4) : d;
                return new KeyValuePair<string,string>("." + d, known);
            }).ToList();
        }

        Dictionary<string, IEnumerable<ILicenseChain>> GetChainsByDomain(IEnumerable<ILicenseChain> chains)
        {
            return chains.SelectMany(chain =>
                    chain.Licenses().SelectMany(b => b.Fields().GetAllDomains())
                    .Select(domain => new KeyValuePair<string, ILicenseChain>(domain, chain)))
                    .GroupBy(pair => pair.Key, pair => pair.Value, (k, v) => new KeyValuePair<string, IEnumerable<ILicenseChain>>(k, v))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                    
        }


        Dictionary<string, string> GetDomainMappings(Config c, IIssueReceiver sink, IEnumerable<string> knownDomains) //c.configurationSectionIssue
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var n = c.getNode("licenses");
            if (n == null) return mappings;
            foreach (var map in n.childrenByName("maphost"))
            {
                var from = map.Attrs["from"]?.Trim().ToLowerInvariant();
                var to = map.Attrs["to"]?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                {
                    sink.AcceptIssue(new Issue("Both from= and to= attributes are required on maphost: " + map.ToString(), IssueSeverity.ConfigurationError));
                }
                else if (from.Replace(".local", "").IndexOf('.') > -1)
                {
                    sink.AcceptIssue(new Issue("You can only map non-public hostnames to arbitrary licenses. Skipping " + from, IssueSeverity.ConfigurationError));
                }
                else if (!knownDomains.Contains(to))
                {
                    sink.AcceptIssue(new Issue(string.Format("You have mapped {0} to {1}. {1} is not one of the known domains: {2}",
                        from, to, string.Join(" ", knownDomains.OrderBy(s => s))), IssueSeverity.ConfigurationError));

                } else {
                    mappings[from] = to;
                }
            }
            return mappings;
        }

        public string ExplainDomainMappings()
        {
            return  (customMappings.Count > 0) ? 
                "For domain licensing, you have mapped the following local (non-public) domains or addresses as follows:\n" +
                    string.Join(", ", customMappings.Select(pair => string.Format("{0} => {1}", pair.Key, pair.Value))) + "\n"
                    : "";
        }

        public string ExplainNormalizations()
        {
            //Where(pair => pair.Value != null && pair.Key != pair.Value)
            return (LookupTableSize > 0) ?
                "The domain lookup table has {0} elements. Displaying subset:\n" +
                    string.Join(", ", lookupTable.OrderByDescending(p => p.Value).Take(200)
                                .Select(pair => string.Format("{0} => {1}", pair.Key, pair.Value))) + "\n" : "";
        }

        public IEnumerable<string> KnownDomains { get { return chainsByDomain.Keys; } }

        public IEnumerable<ILicenseChain> GetChainsForDomain(string domain)
        {
            IEnumerable<ILicenseChain> result;
            return chainsByDomain.TryGetValue(domain, out result) ? result : Enumerable.Empty<ILicenseChain>();
        }

        /// <summary>
        /// Returns null if there is no match or higher-level known domain. 
        /// </summary>
        /// <param name="similarDomain"></param>
        /// <returns></returns>
        public string FindKnownDomain(string similarDomain)
        {
            // Bound ConcurrentDictionary growth; fail instead
            if (lookupTableSize > lookupTableLimit) {
                string result;
                return lookupTable.TryGetValue(TrimLowerInvariant(similarDomain), out result) ? result : null;
            } else {
                return lookupTable.GetOrAdd(TrimLowerInvariant(similarDomain),
                                query =>
                                {
                                    Interlocked.Increment(ref lookupTableSize);
                                    return suffixSearchList.FirstOrDefault(
                                        pair => query.EndsWith(pair.Key, StringComparison.Ordinal)).Value;
                                });
            }
        }

        /// <summary>
        /// Only cleans up string if require; otherwise an identity function
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string TrimLowerInvariant(string s)
        {
            // Cleanup only if required
            return (s.Any(c => char.IsUpper(c) || char.IsWhiteSpace(c))) ? s.Trim().ToLowerInvariant() : s;
        }
    }
}
