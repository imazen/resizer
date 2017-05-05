using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{


    public class LicenseManagerTests
    {
        Mock<HttpMessageHandler> MockRemoteLicense(LicenseManagerSingleton mgr, HttpStatusCode code, string value, Action<HttpRequestMessage, CancellationToken> callback)
        {
            var handler = new Mock<HttpMessageHandler>();
            var method = handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                 .Returns(Task.Run(() => new HttpResponseMessage(code) {
                     Content = new StringContent(value, UTF8Encoding.UTF8)
                 }));

            if (callback != null) method.Callback(callback);

            method.Verifiable("SendAsync must be called");

            mgr.SetHttpMessageHandler(handler.Object, true);
            return handler;
        }

        Mock<HttpMessageHandler> MockRemoteLicenseException(LicenseManagerSingleton mgr, WebExceptionStatus status)
        {
            var ex = new HttpRequestException("Mock failure", new WebException("Mock failure", status));
            var handler = new Mock<HttpMessageHandler>();
            var method = handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(ex);

            method.Verifiable("SendAsync must be called");

            mgr.SetHttpMessageHandler(handler.Object, true);
            return handler;
        }


        [Fact]
        public void Test_Remote_License_Success()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock)
            {
                Cache = new StringCacheMem()
            };
            Uri invokedUri = null;
            var httpHandler = MockRemoteLicense(mgr, HttpStatusCode.OK, LicenseStrings.EliteSubscriptionRemote, (r, c) =>
            {
                invokedUri = r.RequestUri;
            });
           
            Config conf = new Config();
            conf.Plugins.LicenseScope = LicenseAccess.Local;
            conf.Plugins.Install(new LicensedPlugin(mgr, clock, "R4Elite"));
            conf.Plugins.AddLicense(LicenseStrings.EliteSubscriptionPlaceholder);

            Assert.Equal(1, mgr.WaitForTasks());
            Assert.Empty(mgr.GetIssues());

            Mock.Verify(httpHandler);
            Assert.StartsWith("https://s3-us-west-2.amazonaws.com/licenses.imazen.net/v1/licenses/latest/1qggq12t2qwgwg4c2d2dqwfweqfw.txt?", invokedUri.ToString());


            Assert.NotNull(mgr.GetAllLicenses().First().FetchedLicense());

            var result = new LicenseComputation(conf, ImazenPublicKeys.Test, mgr, mgr, clock);

            Assert.True(result.LicensedForRequestUrl(new Uri("http://anydomain")));
            Assert.Equal(0, mgr.WaitForTasks());
            Assert.Empty(mgr.GetIssues());
            Assert.NotNull(conf.GetDiagnosticsPage());

        }



        [Fact]
        public void Test_Offline_License_Success()
        {
            var clock = new RealClock();
            var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock)
            {
                Cache = new StringCacheMem()
            };
            Config conf = new Config();
            conf.Plugins.LicenseScope = LicenseAccess.Local;
            conf.Plugins.Install(new LicensedPlugin(mgr, clock, "R4Creative"));
            conf.Plugins.AddLicense(LicenseStrings.Offlinev4DomainAcmeComCreative);

            Assert.Equal(0, mgr.WaitForTasks());
            Assert.Empty(mgr.GetIssues());

            Assert.NotNull(mgr.GetAllLicenses().First());

            var result = new LicenseComputation(conf, ImazenPublicKeys.Test, mgr, mgr, clock);

            Assert.True(result.LicensedForRequestUrl(new Uri("http://acme.com")));
            Assert.Equal(0, mgr.WaitForTasks());
            Assert.Empty(mgr.GetIssues());
            Assert.NotNull(conf.GetDiagnosticsPage());

        }
        [Fact]
        public void Test_Caching_With_Timeout()
        {
            var clock = new RealClock();
            var cache = new StringCacheMem();

            // Populate cache
            {
                var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock)
                {
                    Cache = cache
                };
                var httpHandler = MockRemoteLicense(mgr, HttpStatusCode.OK, LicenseStrings.EliteSubscriptionRemote, null);

                Config conf = new Config();
                conf.Plugins.LicenseScope = LicenseAccess.Local;
                conf.Plugins.Install(new LicensedPlugin(mgr, clock, "R4Elite"));
                conf.Plugins.AddLicense(LicenseStrings.EliteSubscriptionPlaceholder);

                mgr.WaitForTasks();

                var result = new LicenseComputation(conf, ImazenPublicKeys.Test, mgr, mgr, clock);
                Assert.True(result.LicensedForRequestUrl(new Uri("http://anydomain")));

                Assert.Empty(mgr.GetIssues());
            }

            // Use cache
            {
                var mgr = new LicenseManagerSingleton(ImazenPublicKeys.Test, clock)
                {
                    Cache = cache
                };
                var httpHandler = MockRemoteLicenseException(mgr, WebExceptionStatus.NameResolutionFailure);

                Config conf = new Config();
                conf.Plugins.LicenseScope = LicenseAccess.Local;
                conf.Plugins.Install(new LicensedPlugin(mgr, clock, "R4Elite"));
                conf.Plugins.AddLicense(LicenseStrings.EliteSubscriptionPlaceholder);

                mgr.WaitForTasks();

                var result = new LicenseComputation(conf, ImazenPublicKeys.Test, mgr, mgr, clock);
                Assert.True(result.LicensedForRequestUrl(new Uri("http://anydomain")));
                Assert.NotEmpty(mgr.GetIssues());
                Assert.NotNull(conf.GetDiagnosticsPage());
            }

        }

        [Fact]
        public void Test_GlobalCache()
        {
            // We don't want to test the singleton

            var unique_prefix = "test_cache_" + Guid.NewGuid().ToString() + "__";
            var cacheType = Type.GetType("ImageResizer.Plugins.WriteThroughCache, ImageResizer");
            var cacheCtor = cacheType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {typeof(string) }, null);
            var cacheInstance = cacheCtor.Invoke(new[] { unique_prefix });


            var c = new PeristentGlobalStringCache();
            typeof(PeristentGlobalStringCache).GetField("cache", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(c, cacheInstance);

            Assert.Equal(StringCachePutResult.WriteComplete, c.TryPut("a", "b"));
            Assert.Equal(StringCachePutResult.Duplicate, c.TryPut("a", "b"));
            Assert.Equal("b", c.Get("a"));
            Assert.Equal(null, c.Get("404"));
            Assert.Equal(StringCachePutResult.WriteComplete, c.TryPut("a", null));
            Assert.NotNull(Config.Current.GetDiagnosticsPage());
        }

        //Test network grace period

        // Test invalid content
        // test mixed


            //[Fact]
            //public void Test_Uncached_403()
            //{
            //    var mgr = new LicenseManagerSingleton()
            //    {
            //        Cache = new StringCacheEmpty()
            //    };

            //    var httpHandler = MockRemoteLicense(mgr, HttpStatusCode.Forbidden, "", null);

            //    Config conf = new Config();
            //    conf.Plugins.LicenseScope = LicenseAccess.Local;
            //    conf.Plugins.Install(new LicensedPlugin(mgr, "R4Elite"));
            //    conf.Plugins.AddLicense(LicenseStrings.EliteSubscriptionPlaceholder);

            //    var tasks = mgr.GetAsyncTasksSnapshot().ToArray();
            //    Assert.Equal(1, tasks.Count());
            //    Task.WaitAll(tasks);

            //    mgr.Heartbeat();
            //    Mock.Verify(httpHandler);

            //    var sink = new IssueSink("LicenseManagerTest");
            //    var result = new LicenseComputation(conf, PublicKeys.Test, sink, mgr);


            //    //Assert.NotNull(mgr.GetAllLicenses().First().GetFreshRemoteLicense());
            //    Assert.True(result.LicensedForHost("any"));

            //    tasks = mgr.GetAsyncTasksSnapshot().ToArray();
            //    Assert.Equal(0, tasks.Count());
            //    Task.WaitAll(tasks);

            //}
            // Test with cache states - none, 404, valid, expired, and invalid
            // Test with timeout, 403/404, valid, and invalid response



            //var cacheMock = new Mock<IPersistentStringCache>();
            //cacheMock.Setup((c) => c.Get(It.IsAny<string>())).Returns("404").Verifiable("Cache.Get must be called");
            //cacheMock.Setup((c) => c.TryPut(It.IsAny<string>(), It.IsAny<string>())).Returns(StringCachePutResult.WriteFailed).Verifiable("Cache.TryPut must be called");

            //mgr.Cache = cacheMock.Object;

        }

}
