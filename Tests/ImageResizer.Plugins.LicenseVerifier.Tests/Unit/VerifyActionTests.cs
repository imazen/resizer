using ImageResizer.Plugins.LicenseVerifier.Async;
using ImageResizer.Plugins.LicenseVerifier.Http;
using NSubstitute;
using Should;
using System;
using System.Collections.Generic;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Unit {
    public class VerifyActionTests {

        public class WhenPendingFeaturesStillExistAfterUpdatingFromLocalCacheTests {

            ActionFinishedEventArgs finishedEventArgs;
            VerifyAction verifyAction;
            Guid featureOne;
            Guid featureTwo;
            Guid featureThree;
            ILicenseStore licenseStore;

            public WhenPendingFeaturesStillExistAfterUpdatingFromLocalCacheTests() {
                featureOne = Guid.NewGuid();
                featureTwo = Guid.NewGuid();
                featureThree = Guid.NewGuid();

                var httpClient = Substitute.For<IHttpClient>();

                string keyPair = Helpers.GenerateKeyPairXml();
                var domainLicense = Helpers.GenerateDomainLicense(new List<Guid> {
                    featureOne,
                    featureThree
                });

                httpClient.Send(Arg.Any<HttpRequest>()).Returns(new HttpResponse {
                    Content = Helpers.GenerateValidXmlResponseFromKeyHub(keyPair, new List<DomainLicense> { domainLicense })
                });

                licenseStore = Substitute.For<ILicenseStore>();
                licenseStore.GetLicenses().Returns(new List<byte[]>());

                var featureStates = new Dictionary<string, Dictionary<Guid, FeatureState>> {
                    {
                        "domain.com",
                        new Dictionary<Guid, FeatureState> {
                            { featureOne, FeatureState.Pending },
                            { featureTwo, FeatureState.Pending },
                            { featureThree, FeatureState.Pending }
                        }
                    }
                };

                var pendingFeatures = new Dictionary<string, List<Guid>> {
                    {
                        "domain.com",
                        new List<Guid> {
                            featureOne,
                            featureTwo,
                            featureThree
                        }
                    }
                };
                
                verifyAction = new VerifyAction(httpClient, keyPair, Guid.NewGuid(), licenseStore, featureStates, pendingFeatures);
                verifyAction.Finished += verifyAction_Finished;
            }

            public void Should_be_able_to_get_licenses_from_keyhub() {
                verifyAction.Begin();

                Helpers.WaitForCondition(() => verifyAction.State == ActionState.Success);

                verifyAction.Finished -= verifyAction_Finished;
            }

            private void verifyAction_Finished(object sender, ActionFinishedEventArgs e) {
                finishedEventArgs = e;
                verifyAction.PendingFeatures.Count.ShouldEqual(0);
                verifyAction.FeatureStates["domain.com"][featureOne].ShouldEqual(FeatureState.Enabled);
                verifyAction.FeatureStates["domain.com"][featureTwo].ShouldEqual(FeatureState.Rejected);
                verifyAction.FeatureStates["domain.com"][featureThree].ShouldEqual(FeatureState.Enabled);
                
                // TODO: Test the argument to be correct
                licenseStore.Received(1).SetLicenses(Arg.Any<ICollection<KeyValuePair<string, byte[]>>>());
            }
        }

        public class WhenLocalCacheContainsLicenseInformationTests {
            ActionFinishedEventArgs finishedEventArgs;
            VerifyAction verifyAction;
            Guid featureOne;
            Guid featureTwo;
            Guid featureThree;
            Guid featureFour;
            Guid featureFive;
            Guid featureSix;
            Guid featureSeven;
            ILicenseStore licenseStore;

            public WhenLocalCacheContainsLicenseInformationTests() {
                featureOne = Guid.NewGuid();
                featureTwo = Guid.NewGuid();
                featureThree = Guid.NewGuid();
                featureFour = Guid.NewGuid();
                featureFive = Guid.NewGuid();
                featureSix = Guid.NewGuid();
                featureSeven = Guid.NewGuid();

                var httpClient = Substitute.For<IHttpClient>();

                string keyPair = Helpers.GenerateKeyPairXml();

                // featureOne and featureThree belong to an expired license
                var expiredDomainLicense = new DomainLicense(
                    "expired.com",
                    "John Doe",
                    DateTime.UtcNow.AddYears(-1),
                    DateTime.UtcNow.AddHours(24).AddYears(-1),
                    new List<Guid> {
                        featureOne,
                        featureThree
                    }
                );

                var issued = DateTime.UtcNow;
                var expires = DateTime.UtcNow.AddHours(24);
                // featureTwo and featureFive have already been verified
                var cachedDomainLicense = new DomainLicense(
                    "domain.com",
                    "John Doe",
                    issued,
                    expires, 
                    new List<Guid> {
                        featureTwo,
                        featureFive
                    }
                );
                
                // featureSix and featureSeven are Pending, but only featureSix will be enabled
                var keyHubDomainLicense = new DomainLicense(
                    "domain.com", 
                    "John Doe", 
                    issued, 
                    expires, 
                    new List<Guid> {
                        featureTwo,
                        featureFour, // Rejected in the past is now enabled
                        featureFive,
                        featureSix // Pending new feature
                    }
                );

                httpClient.Send(Arg.Any<HttpRequest>()).Returns(new HttpResponse {
                    Content = Helpers.GenerateValidXmlResponseFromKeyHub(keyPair, new List<DomainLicense> { keyHubDomainLicense })
                });

                licenseStore = Substitute.For<ILicenseStore>();
                licenseStore.GetLicenses().Returns(new List<byte[]> {
                    expiredDomainLicense.SerializeAndEncrypt(keyPair),
                    cachedDomainLicense.SerializeAndEncrypt(keyPair)
                });

                var featureStates = new Dictionary<string, Dictionary<Guid, FeatureState>> {
                    {
                        "expired.com",
                        new Dictionary<Guid, FeatureState> {
                            { featureOne, FeatureState.Enabled },
                            { featureThree, FeatureState.Enabled }
                        }
                    },
                    {
                        "domain.com",
                        new Dictionary<Guid, FeatureState> {
                            { featureTwo, FeatureState.Enabled },
                            { featureFour, FeatureState.Rejected },
                            { featureFive, FeatureState.Enabled },
                            { featureSix, FeatureState.Pending },
                            { featureSeven, FeatureState.Pending }
                        }
                    }
                };

                var pendingFeatures = new Dictionary<string, List<Guid>> {
                    {
                        "domain.com",
                        new List<Guid> {
                            featureSix,
                            featureSeven
                        }
                    }
                };

                verifyAction = new VerifyAction(httpClient, keyPair, Guid.NewGuid(), licenseStore, featureStates, pendingFeatures);
                verifyAction.Finished += verifyAction_Finished;
            }

            public void Should_be_able_to_update_from_cache_and_keyhub() {
                verifyAction.Begin();

                Helpers.WaitForCondition(() => verifyAction.State == ActionState.Success);

                verifyAction.Finished -= verifyAction_Finished;
            }

            private void verifyAction_Finished(object sender, ActionFinishedEventArgs e) {
                finishedEventArgs = e;
                verifyAction.PendingFeatures.Count.ShouldEqual(0);
                verifyAction.FeatureStates["domain.com"][featureTwo].ShouldEqual(FeatureState.Enabled);
                verifyAction.FeatureStates["domain.com"][featureFour].ShouldEqual(FeatureState.Enabled);
                verifyAction.FeatureStates["domain.com"][featureFive].ShouldEqual(FeatureState.Enabled);
                verifyAction.FeatureStates["domain.com"][featureSix].ShouldEqual(FeatureState.Enabled);
                verifyAction.FeatureStates["domain.com"][featureSeven].ShouldEqual(FeatureState.Rejected);

                // TODO: Test the argument to be correct
                licenseStore.Received(1).SetLicenses(Arg.Any<ICollection<KeyValuePair<string, byte[]>>>());
            }
        }
    }
}
