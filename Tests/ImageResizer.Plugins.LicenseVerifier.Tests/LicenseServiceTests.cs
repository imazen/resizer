using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier.Tests {
    // Each plugin will notify the service of it's use, by calling NotifyUse
    // The service will then mark the feature/plugin as pending if it's state is unknown.
    // We will ping and retrieve the license information based on the appid.
    // Once we have the information, we then go through the list and mark the features accrodingly.
    // We also cache the licenses retrieved for later use.
    public class LicenseServiceTests {
        public void Should_mark_features_we_do_not_know_the_status_of_pending() {
            var featureOne = Guid.NewGuid();
            var featureTwo = Guid.NewGuid();
            var featureThree = Guid.NewGuid();

            var licenseService = new LicenseService();
            
            licenseService.NotifyUse("domain.com", featureOne);
            licenseService.NotifyUse("domain.com", featureTwo);
            licenseService.NotifyUse("domain.com", featureThree);
        }
    }
}
