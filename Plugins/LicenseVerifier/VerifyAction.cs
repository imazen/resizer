using ImageResizer.Plugins.LicenseVerifier.Async;
using ImageResizer.Plugins.LicenseVerifier.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace ImageResizer.Plugins.LicenseVerifier {
    public class VerifyAction : BackgroundAction {

        private readonly IHttpClient httpClient;
        private readonly string password;
        private readonly Guid appId;
        private readonly ILicenseStore licenseStore;
        
        private readonly Dictionary<string, Dictionary<Guid, FeatureState>> featureStates;
        public Dictionary<string, Dictionary<Guid, FeatureState>> FeatureStates { get { return featureStates; } }
        
        private readonly Dictionary<string, List<Guid>> pendingFeatures;
        public Dictionary<string, List<Guid>> PendingFeatures { get { return pendingFeatures; } }

        private readonly object lockFeatures = new object();
        
        public VerifyAction(IHttpClient httpClient, 
            string password,
            Guid appId, 
            ILicenseStore licenseStore,
            Dictionary<string, Dictionary<Guid, FeatureState>> featureStates,
            Dictionary<string, List<Guid>> pendingFeatures) {
            
            this.httpClient = httpClient;
            this.password = password;
            this.appId = appId;
            this.licenseStore = licenseStore;
            this.featureStates = featureStates;
            this.pendingFeatures = pendingFeatures;
        }

        protected override void PerformBackgroundAction() {
            var licenses = RemovedExpired(DecryptAll(licenseStore.GetLicenses()));

            // Fix all possible pendingFeatures using existing license data.
            UpdateFeatureStatus(licenses, true);

            // If we still have pending features, retrieve license info from KeyHub
            if (pendingFeatures.Count > 0) {
                var newLicenses = DecryptAll(RequestLicenses());

                if (newLicenses.Count > 0) {
                    UpdateFeatureStatus(newLicenses, false);
                    
                    licenseStore.SetLicenses(ExportLicenses(Merge(licenses, newLicenses)));
                }
            }
        }

        private List<KeyValuePair<string, byte[]>> ExportLicenses(Dictionary<string, List<DomainLicense>> licenses) {
            List<KeyValuePair<string, byte[]>> n = new List<KeyValuePair<string, byte[]>>();
            foreach (string key in licenses.Keys) {
                if (licenses[key] != null) {
                    foreach (var d in licenses[key]) {
                        n.Add(new KeyValuePair<string, byte[]>(d.GetShortDescription(), d.SerializeAndEncrypt(password)));
                    }
                }
            }
            return n;
        }
        
        private Dictionary<string, List<DomainLicense>> Merge(Dictionary<string, List<DomainLicense>> a, Dictionary<string, List<DomainLicense>> b) {
            var n = new Dictionary<string, List<DomainLicense>>(a);
            foreach (string key in b.Keys) {
                List<DomainLicense> existingList;
                a.TryGetValue(key, out existingList);
                if (existingList == null) n[key] = b[key];
                else if (b[key] != null) existingList.AddRange(b[key]);
            }
            return n;
        }

        private Dictionary<string, List<DomainLicense>> RemovedExpired(Dictionary<string, List<DomainLicense>> licenses) {
            DateTime now = DateTime.UtcNow;
            var d = new Dictionary<string, List<DomainLicense>>(licenses.Comparer);
            foreach (string s in licenses.Keys) {
                List<DomainLicense> remaining = null;
                if (licenses[s] == null) continue;
                foreach (DomainLicense l in licenses[s]) {
                    if (l.Expires > now) {
                        if (remaining == null) remaining = new List<DomainLicense>();
                        remaining.Add(l);
                    }
                }
                if (remaining != null) d[s] = remaining;
            }
            return d;
        }

        private void UpdateFeatureStatus(Dictionary<string, List<DomainLicense>> licenses, bool updatingFromCache) {
            DateTime now = DateTime.UtcNow;
            // Iterate through pendingFeatures and resolve them.
            lock (lockFeatures) {
                // Loop through pendingFeature domain names
                var pendingDomains = new List<string>(pendingFeatures.Keys);
                foreach (string domain in pendingDomains) {
                    // Skip domain names that don't have matching licenses
                    List<DomainLicense> forDomain;
                    if (!licenses.TryGetValue(domain, out forDomain)) continue;

                    // Loop through all features for domain and update accordingly
                    var domainPendingFeatures = pendingFeatures[domain];
                    var originalFeatures = new List<Guid>(featureStates[domain].Keys);
                    foreach (var featureId in originalFeatures) {
                        // Loop through licenses for the domain
                        foreach (var l in forDomain) {
                            // If the license hasn't expired and has a matching feature,
                            // update featureStates
                            if (l.Expires > now && l.Features.Contains(featureId)) {
                                featureStates[domain][featureId] = FeatureState.Enabled;
                            }
                            
                            // If we're not updating from the cache and we didn't find a matching feature,
                            // then reject the feature.
                            if (!updatingFromCache && (l.Expires <= now || !l.Features.Contains(featureId))) {
                                featureStates[domain][featureId] = FeatureState.Rejected;
                            }

                            if (!updatingFromCache) {
                                // Remove the feature from pendingFeatures
                                domainPendingFeatures.Remove(featureId);
                                if (domainPendingFeatures.Count == 0) pendingFeatures.Remove(domain);
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, List<DomainLicense>> DecryptAll(ICollection<byte[]> encrypted) {
            var licenses = new Dictionary<string, List<DomainLicense>>(StringComparer.OrdinalIgnoreCase);

            foreach (byte[] data in encrypted) {
                var d = new DomainLicense(data, password);
                string domain = d.Domain;
                List<DomainLicense> forDomain;
                if (!licenses.TryGetValue(domain, out forDomain)) {
                    forDomain = new List<DomainLicense>();
                    licenses[domain] = forDomain;
                }
                forDomain.Add(d);
            }
            return licenses;
        }

        private List<byte[]> RequestLicenses() {
            var results = new List<byte[]>();

            if (appId == Guid.Empty) return results;

            XmlDocument doc = CreateRequestXml();

            var request = new HttpRequest {
                Url = new Uri("http://keyhub.lucrasoft-staging.nl/GetLicenses"),
                ContentType = "application/xml",
                Method = HttpMethod.Post,
                Content = doc.OuterXml
            };

            var response = httpClient.Send(request);

            XmlDocument rdoc = new XmlDocument();
            using (TextReader sr = new StringReader(response.Content)) {
                rdoc.Load(sr);
                foreach (var l in rdoc.DocumentElement.ChildNodes) {
                    var lic = l as XmlElement;
                    if (lic != null && lic.Name == "license")
                        results.Add(Convert.FromBase64String(lic.InnerText.Trim()));
                }
            }

            return results;
        }

        private XmlDocument CreateRequestXml() {
            XmlDocument doc = new XmlDocument();
            var root = doc.CreateElement("licenseReqeust");
            doc.AppendChild(root);
            var appIdElement = doc.CreateElement("appId");
            appIdElement.AppendChild(doc.CreateTextNode(appId.ToString()));
            root.AppendChild(appIdElement);
            var domains = doc.CreateElement("domains");
            root.AppendChild(domains);
            foreach (string domain in pendingFeatures.Keys) {
                var d = doc.CreateElement("domain");
                d.SetAttribute("name", domain);
                foreach (Guid g in pendingFeatures[domain]) {
                    var feature = doc.CreateElement("feature");
                    feature.AppendChild(doc.CreateTextNode(g.ToString()));
                    d.AppendChild(feature);
                }
                domains.AppendChild(d);
            }

            return doc;
        }
    }
}
