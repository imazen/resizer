// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    ///     Computes an (expiring) boolean result for whether the software is licensed for the functionality installed on the
    ///     Config, and the license data instantly available
    ///     Transient issues are stored within the class; permanent issues are stored in the  provided sink
    /// </summary>
    class Computation : IssueSink, IDiagnosticsProvider, IDiagnosticsHeaderProvider, IDiagnosticsFooterProvider, ILicenseDiagnosticsProvider
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
        bool EnforcementEnabled { get; }
        IDictionary<string, bool> KnownDomainStatus { get; }
        public DateTimeOffset? ComputationExpires { get; }
        LicenseAccess Scope { get; }
        LicenseErrorAction LicenseError { get; }
        public Computation(Config c, IReadOnlyCollection<RSADecryptPublic> trustedKeys,
                           IIssueReceiver permanentIssueSink,
                           ILicenseManager mgr, ILicenseClock clock, bool enforcementEnabled) : base("Computation")
        {
            permanentIssues = permanentIssueSink;
            EnforcementEnabled = enforcementEnabled;
            this.clock = clock;
            Scope = c.Plugins.LicenseScope;
            LicenseError = c.Plugins.LicenseError;
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
                      .Concat(Scope.HasFlag(LicenseAccess.ProcessReadonly)
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
                                      .Where(b => !b.Fields.IsRemotePlaceholder() && IsLicenseValid(b))
                                      .ToList();

            // This computation expires when we cross an expires, issued date, or NetworkGracePeriod expiration
            ComputationExpires = chains.SelectMany(chain => chain.Licenses())
                                       .SelectMany(b => new[] {b.Fields.Expires, b.Fields.Issued})
                                       .Concat(gracePeriods)
                                       .Where(date => date != null)
                                       .OrderBy(d => d)
                                       .FirstOrDefault(d => d > clock.GetUtcNow());

            AllDomainsLicensed = gracePeriods.Any(t => t != null) ||
                                 validLicenses
                                     .Any(license => !license.Fields.GetAllDomains().Any() && AreFeaturesLicensed(license, pluginFeaturesUsed, false));

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

            if (UpgradeNeeded()) {
                foreach (var b in validLicenses)
                    AreFeaturesLicensed(b, pluginFeaturesUsed, true);
            }
        }


        bool IsLicenseExpired(ILicenseDetails details) => details.Expires != null &&
                                                          details.Expires < clock.GetUtcNow();

        bool HasLicenseBegun(ILicenseDetails details) => details.Issued != null &&
                                                         details.Issued < clock.GetUtcNow();


        public IEnumerable<string> GetMessages(ILicenseDetails d) => new[] {
            d.GetMessage(),
            IsLicenseExpired(d) ? d.GetExpiryMessage() : null,
            d.GetRestrictions()
        }.Where(s => !string.IsNullOrWhiteSpace(s));


    

        public DateTimeOffset? GetBuildDate() => clock.GetBuildDate() ?? clock.GetAssemblyWriteDate();

        public bool IsBuildDateNewer(DateTimeOffset? value)
        {
            var buildDate = GetBuildDate();
            return buildDate != null &&
                   value != null &&
                   buildDate > value;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        bool AreFeaturesLicensed(ILicenseBlob b, IEnumerable<IEnumerable<string>> oneFromEach, bool logIssues)
        {
            var licenseFeatures = b.Fields.GetFeatures();
            var notCovered = oneFromEach.Where(
                set => !set.Intersect(licenseFeatures, StringComparer.OrdinalIgnoreCase).Any());
            var success = !notCovered.Any();
            if (!success && logIssues) {
                permanentIssues.AcceptIssue(new Issue(
                    $"License {b.Fields.Id} needs to be upgraded; it does not cover in-use features {notCovered.SelectMany(v => v).Distinct().Delimited(", ")}", b.ToRedactedString(),
                    IssueSeverity.Error));
            }
            return success;
        }
        

        bool IsLicenseValid(ILicenseBlob b)
        {
            var details = b.Fields;
            if (IsLicenseExpired(details)) {
                permanentIssues.AcceptIssue(new Issue("License " + details.Id + " has expired.", b.ToRedactedString(),
                    IssueSeverity.Error));
                return false;
            }

            if (!HasLicenseBegun(details)) {
                permanentIssues.AcceptIssue(new Issue(
                    "License " + details.Id + " was issued in the future; check system clock.", b.ToRedactedString(),
                    IssueSeverity.Error));
                return false;
            }

            if (IsBuildDateNewer(details.SubscriptionExpirationDate)) {
                permanentIssues.AcceptIssue(new Issue(
                    $"License {details.Id} covers ImageResizer versions prior to {details.SubscriptionExpirationDate?.ToString("D")}, but you are using a build dated {GetBuildDate()?.ToString("D")}",
                    b.ToRedactedString(),
                    IssueSeverity.Error));
                return false;
            }
            if (details.IsRevoked()) {
                var message = b.Fields.GetMessage();
                permanentIssues.AcceptIssue(new Issue($"License {details.Id}" + (message != null ? $": {message}" : " is no longer valid"),
                    b.ToRedactedString(), IssueSeverity.Error));
                return false;
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
                permanentIssues.AcceptIssue(new Issue($"Grace period of {graceMinutes}m expired for license {chain.Id}",
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
                $"Grace period of {graceMinutes}m will expire for license {chain.Id} at UTC {expires:HH:mm} on {expires:D}",
                $"License {chain.Id} was not found in the disk cache and could not be retrieved from the remote server.",
                IssueSeverity.Error));

            return expires;
        }


        public bool LicensedForAll() => !EverythingDenied && AllDomainsLicensed;

        public bool LicensedForSomething()
        {
            return !EverythingDenied &&
                   (AllDomainsLicensed || (domainLookup.KnownDomainCount > 0));
        }

        public bool UpgradeNeeded() =>  !AllDomainsLicensed || KnownDomainStatus.Values.Contains(false); 

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



        public string LicenseStatusSummary()
        {
            if (EverythingDenied) {
                return "License error. Contact support@imageresizing.net";
            }
            if (AllDomainsLicensed) {
                return "License valid for all domains";
            }
            if (KnownDomainStatus.Any())
            {
                
                var valid = KnownDomainStatus.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                var invalid = KnownDomainStatus.Where(pair => !pair.Value).Select(pair => pair.Key).ToArray();
                var notCovered = unknownDomains.Select(pair => pair.Key).ToArray();

                var sb = new StringBuilder($"License valid for {valid.Length} domains");
                if (invalid.Length > 0)
                    sb.Append($", insufficient for {invalid.Length} domains");
                if (notCovered.Length > 0)
                    sb.Append($", missing for {notCovered.Length} domains");

                sb.Append(": ");

                sb.Append(valid.Select(s => $"{s} (valid)")
                               .Concat(invalid.Select(s => $"{s} (not sufficient)"))
                               .Concat(notCovered.Select(s => $"{s} (not licensed)"))
                               .Delimited(", "));
                return sb.ToString();
            }
   
            return chains.Any() ? "No valid licenses found" : "No licenses found";
        }

        IEnumerable<string> GetMessages() =>
            chains.SelectMany(c => c.Licenses()
                                    .SelectMany(l => GetMessages(l.Fields))
                                    .Select(s => $"License {c.Id}: {s}"));

        string RestrictionsAndMessages() => GetMessages().Delimited("\r\n");

        string EnforcementMethodMessage => LicenseError == LicenseErrorAction.Exception
                ? $"You are using <licenses licenseError='{LicenseError}'>. If there is a licensing error, an exception will be thrown (with HTTP status code 402). This can also be set to '{LicenseErrorAction.Watermark}'."
                : $"You are using <licenses licenseError='{LicenseError}'>. If there is a licensing error, an red dot will be drawn on the bottom-right corner of each image. This can be set to '{LicenseErrorAction.Exception}' instead (valuable if you are storing results)."
            ;
        

        string SalesMessage() { 
            if (mgr.GetAllLicenses().All(l => !l.IsRemote && l.Id.Contains("."))) {
                return "Need to change domains? Get a discounted upgrade to a floating license: https://imageresizing.net/licenses/convert";
            }

            if (!chains.Any()) {
                return "To get a license, visit https://imageresizing.net/licenses";
            }

            // Missing feature codes (could be edition OR version, i.e, R4Performance vs R_Performance
            if (KnownDomainStatus.Values.Contains(false))
            {
                return "To upgrade your license, visit https://imageresizing.net/licenses";
            }

            if (!EnforcementEnabled)
            {
                return @"Having trouble with NuGet caching a DRM-enabled version of ImageResizer?
A universal license key would fix that. See if your purchase is eligible for a free key: https://imageresizing.net/licenses/convert";
            }
            return null;
        }

        public string ProvideDiagnosticsHeader() => GetHeader(true, true);
    



        string GetHeader(bool includeSales, bool includeScope)
        {

            var summary = includeScope
                ? $"License status for active features (for {Scope}):\r\n{LicenseStatusSummary()}"
                : LicenseStatusSummary();
            var restrictionsAndMessages = RestrictionsAndMessages();

            var salesMessage = includeSales ? SalesMessage() : null;

            var hr = EnforcementEnabled
                ? $"---------------------- Licensing ON ----------------------\r\n"
                : $"---------------------- Licensing OFF -----------------------\r\n";



            if (!EnforcementEnabled)
            {
                return $@"{hr}
You are using a DRM-disabled version of ImageResizer. License enforcement is OFF.
DRM-enabled assemblies (if present) would see <licenses licenseError='{LicenseError}'>
                
{salesMessage}

{restrictionsAndMessages}
{hr}";
            }

            return hr + new[] { summary, restrictionsAndMessages, salesMessage, EnforcementMethodMessage }
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Delimited("\r\n\r\n") + "\r\n" + hr;
        }

        public string ProvideDiagnostics() => ProvideDiagnosticsInternal(c => c.ToString());

        public string ProvidePublicText() => GetHeader(false, false) + ProvideDiagnosticsInternal(c => c.ToPublicString());

        string ProvideDiagnosticsInternal(Func<ILicenseChain, string> stringifyChain)
        {
            var sb = new StringBuilder();
            if (chains.Count > 0)
            {
                sb.AppendLine("Licenses for this Config instance:\n");
                sb.AppendLine(string.Join("\n", chains.Select(stringifyChain)));
            }
            var others = mgr.GetAllLicenses().Except(chains).Select(stringifyChain).ToList();
            if (others.Any())
            {
                sb.AppendLine("Licenses only used by other Config instances in this process:\n");
                sb.AppendLine(string.Join("\n", others));
            }
            sb.AppendLine();
            sb.Append(domainLookup.ExplainNormalizations());
            return sb.ToString();
        }



        public string ProvideDiagnosticsFooter()
        {
            return "The most recent license fetch used the following URL:\r\n\r\n" +
                   mgr.GetAllLicenses().Select(c => c.LastFetchUrl()).Delimited("\r\n");
        }
    }
}
