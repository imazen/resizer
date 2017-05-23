using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;
using System.Drawing;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{
    public class LicenseEnforcerTests
    {
        public LicenseEnforcerTests(ITestOutputHelper output) { this.output = output; }

        readonly ITestOutputHelper output;


        bool IsWatermarking(Config c)
        {
            var j = new ImageJob("~/gradient.png", typeof(Bitmap), new Instructions("format=png&width=10"));
            c.Build(j);
            using (var b = j.Result as Bitmap) {
                var cornerPixel = b.GetPixel(b.Width - 1, b.Height - 1);
                var watermarked = cornerPixel.ToArgb() == Color.Red.ToArgb();
                output.WriteLine($"Watermarked={watermarked}, color={cornerPixel}");
                return watermarked;
            }
            
        }
        
        [Fact]
        public void Test_License_Enforcer_Exception_Web_Config()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
            var request = new RequestUrlProvider();

            var conf = new Config(new ResizerSection(
                @"<resizer><licenses licenseError='exception' licenseScope='local'>
      <maphost from='localhost' to='acme.com' />
      <license>" + LicenseStrings.Offlinev4DomainAcmeComCreative + "</license></licenses>" +
                "<plugins><add name='Gradient'/></plugins></resizer>"));

            conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R4Creative"));

            request.Url = null;
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://acme.com");
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://subdomain.acme.com");
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://localhost");
            Assert.False(IsWatermarking(conf));

            request.Url = new Uri("http://other.co");
            var e = Record.Exception(() => IsWatermarking(conf));
            Assert.NotNull(e);
            Assert.IsType<LicenseException>(e);
            
            Assert.Empty(mgr.GetIssues());
        }

        [Fact]
        public void Test_License_Enforcer_Exception()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
            var request = new RequestUrlProvider();

            var conf = new Config();

            conf.Plugins.LicenseScope = LicenseAccess.Local;
            conf.Plugins.AddLicense(LicenseStrings.Offlinev4DomainAcmeComCreative);
            conf.Plugins.LicenseError = LicenseErrorAction.Exception;
            conf.Plugins.AddLicensedDomainMapping("localhost", "acme.com");
            conf.Plugins.Install(new Gradient());
            conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R4Creative"));

            request.Url = null;
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://acme.com");
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://subdomain.acme.com");
            Assert.False(IsWatermarking(conf));
            request.Url = new Uri("http://localhost");
            Assert.False(IsWatermarking(conf));

            request.Url = new Uri("http://other.co");
            var e = Record.Exception(() => IsWatermarking(conf));
            Assert.NotNull(e);
            Assert.IsType<LicenseException>(e);

            Assert.Empty(mgr.GetIssues());
        }

        [Fact]
        public void Test_License_Enforcer_Watermark()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
            var request = new RequestUrlProvider();

            var conf = new Config();
            conf.Plugins.LicenseScope = LicenseAccess.Local;
            conf.Plugins.AddLicense(LicenseStrings.Offlinev4DomainAcmeComCreative);
            conf.Plugins.LicenseError = LicenseErrorAction.Watermark;
            conf.Plugins.AddLicensedDomainMapping("localhost", "acme.com");
            conf.Plugins.Install(new Gradient());
            conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R4Creative"));

            request.Url = null;
            Assert.False(IsWatermarking(conf));

            request.Url = new Uri("http://acme.com");
            Assert.False(IsWatermarking(conf));

            request.Url = new Uri("http://subdomain.acme.com");
            Assert.False(IsWatermarking(conf));

            request.Url = new Uri("http://localhost");
            Assert.False(IsWatermarking(conf));

            // We should watermark unlicensed domains
            request.Url = new Uri("http://other.co");
            Assert.True(IsWatermarking(conf));
            Assert.True(IsWatermarking(conf));

            Assert.Empty(mgr.GetIssues());
        }

        [Fact]
        public void Test_NoLicense()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
            var request = new RequestUrlProvider();

            var conf = new Config();
            conf.Plugins.LicenseScope = LicenseAccess.Local;
            conf.Plugins.Install(new Gradient());
            conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R4Creative"));

            // We don't watermark outside of http requests (even if there are no valid licenses)
            request.Url = null;
            Assert.False(IsWatermarking(conf));

            // But we certainly should be, here
            request.Url = new Uri("http://other.co");
            Assert.True(IsWatermarking(conf));

            Assert.Empty(mgr.GetIssues());
        }


        [Fact]
        public void Test_Revocations()
        {
            // set clock to present, and build date to far future
            var clock = new FakeClock("2017-04-25", "2022-01-01");

            foreach (var set in LicenseStrings.GetSets("Cancelled", "SoftRevocation", "HardRevocation")) {

                output.WriteLine($"Testing revocation for {set.Name}");
                var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
                MockHttpHelpers.MockRemoteLicense(mgr, HttpStatusCode.OK, set.Remote, null);

                var request = new RequestUrlProvider();

                var conf = new Config();
                conf.Plugins.LicenseScope = LicenseAccess.Local;
                conf.Plugins.AddLicense(set.Placeholder);
                conf.Plugins.LicenseError = LicenseErrorAction.Watermark;
                conf.Plugins.Install(new Gradient());
                conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R4Creative"));
                Assert.Equal(1, mgr.WaitForTasks());

                request.Url = null;
                Assert.False(IsWatermarking(conf));


                // We don't raise exceptions outside of http requests, unless there are absolutely no valid licenses
                conf.Plugins.LicenseError = LicenseErrorAction.Exception;
                // There are no valid licences
                request.Url = null;
                Assert.Throws<LicenseException>(() => IsWatermarking(conf));

                // But we certainly should be, here
                request.Url = new Uri("http://other.co");
                Assert.Throws<LicenseException>(() => IsWatermarking(conf));

                Assert.NotEmpty(mgr.GetIssues());
            }
        }

        [Fact]
        public void Test_DomainPerCore()
        {
            // set clock to present
            var clock = new FakeClock("2017-04-25", "2017-04-25");

            foreach (var set in LicenseStrings.GetSets("PerCore2Domains"))
            {
                var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock, new StringCacheMem());
                MockHttpHelpers.MockRemoteLicense(mgr, HttpStatusCode.OK, set.Remote, null);

                var request = new RequestUrlProvider();

                var conf = new Config();
                conf.Plugins.LicenseScope = LicenseAccess.Local;
                conf.Plugins.AddLicense(set.Placeholder);
                conf.Plugins.LicenseError = LicenseErrorAction.Exception;
                conf.Plugins.Install(new Gradient());
                conf.Plugins.Install(new EmptyLicenseEnforcedPlugin(new LicenseEnforcer<EmptyLicenseEnforcedPlugin>(mgr, mgr, request.Get), "R_Performance"));

                Assert.Equal(1, mgr.WaitForTasks());
                // We don't raise exceptions without a request url, unless there are absolutely no valid licenses
                request.Url = null;
                Assert.False(IsWatermarking(conf));

                // We never watermark outside of http requests
                conf.Plugins.LicenseError = LicenseErrorAction.Watermark;
                Assert.False(IsWatermarking(conf));

                conf.Plugins.LicenseError = LicenseErrorAction.Exception;

                //We should not raise an exception on our domains
                request.Url = new Uri("http://acme.com");
                Assert.False(IsWatermarking(conf));
                request.Url = new Uri("http://acmestaging.com");
                Assert.False(IsWatermarking(conf));

                // We should raise an exception on other domains
                request.Url = new Uri("http://other.co");
                Assert.Throws<LicenseException>(() => IsWatermarking(conf));


                Assert.Empty(mgr.GetIssues());
            }
        }
    }
}