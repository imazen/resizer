using ImageResizer.Configuration;
using ImageResizer.Plugins.LicenseVerifier.Http;
using NSubstitute;
using Should;
using System.Collections.Generic;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Unit.LicenseServiceTests {
    public class InstallUninstallTests {
        private readonly LicenseService licenseService;

        public InstallUninstallTests() {
            var httpClient = Substitute.For<IHttpClient>();
            licenseService = new LicenseService(httpClient);
        }

        public void Should_install_license_service_into_config() {
            var config = new Config();
            var plugin = licenseService.Install(config);

            var installedLicenseServicePlugins = config.Plugins.GetPlugins(typeof(LicenseService));

            installedLicenseServicePlugins.Count.ShouldEqual(1);
            installedLicenseServicePlugins[0].ShouldBeType(typeof(LicenseService));
            installedLicenseServicePlugins[0].ShouldBeSameAs(licenseService);
            plugin.ShouldNotBeNull();
            plugin.ShouldBeSameAs(licenseService);
        }

        public void Should_never_uninstall_license_service() {
            var config = new Config();
            licenseService.Install(config);

            var success = licenseService.Uninstall(config);
            success.ShouldBeFalse();
            
            var installedLicenseServicePlugins = config.Plugins.GetPlugins(typeof(LicenseService));
            installedLicenseServicePlugins.Count.ShouldEqual(1);
            installedLicenseServicePlugins[0].ShouldBeType(typeof(LicenseService));
            installedLicenseServicePlugins[0].ShouldBeSameAs(licenseService);
        }
    }
}

