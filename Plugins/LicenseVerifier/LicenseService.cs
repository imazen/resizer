using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier {
    
    public class LicenseService : ILicenseService {

        public void NotifyUse(string domain, Guid feature) {
            throw new NotImplementedException();
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
    }
}
