// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    ///     Computes an (expiring) boolean result for whether the software is licensed for the functionality installed on the
    ///     Config, and the license data instantly available
    ///     Transient issues are stored within the class; permanent issues are stored in the  provided sink
    /// </summary>
    class Computation : IssueSink, IDiagnosticsProvider
    {
        /// <summary>
        ///     If a placeholder license doesn't specify NetworkGraceMinutes, we use this value.
        /// </summary>
        const int DefaultNetworkGraceMinutes = 6;

        const int UnknownDomainsLimit = 200;

        readonly IList<ILicenseChain> chains;
        readonly ILicenseClock clock;
        readonly DomainLookup domainLookup;

        readonly ILicenseManager mgr;
        readonly IIssueReceiver permanentIssues;

        // This is mutated to track unknown domains
        readonly ConcurrentDictionary<string, bool> unknownDomains = new ConcurrentDictionary<string, bool>();

        bool EverythingDenied { get; }
        bool AllDomainsLicensed { get; }
        IDictionary<string, bool> KnownDomainStatus { get; }
        public DateTimeOffset? ComputationExpires { get; }

        public Computation(Config c, IReadOnlyCollection<RSADecryptPublic> trustedKeys,
                           IIssueReceiver permanentIssueSink,
                           ILicenseManager mgr, ILicenseClock clock) : base("Computation")
        {
            permanentIssues = permanentIssueSink;
            this.clock = clock;
            this.mgr = mgr;
            if (mgr.FirstHeartbeat == null) {
                throw new ArgumentException("ILicenseManager.Heartbeat() must be called before Computation.new");
            }

            // What features are installed on this instance?
            // For a license to be OK, it must have one of each of this nested list;
            IEnumerable<IEnumerable<string>> pluginFeaturesUsed =
                c.Plugins.GetAll<ILicensedPlugin>().Select(p => p.LicenseFeatureCodes).ToList();

            // Create or fetch all relevant license chains; ignore the empty/invalid ones, they're logged to the manager instance
            chains = c.Plugins.GetAll<ILicenseProvider>()
                      .SelectMany(p => p.GetLicenses())
                      .Select(str => mgr.GetOrAdd(str, c.Plugins.LicenseScope))
                      .Where(x => x != null && x.Licenses().Any())
                      .Concat(c.Plugins.LicenseScope.HasFlag(LicenseAccess.ProcessReadonly)
                          ? mgr.GetSharedLicenses()
                          : Enumerable.Empty<ILicenseChain>())
                      .Distinct()
                      .ToList();


            // Set up our domain map/normalize/search manager
            domainLookup = new DomainLookup(c, permanentIssueSink, chains);


            // Check for tampering via interfaces
            if (chains.Any(chain => chain.Licenses().Any(b => !b.Revalidate(trustedKeys)))) {
                EverythingDenied = true;
                permanentIssueSink.AcceptIssue(new Issue(
                    "Licenses failed to revalidate; please contact support@imageresizing.net", IssueSeverity.Error));
            }

            // Look for grace periods
            var gracePeriods = chains.Where(IsPendingLicense).Select(GetGracePeriodFor).ToList();

            // Look for fetched and valid licenses
            var validLicenses = chains.Where(chain => !IsPendingLicense(chain))
                                      .SelectMany(chain => chain.Licenses())
                                      .Where(IsLicenseValid)
                                      .ToList();

            // This computation expires when we cross an expires, issued date, or NetworkGracePeriod expiration
            ComputationExpires = chains.SelectMany(chain => chain.Licenses())
                                       .SelectMany(b => new[] {b.Fields.Expires, b.Fields.Issued})
                                       .Concat(gracePeriods)
                                       .Where(date => date != null)
                                       .OrderBy(d => d)
                                       .FirstOrDefault(d => d > clock.GetUtcNow());


            AllDomainsLicensed = gracePeriods.Any(t => t != null) ||
                                 validLicenses.Any(b => !b.Fields.GetAllDomains().Any());

            KnownDomainStatus = validLicenses.SelectMany(
                                                 b => b.Fields.GetAllDomains()
                                                       .SelectMany(domain => b.Fields.GetFeatures()
                                                                              .Select(
                                                                                  feature => new
                                                                                      KeyValuePair<string, string>(
                                                                                          domain, feature))))
                                             .GroupBy(pair => pair.Key, pair => pair.Value,
                                                 (k, v) => new KeyValuePair<string, IEnumerable<string>>(k, v))
                                             .Select(pair => new KeyValuePair<string, bool>(pair.Key,
                                                 pluginFeaturesUsed.All(
                                                     set => set.Intersect(pair.Value, StringComparer.OrdinalIgnoreCase)
                                                               .Any())))
                                             .ToDictionary(pair => pair.Key, pair => pair.Value,
                                                 StringComparer.Ordinal);
        }

        public string ProvideDiagnostics()
        {
            var sb = GetLicenseStatus();
            sb.AppendLine();
            sb.Append(domainLookup.ExplainNormalizations());
            if (chains.Count > 0) {
                sb.AppendLine("Licenses for this Config instance:\n");
                sb.AppendLine(string.Join("\n", chains.Select(c => c.ToString())));
            }
            var others = mgr.GetAllLicenses().Except(chains).Select(c => c.ToString()).ToList();
            if (others.Any()) {
                sb.AppendLine("Licenses only used by other Config instances in this process:\n");
                sb.AppendLine(string.Join("\n", others));
            }

            sb.AppendLine("\n----------------\n");
            return sb.ToString();
        }


        bool IsLicenseExpired(ILicenseDetails details) => details.Expires != null &&
                                                          details.Expires < clock.GetUtcNow();

        bool HasLicenseBegun(ILicenseDetails details) => details.Issued != null &&
                                                         details.Issued > clock.GetUtcNow();

        public DateTimeOffset? GetBuildDate() => clock.GetBuildDate() ?? clock.GetAssemblyWriteDate();

        public bool IsBuildDateNewer(DateTimeOffset? value)
        {
            var buildDate = GetBuildDate();
            return buildDate != null &&
                   value != null &&
                   buildDate > value;
        }

        bool IsLicenseValid(ILicenseBlob b)
        {
            var details = b.Fields;
            if (IsLicenseExpired(details)) {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " has expired.", b.ToRedactedString(),
                    IssueSeverity.Warning));
                return false;
            }

            if (HasLicenseBegun(details)) {
                permanentIssues.AcceptIssue(new Issue(
                    "License " + details.Id + " was issued in the future; check system clock ", b.ToRedactedString(),
                    IssueSeverity.Warning));
                return false;
            }

            if (IsBuildDateNewer(details.SubscriptionExpirationDate)) {
                permanentIssues.AcceptIssue(new Issue(
                    $"License {details.Id} covers ImageResizer versions prior to {details.SubscriptionExpirationDate?.ToString("D")}, but you are using a build dated {GetBuildDate()?.ToString("D")}",
                    b.ToRedactedString(),
                    IssueSeverity.Warning));
                return false;
            }
            if (details.IsRevoked()) {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " is no longer active",
                    b.ToRedactedString(), IssueSeverity.Warning));
            }
            return true;
        }

        bool IsPendingLicense(ILicenseChain chain)
        {
            return chain.IsRemote && chain.Licenses().All(b => b.Fields.IsRemotePlaceholder());
        }

        /// <summary>
        ///     Pending licenses can offer grace periods. Logs a local issue; trusts the instance (and issue) will be cleared
        ///     when the returned DateTime passes. May subdivide a grace period for more granular issue text.
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        DateTimeOffset? GetGracePeriodFor(ILicenseChain chain)
        {
            // If the placeholder license fails its own constraints, don't add a grace period
            if (chain.Licenses().All(b => !IsLicenseValid(b))) {
                return null;
            }

            var graceMinutes = chain.Licenses()
                                    .Where(IsLicenseValid)
                                    .Select(b => b.Fields.NetworkGraceMinutes())
                                    .OrderByDescending(v => v)
                                    .FirstOrDefault() ?? DefaultNetworkGraceMinutes;

            // Success will automatically replace this instance. Warn immediately.
            Debug.Assert(mgr.FirstHeartbeat != null, "mgr.FirstHeartbeat != null");

            // NetworkGraceMinutes Expired?
            var expires = mgr.FirstHeartbeat.Value.AddMinutes(graceMinutes);
            if (expires < clock.GetUtcNow()) {
                AcceptIssue(new Issue($"Grace period of {graceMinutes}m expired for license {chain.Id}",
                    $"License {chain.Id} was not found in the disk cache and could not be retrieved from the remote server within {graceMinutes} minutes.",
                    IssueSeverity.Error));
                return null;
            }

            // Less than 30 seconds since boot time?
            var thirtySeconds = mgr.FirstHeartbeat.Value.AddSeconds(30);
            if (thirtySeconds > clock.GetUtcNow()) {
                AcceptIssue(new Issue($"Fetching license {chain.Id} (not found in disk cache).",
                    $"Network grace period expires in {graceMinutes} minutes", IssueSeverity.Warning));
                return thirtySeconds;
            }

            // Otherwise in grace period
            AcceptIssue(new Issue(
                $"Grace period of {graceMinutes}m will expire for license {chain.Id} at {expires:HH:mm} on {expires:D}",
                $"License {chain.Id} was not found in the disk cache and could not be retrieved from the remote server.",
                IssueSeverity.Error));

            return expires;
        }


        public bool LicensedForAll() => !EverythingDenied && AllDomainsLicensed;

        public bool LicensedForRequestUrl(Uri url)
        {
            if (EverythingDenied) {
                return false;
            }
            if (AllDomainsLicensed) {
                return true;
            }
            var host = url?.DnsSafeHost;
            if (domainLookup.KnownDomainCount > 0 && host != null) {
                var knownDomain = domainLookup.FindKnownDomain(host);
                if (knownDomain != null) {
                    return KnownDomainStatus[knownDomain];
                }
                if (unknownDomains.Count < UnknownDomainsLimit) {
                    unknownDomains.TryAdd(domainLookup.TrimLowerInvariant(host), false);
                }
                return false;
            }
            return false;
        }

        StringBuilder GetLicenseStatus()
        {
            var sb = new StringBuilder();
            sb.Append("\nLicense status for active features: ");
            if (EverythingDenied) {
                sb.AppendLine("License error. Contact support@imageresizing.net");
            } else if (AllDomainsLicensed) {
                sb.AppendLine("Valid for all domains");
            } else if (KnownDomainStatus.Any()) {
                sb.AppendFormat("Valid for {0} domains, invalid for {1} domains, not covered for {2} domains:\n",
                    KnownDomainStatus.Count(pair => pair.Value),
                    KnownDomainStatus.Count(pair => !pair.Value),
                    unknownDomains.Count);
                sb.AppendFormat("Valid: {0}\nInvalid: {1}\nNot covered: {2}\n",
                    string.Join(", ", KnownDomainStatus.Where(pair => pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", KnownDomainStatus.Where(pair => !pair.Value).Select(pair => pair.Key)),
                    string.Join(", ", unknownDomains.Select(pair => pair.Key)));
            } else {
                sb.AppendLine("No valid licenses");
            }
            sb.AppendLine();
            return sb;
        }

        public string ProvidePublicDiagnostics()
        {
            var sb = GetLicenseStatus();
            if (chains.Count > 0) {
                sb.AppendLine("Licenses for this Config instance:\n");
                sb.AppendLine(string.Join("\n", chains.Select(c => c.ToPublicString())));
            }
            var others = mgr.GetAllLicenses().Except(chains).Select(c => c.ToPublicString()).ToList();
            if (others.Any()) {
                sb.AppendLine("Licenses only used by other Config instances in this process:\n");
                sb.AppendLine(string.Join("\n", others));
            }
            return sb.ToString();
        }
    }
}
