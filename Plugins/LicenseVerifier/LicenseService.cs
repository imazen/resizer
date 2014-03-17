using ImageResizer.Configuration;
using ImageResizer.Plugins.LicenseVerifier.Async;
using ImageResizer.Plugins.LicenseVerifier.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier {
    
    public class LicenseService : ILicenseService {

        private readonly IHttpClient httpClient;

        private readonly object lockFeatures = new object();

        private readonly Dictionary<string, Dictionary<Guid, FeatureState>> featureStates;
        public IDictionary<string, Dictionary<Guid, FeatureState>> FeatureStates { get { return featureStates; } }

        private readonly Dictionary<string, List<Guid>> pendingFeatures;
        public IDictionary<string, List<Guid>> PendingFeatures { get { return pendingFeatures; } }

        private Config config;

        public Guid AppId { get; set; }
        public Uri LicensingUrl { get; set; }
        public string PublicKeyXml { get; set; }
        public TimeSpan VerificationInterval { get; set; }
        public string LicenseCacheFilePattern { get; set; }

        public LicenseService(IHttpClient httpClient) {
            this.httpClient = httpClient;

            featureStates = new Dictionary<string, Dictionary<Guid, FeatureState>>(StringComparer.OrdinalIgnoreCase);
            pendingFeatures = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);

            VerificationInterval = new TimeSpan(0, 10, 0);//10 mins
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

            // TODO: Mark the request if feature license invalid. We'll deal with it later.

            PingBackgroundVerification();
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

        /// <summary>
        /// Installs the plugin in the specified Config instance. The plugin must handle all the work of loading settings, registering the plugin etc.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            config = c;
            c.Plugins.add_plugin(this);
            // TODO: Read config for initial settings
            return this;
        }

        /// <summary>
        /// The LicenseService cannot be uninstalled.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c) {
            return false;
        }

        private string NormalizeDomain(string domain) {
            //lowercase
            domain = domain.ToLowerInvariant();
            
            //Strip www prefix off.
            if (domain.StartsWith("www.")) domain = domain.Substring(4);

            return domain;
        }

        private DateTime lastScheduledVerification = DateTime.MinValue;
        private DateTime lastEndedVerification = DateTime.MinValue;
        private object lockStart = new object();

        private void PingBackgroundVerification() {
            var now = DateTime.UtcNow;
            if (lastEndedVerification >= lastScheduledVerification && lastScheduledVerification + VerificationInterval < now && lastEndedVerification + VerificationInterval < now) {
                lock (lockStart) {
                    if (lastScheduledVerification + VerificationInterval > now) return; //Exit from race condition.
                    lastScheduledVerification = now;
                    var appId = GetAppId();
                    //var verifyAction = new VerifyAction(httpClient, appId, pendingFeatures);
                    //verifyAction.Finished += verifyAction_Finished;
                    //verifyAction.Begin(); //If it failed to queue, we'll get it next time.
                }
            }
        }

        private void verifyAction_Finished(object sender, ActionFinishedEventArgs e) {
            lastEndedVerification = DateTime.UtcNow;
        }

        private Guid GetAppId() {
            if (config == null) throw new Exception("Please install the LicenseService plugin into a config before using.");

            string appStr = config.get("licenses.auto.appId", null);

            if (string.IsNullOrEmpty(appStr)) return Guid.Empty;

            return new Guid(appStr);
        }
    }
}
