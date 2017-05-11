using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Licensing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImageResizer.Plugins.LicenseVerifier
{

    class DomainLookup
    {
        // For runtime use
        ConcurrentDictionary<string, string> lookupTable;
        long lookupTableSize = 0;
        const long lookupTableLimit = 8000;
        List<KeyValuePair<string, string>> suffixSearchList;
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
                    knownDomains.Select(v => new KeyValuePair<string, string>(v, v)))
                    , StringComparer.Ordinal);

            lookupTableSize = lookupTable.Count;

            // Set up a list for suffix searching
            suffixSearchList = knownDomains.Select(known =>
            {
                var d = known.TrimStart('.');
                d = d.StartsWith("www.") ? d.Substring(4) : d;
                return new KeyValuePair<string, string>("." + d, known);
            }).ToList();
        }

        Dictionary<string, IEnumerable<ILicenseChain>> GetChainsByDomain(IEnumerable<ILicenseChain> chains)
        {
            return chains.SelectMany(chain =>
                    chain.Licenses().SelectMany(b => b.Fields.GetAllDomains())
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

                }
                else
                {
                    mappings[from] = to;
                }
            }
            return mappings;
        }

        public string ExplainDomainMappings()
        {
            return (customMappings.Count > 0) ?
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
            if (lookupTableSize > lookupTableLimit)
            {
                string result;
                return lookupTable.TryGetValue(TrimLowerInvariant(similarDomain), out result) ? result : null;
            }
            else
            {
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
