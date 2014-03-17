using ImageResizer.Configuration;
using ImageResizer.Plugins.LicenseVerifier.Http;
using NSubstitute;
using Should;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Unit.LicenseServiceTests {
    // Each plugin will notify the service of it's use, by calling NotifyUse
    // The service will then mark the feature/plugin as pending if it's state is unknown.
    // We will ping and retrieve the license information based on the appid.
    // Once we have the information, we then go through the list and mark the features accrodingly.
    // We also cache the licenses retrieved for later use.
    public class NotifyUseTests {
        public class WhenStateOfFeatureIsUnknownTests {
            private readonly string domain;
            private readonly Guid featureOne;
            private readonly Guid featureTwo;
            private readonly Guid featureThree;
            private readonly LicenseService licenseService;

            public WhenStateOfFeatureIsUnknownTests() {
                domain = "domain.com";
                featureOne = Guid.NewGuid();
                featureTwo = Guid.NewGuid();
                featureThree = Guid.NewGuid();

                var httpClient = Substitute.For<IHttpClient>();
                licenseService = new LicenseService(httpClient);

                var config = new Config();
                licenseService.Install(config);

                licenseService.NotifyUse(domain, featureOne);
                licenseService.NotifyUse(domain, featureTwo);
                licenseService.NotifyUse(domain, featureThree);
            }

            public void Should_mark_features_as_pending() {
                licenseService.FeatureStates.Count.ShouldEqual(1);
                licenseService.FeatureStates[domain].Count.ShouldEqual(3);
                licenseService.FeatureStates[domain][featureOne].ShouldEqual(FeatureState.Pending);
                licenseService.FeatureStates[domain][featureTwo].ShouldEqual(FeatureState.Pending);
                licenseService.FeatureStates[domain][featureThree].ShouldEqual(FeatureState.Pending);
            }

            public void Should_add_pending_features_to_the_short_list() {
                licenseService.PendingFeatures.Count.ShouldEqual(1);
                licenseService.PendingFeatures[domain].Count.ShouldEqual(3);
                licenseService.PendingFeatures[domain].ShouldContain(featureOne);
                licenseService.PendingFeatures[domain].ShouldContain(featureTwo);
                licenseService.PendingFeatures[domain].ShouldContain(featureThree);
            }
        }
    }
}
