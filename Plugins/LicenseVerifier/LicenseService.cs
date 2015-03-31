// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using ImageResizer.Configuration;
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

namespace ImageResizer.Plugins.LicenseVerifier {
    


    internal class LicenseService  {
        IEnumerable<ILicenseProvider> licenseSources;
        RSADecryptPublic rsa;
        IIssueReceiver sink;
        public LicenseService(IEnumerable<ILicenseProvider> licenseSources,  BigInteger keyMod, BigInteger keyExponent, IIssueReceiver sink){
            rsa = new RSADecryptPublic(keyMod, keyExponent);
            this.sink = sink;
            this.licenseSources = licenseSources;
        }

        bool licenseProblems = false;
 
        public IDictionary<string, ISet<string>> CollectDomainFeatures(){
            Dictionary<string, ISet<string>> dict = new Dictionary<string,ISet<string>>(StringComparer.OrdinalIgnoreCase);
            
            foreach(var source in licenseSources){
                var rawLicenses = source.GetLicenses();
                foreach (string rawLicense in rawLicenses){
                    try{
                        var blob = LicenseBlob.Deserialize(rawLicense);
                        StringBuilder log = new StringBuilder();
                        bool valid_signature = new LicenseValidator().Validate(blob, rsa, log);
                        bool expired = blob.Details.Expires < DateTime.UtcNow;
                        bool invalid_time = blob.Details.Issued > DateTime.UtcNow;

                        if (expired || invalid_time)
                        {
                            sink.AcceptIssue(new Issue("License key " + (expired ? "has expired: " : "was issued in the future; check system clock: ") + blob.Details.Domain, IssueSeverity.Warning));
                        }
                        if (!valid_signature){
                            sink.AcceptIssue(new Issue("Invalid license key: failed to validate signature.",log.ToString(), IssueSeverity.Error));
                        }
                        if (valid_signature && !invalid_time && !expired){

                            dict[blob.Details.Domain] = new HashSet<string>(Enumerable.Concat(blob.Details.Features, dict.ContainsKey(blob.Details.Domain) ?  dict[blob.Details.Domain]  : Enumerable.Empty<string>()));
                        }
                        else
                        {
                            licenseProblems = true;
                        }
                    }catch (Exception e){
                        sink.AcceptIssue(new Issue("Error parsing & validating license key: " + e.Message,e.StackTrace, IssueSeverity.Error));
                        licenseProblems = true;
                    }
                }
            }
            return dict;

        }

        public void InvalidateLicenseCache(){
           lock(_cacheLock){
               _cachedFeatures = null;
           }
        }

        IDictionary<string, ISet<string>> _cachedFeatures = null;
        object _cacheLock = new object();
        internal IDictionary<string, ISet<string>> LicensedFeatures{
            get{
                if (_cachedFeatures == null){
                    lock(_cacheLock){
                        if (_cachedFeatures == null) _cachedFeatures = CollectDomainFeatures();
                    }
                }
                return _cachedFeatures;
            }
        }

        public bool CheckFeaturesLicensed(string domain, IEnumerable<IEnumerable<string>> require_one_from_each_collection, bool alsoRequireCleanLicenses)
        {
            if (alsoRequireCleanLicenses && licenseProblems) return false;
            
            var norm = NormalizeDomain(domain);

            ISet<string> domain_features;
            bool found = false;
            if (LicensedFeatures.TryGetValue(norm, out domain_features))
            {
                found = require_one_from_each_collection.All(coll => domain_features.Intersect(coll).Count() > 0);
            }
            if (!found)
            {
                sink.AcceptIssue(new Issue(string.Format("No license found for domain {0} - features installed: {1}", domain, String.Join(" AND ", require_one_from_each_collection.Select(coll => String.Join(" or ", coll)))), IssueSeverity.Error));
            }
            return found;
        }


        ConcurrentDictionary<string, string> normalized_domains = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string NormalizeDomain(string domain)
        {
            var licensed = LicensedFeatures;
            if (licensed.ContainsKey(domain)) return domain;
            
            string normal;
            if (normalized_domains.TryGetValue(domain, out normal)) return normal;

            normal = domain.ToLowerInvariant(); 
            ///Deal with localhost, IP addresses, etc, hosts file

            if (!licensed.ContainsKey(normal))
            {
                //Try to find the first licensed that is a subset of the provided domain
                normal = licensed.Keys.Where(d => normal.EndsWith(d, StringComparison.Ordinal)).FirstOrDefault(d => normal.EndsWith("." + d, StringComparison.Ordinal)) ?? normal;
            }
            normalized_domains[domain] = normal;
            return normal;
        }

        public string GetLicensedFeaturesDescription()
        {
            return String.Join("\n",LicensedFeatures.Select(pair => String.Format("{0} => {1}", pair.Key, String.Join(" ", pair.Value))));
        }


    }
    internal class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider
    {

        public LicenseEnforcer(){}

        //Map plugin names to feature sets?
        //ILicensedPlugin -> feature code strings

        private string pubkey_exponent = "65537";
        private string pubkey_modulus = "28178177427582259905122756905913963624440517746414712044433894631438407111916149031583287058323879921298234454158166031934230083094710974550125942791690254427377300877691173542319534371793100994953897137837772694304619234054383162641475011138179669415510521009673718000682851222831185756777382795378538121010194881849505437499638792289283538921706236004391184253166867653735050981736002298838523242717690667046044130539971131293603078008447972889271580670305162199959939004819206804246872436611558871928921860176200657026263241409488257640191893499783065332541392967986495144643652353104461436623253327708136399114561";

        private Configuration.Config c;
        private LicenseService GetService(Configuration.Config c)
        {
            return new LicenseService(c.Plugins.GetAll<ILicenseProvider>().ToList(), BigInteger.Parse(pubkey_modulus), BigInteger.Parse(pubkey_exponent), c.configurationSectionIssues);
        }

        private Dictionary<string, string> GetDomainMappings(Configuration.Config c)
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var n = c.getNode("licenses");
            if (n == null) return mappings;
            foreach (var map in n.childrenByName("maphost"))
            {
                var from = map.Attrs["from"];
                var to = map.Attrs["to"];
                if (from == null || to == null)
                {
                    c.configurationSectionIssues.AcceptIssue(new Issue("Both from= and to= attributes are required on maphost: " + map.ToString(), IssueSeverity.ConfigurationError));

                }
                else
                {
                    from = from.ToLowerInvariant();
                    if (from.Replace(".local", "").IndexOf('.') > -1)
                    {
                        c.configurationSectionIssues.AcceptIssue(new Issue("You can only map non-public hostnames to arbitrary licenses. Skipping " + from, IssueSeverity.ConfigurationError));
                    }
                    else
                    {
                        mappings[from] = to;
                    }
                }
            }
            return mappings;
        }


        public string ProvideDiagnostics()
        {
            StringBuilder sb = new StringBuilder();

            var mappings = _mappings ?? GetDomainMappings(c);
            var service = _service ?? GetService(c);
            var features = _installed_features ?? c.Plugins.GetAll<ILicensedPlugin>().Select(p => p.LicenseFeatureCodes).ToList();

            sb.AppendLine("\n----------------\n");
            sb.AppendLine("License keys");
            if (mappings.Count > 0)
            {
                sb.AppendLine("For licensing, you have mapped the following local (non-public) domains or addresses as follows:\n" +
                    String.Join(", ", mappings.Select(pair => string.Format("{0} => {1}", pair.Key, pair.Value))));
            }

            var licenses = service.GetLicensedFeaturesDescription();
            sb.AppendLine();
            if (licenses.Length > 0)
            {
                sb.AppendLine("List of installed domain licenses:\n" + licenses);
            }
            else
            {
                sb.AppendLine("You do not have any license keys installed.");
            
            }
            sb.AppendLine("\n----------------\n");
            return sb.ToString();
        }

   

        DateTime first_request = DateTime.MinValue;
        int invalidated_count = 0;
        LicenseService _service = null;
        IEnumerable<IEnumerable<string>> _installed_features;
        Dictionary<string, string> _mappings;
        private bool ShouldDisplayDot(Configuration.Config c, ImageState s)
        {
            if (c == null || c.configurationSectionIssues == null || System.Web.HttpContext.Current == null) return false;

            //We want to invalidate the caches after 5 and 30 seconds.
            if (first_request == DateTime.MinValue) first_request = DateTime.UtcNow;
            bool invalidate = invalidated_count == 0 && DateTime.UtcNow - first_request > TimeSpan.FromSeconds(5) ||
                invalidated_count == 1 && DateTime.UtcNow - first_request > TimeSpan.FromSeconds(30);


            if (invalidate) invalidated_count++;

            //Cache a LicenseService and nested enumeration of installed feature codes
            if (_service == null || invalidate) _service = GetService(c);
            if (_installed_features == null|| invalidate) _installed_features = c.Plugins.GetAll<ILicensedPlugin>().Select(p => p.LicenseFeatureCodes).ToList();
            if (_mappings == null|| invalidate) _mappings = GetDomainMappings(c);

            //if (invalidated_count == 1) LogLicenseConfiguration(c);

            var domain = System.Web.HttpContext.Current.Request.UserHostName;
            
            //Handles remapping
            if (_mappings.ContainsKey(domain))
            {
                domain = _mappings[domain];
            }

            return !_service.CheckFeaturesLicensed(domain, _installed_features, false); //show an error if there are any issues parsing a lc

        }


        protected override RequestedAction PreFlushChanges(ImageState s)
        {

            bool enforce = false;
            if (s.destBitmap != null && ShouldDisplayDot(c, s) && enforce)
            {
                int w = s.destBitmap.Width;
                int h = s.destBitmap.Height;
                int dot_w = 3;
                int dot_h = 3;
                if (w > dot_w && h > dot_h)
                {
                    for (int y = 0; y < dot_h; y++)
                        for (int x = 0; x < dot_w; x++ )
                            s.destBitmap.SetPixel(w - 1 - x, h - 1 - y, Color.Red);
                }
            }
            return RequestedAction.None;
        }

        public IPlugin Install(Configuration.Config c)
        {
            this.c = c;
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

    }
}
