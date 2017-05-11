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
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    /// Computes an (expiring) boolean result for whether the software is licensed for the functionality installed on the Config, and the license data instantly available
    /// Transient issues are stored within the class; permanent issues are stored in the  provided sink
    /// </summary>
    class Computation : IssueSink, IDiagnosticsProvider
    {
        /// <summary>
        /// If a placeholder license doesn't specify NetworkGraceMinutes, we use this value.
        /// </summary>
        const int DefaultNetworkGraceMinutes = 3;

        Config c;
        ILicenseManager mgr;
        ILicenseClock clock;
        IEnumerable<RSADecryptPublic> trustedKeys;
        IIssueReceiver permanentIssues;

        DomainLookup domainLookup;
        IList<ILicenseChain> chains;

        bool AllDomainsLicensed = false;

        bool EverythingDenied = false;

        IDictionary<string, bool> knownDomainStatus = null;

        ConcurrentDictionary<string, bool> unknownDomains = new ConcurrentDictionary<string, bool>();

        public DateTimeOffset? ComputationExpires { get; private set; }

        public Computation(Config c, IEnumerable<RSADecryptPublic> trustedKeys, IIssueReceiver permanentIssueSink, ILicenseManager mgr, ILicenseClock clock) : base("Computation")
        {
            this.c = c;
            this.trustedKeys = trustedKeys;
            this.permanentIssues = permanentIssueSink;
            this.clock = clock;
            this.mgr = mgr;
            if (mgr.FirstHeartbeat == null)
            {
                throw new ArgumentException("ILicenseManager.Heartbeat() must be called before Computation.new");
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

        bool IsLicenseExpired(ILicenseDetails details, ILicenseClock clock)
        {
            return details.Expires != null && details.Expires < clock.GetUtcNow();
        }
        bool HasLicenseBegun(ILicenseDetails details, ILicenseClock clock)
        {
            return details.Issued != null && details.Issued > clock.GetUtcNow();
        }
        public DateTimeOffset? GetBuildDate()
        {
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
            if (IsLicenseExpired(details, clock))
            {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " has expired.", b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }

            if (HasLicenseBegun(details, clock))
            {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " was issued in the future; check system clock ", b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }

            if (IsBuildDateNewer(details.SubscriptionExpirationDate))
            {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id +
                        " covers ImageResizer versions prior to " + details.SubscriptionExpirationDate.Value.ToString("D") +
                        ", but you are using a build dated " + GetBuildDate().Value.ToString("D"), b.ToRedactedString(), IssueSeverity.Warning));
                return false;
            }
            if (details.IsRevoked())
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
                                           .Select(b => b.Fields().NetworkGraceMinutes())
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
            if (EverythingDenied)
            {
                return false;
            }
            else
            {
                return AllDomainsLicensed;
            }
        }
        public bool LicensedForRequestUrl(Uri url)
        {
            if (EverythingDenied)
            {
                return false;
            }
            else if (AllDomainsLicensed)
            {
                return true;
            }
            else
            {
                var host = url?.DnsSafeHost;
                if (domainLookup.KnownDomainCount > 0 && host != null)
                {
                    var knownDomain = domainLookup.FindKnownDomain(host);
                    if (knownDomain != null)
                    {
                        return knownDomainStatus[knownDomain];
                    }
                    else
                    {
                        unknownDomains.TryAdd(domainLookup.TrimLowerInvariant(host), false);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public string ProvideDiagnostics()
        {
            var sb = new StringBuilder();
            sb.Append("\nLicense status for active features: ");
            if (EverythingDenied)
            {
                sb.AppendLine("contact support");
            }
            else if (AllDomainsLicensed)
            {
                sb.AppendLine("Valid for all domains");
            }
            else if (knownDomainStatus.Count > 0)
            {
                sb.AppendFormat("Valid for {0} domains, invalid for {1} domains, not covered for {2} domains:\n",
                    knownDomainStatus.Count(pair => pair.Value),
                    knownDomainStatus.Count(pair => !pair.Value),
                    unknownDomains.Count);
                sb.AppendFormat("Valid: {0}\nInvalid: {1}\nNot covered: {2}\n",
                    string.Join(", ", knownDomainStatus.Where(pair => pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", knownDomainStatus.Where(pair => !pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", unknownDomains.Select(pair => pair.Key)));
            }
            else
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

            sb.AppendLine("\n----------------\n");
            return sb.ToString();
        }

        public string ProvidePublicDiagnostics()
        {
            var sb = new StringBuilder();
            sb.Append("License status for active features: ");
            if (EverythingDenied)
            {
                sb.AppendLine("contact support");
            }
            else if (AllDomainsLicensed)
            {
                sb.AppendLine("Valid for all domains");
            }
            else if (knownDomainStatus.Count > 0)
            {
                sb.AppendFormat("Valid for {0} domains, invalid for {1} domains, not covered for {2} domains:\n",
                    knownDomainStatus.Count(pair => pair.Value),
                    knownDomainStatus.Count(pair => !pair.Value),
                    unknownDomains.Count);
            }
            else
            {
                sb.AppendLine("No valid licenses");
            }
            if (chains.Count > 0)
            {
                sb.AppendLine("Licenses for this Config instance:\n");
                sb.AppendLine(string.Join("\n", chains.Select(c => c.ToPublicString())));
            }
            var others = mgr.GetAllLicenses().Except(chains);
            if (others.Count() > 0)
            {
                sb.AppendLine("Licenses only used by other Config instances in this procces:\n");
                sb.AppendLine(string.Join("\n", others.Select(c => c.ToPublicString())));
            }
            return sb.ToString();
        }
    }

}
