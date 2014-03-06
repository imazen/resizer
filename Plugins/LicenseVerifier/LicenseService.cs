using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier {
    
    public class LicenseService : ILicenseService {

        private readonly object lockFeatures = new object();

        private readonly Dictionary<string, Dictionary<Guid, FeatureState>> featureStates;
        public IDictionary<string, Dictionary<Guid, FeatureState>> FeatureStates { get { return featureStates; } }

        private readonly Dictionary<string, List<Guid>> pendingFeatures;
        public IDictionary<string, List<Guid>> PendingFeatures { get { return pendingFeatures; } }

        public LicenseService() {
            featureStates = new Dictionary<string, Dictionary<Guid, FeatureState>>(StringComparer.OrdinalIgnoreCase);
            pendingFeatures = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Notify the license service that the given feature is being used for the given domain. 
        /// </summary>
        /// <param name="domain">The domain the feature is used for.</param>
        /// <param name="feature">The feature being used.</param>
        public void NotifyUse(string domain, Guid feature) {
            domain = NormalizeDomain(domain);

            FeatureState state;
            
            lock (lockFeatures) {
                // Find out what the state of this feature is in the local cache, or initialize to Pending if unknown.
                Dictionary<Guid, FeatureState> features;
                if (!featureStates.TryGetValue(domain, out features)) {
                    features = new Dictionary<Guid, FeatureState>();
                    featureStates[domain] = features;
                }

                if (!features.TryGetValue(feature, out state)) {
                    features[feature] = state = FeatureState.Pending;

                    //Add to shortlist pendingFeatures
                    List<Guid> forDomain;
                    if (!pendingFeatures.TryGetValue(domain, out forDomain)) {
                        forDomain = new List<Guid>();
                        pendingFeatures[domain] = forDomain;
                    }
                    forDomain.Add(feature);
                }
            }
        }

        public void SetFriendlyName(Guid id, string featureDisplayName) {
            throw new NotImplementedException();
        }

        public long VerifyAuthenticity(Guid feature) {
            throw new NotImplementedException();
        }

        public string GetLicensingOverview(bool forceVerification) {
            throw new NotImplementedException();
        }

        public IPlugin Install(Configuration.Config c) {
            throw new NotImplementedException();
        }

        public bool Uninstall(Configuration.Config c) {
            throw new NotImplementedException();
        }

        private string NormalizeDomain(string domain) {
            //lowercase
            domain = domain.ToLowerInvariant();
            
            //Strip www prefix off.
            if (domain.StartsWith("www.")) domain = domain.Substring(4);

            return domain;
        }
    }
}
