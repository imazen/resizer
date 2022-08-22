using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{


    class FakeClock : ILicenseClock
    {
        DateTimeOffset now;
        readonly DateTimeOffset built;

        public FakeClock(string date, string buildDate)
        {
            now = DateTimeOffset.Parse(date);
            built = DateTimeOffset.Parse(buildDate);
        }

        public void AdvanceSeconds(long seconds) { now = now.AddSeconds(seconds); }
        public DateTimeOffset GetUtcNow() => now;
        public long GetTimestampTicks() => now.Ticks;
        public long TicksPerSecond { get; } = Stopwatch.Frequency;
        public DateTimeOffset? GetBuildDate() => built;
        public DateTimeOffset? GetAssemblyWriteDate() => built;
    }

    /// <summary>
    /// Time advances normally, but starting from the givien date instead of now
    /// </summary>
    class OffsetClock : ILicenseClock
    {
        TimeSpan offset;
        long ticksOffset;
        readonly DateTimeOffset built;

        public OffsetClock(string date, string buildDate)
        {
            offset = DateTimeOffset.UtcNow - DateTimeOffset.Parse(date);
            ticksOffset = Stopwatch.GetTimestamp() - 1;
            built = DateTimeOffset.Parse(buildDate);
        }

        public void AdvanceSeconds(int seconds) { offset = offset + new TimeSpan(0,0, seconds); }
        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow - offset;
        public long GetTimestampTicks() => Stopwatch.GetTimestamp() - ticksOffset;
        public long TicksPerSecond { get; } = Stopwatch.Frequency;
        public DateTimeOffset? GetBuildDate() => built;
        public DateTimeOffset? GetAssemblyWriteDate() => built;
    }

    class StringCacheEmpty : IPersistentStringCache
    {
        public string Get(string key) => null;

        public DateTime? GetWriteTimeUtc(string key) => null;

        public StringCachePutResult TryPut(string key, string value) => StringCachePutResult.WriteFailed;
    }

    class StringCacheMem : IPersistentStringCache
    {
        readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
        readonly ConcurrentDictionary<string, DateTime> cacheWrite = new ConcurrentDictionary<string, DateTime>();

        public StringCachePutResult TryPut(string key, string value)
        {
            string current;
            if (cache.TryGetValue(key, out current) && current == value) {
                return StringCachePutResult.Duplicate;
            }
            cache[key] = value;
            cacheWrite[key] = DateTime.UtcNow;
            return StringCachePutResult.WriteComplete;
        }

        public string Get(string key)
        {
            string current;
            if (cache.TryGetValue(key, out current)) {
                return current;
            }
            return null;
        }

        public DateTime? GetWriteTimeUtc(string key)
        {
            DateTime current;
            if (cacheWrite.TryGetValue(key, out current))
            {
                return current;
            }
            return null;
        }
    }


    class LicensedPlugin : ILicensedPlugin, IPlugin, IDiagnosticsProviderFactory, IIssueProvider
    {
        Config c;
        Computation cache;
        readonly string[] codes;

        readonly ILicenseManager mgr;
        ILicenseClock Clock { get; } = new RealClock();

        Computation Result
        {
            get {
                if (cache?.ComputationExpires != null && cache.ComputationExpires.Value < Clock.GetUtcNow()) {
                    cache = null;
                }
                return cache = cache ?? new Computation(c, ImazenPublicKeys.All, c.configurationSectionIssues, mgr,
                                   Clock, true);
            }
        }

        public LicensedPlugin(ILicenseManager mgr, ILicenseClock clock, params string[] codes)
        {
            this.codes = codes;
            this.mgr = mgr;
            Clock = clock ?? Clock;
        }


        public IEnumerable<string> LicenseFeatureCodes => codes;

        public IPlugin Install(Config c)
        {
            this.c = c;

            mgr.MonitorLicenses(c);
            mgr.MonitorHeartbeat(c);

            // Ensure our cache is appropriately invalidated
            cache = null;
            mgr.AddLicenseChangeHandler(this, (me, manager) => me.cache = null);

            // And repopulated, so that errors show up.
            if (Result == null) {
                throw new ApplicationException("Failed to populate license result");
            }

            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<IIssue> GetIssues() => mgr.GetIssues().Concat(Result?.GetIssues() ?? Enumerable.Empty<IIssue>());

        public object GetDiagnosticsProvider() => Result;
    }
    class RequestUrlProvider
    {
        public Uri Url { get; set; } = null;
        public Uri Get() => Url;
    }

    class EmptyLicenseEnforcedPlugin : ILicensedPlugin, IPlugin
    {
        readonly string[] codes;

        public IEnumerable<string> LicenseFeatureCodes => codes;
        
        public LicenseEnforcer<EmptyLicenseEnforcedPlugin> EnforcerPlugin { get; private set; }

        public EmptyLicenseEnforcedPlugin(LicenseEnforcer<EmptyLicenseEnforcedPlugin> enforcer, params string[] codes)
        {
            EnforcerPlugin = enforcer;
            this.codes = codes;
        }
        
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            EnforcerPlugin = c.Plugins.GetOrInstall(EnforcerPlugin);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }

    static class MockHttpHelpers
    {
        public static Mock<HttpMessageHandler> MockRemoteLicense(LicenseManagerSingleton mgr, HttpStatusCode code, string value,
                                                   Action<HttpRequestMessage, CancellationToken> callback)
        {
            var handler = new Mock<HttpMessageHandler>();
            var method = handler.Protected()
                                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                                    ItExpr.IsAny<CancellationToken>())
                                .Returns(Task.Run(() => new HttpResponseMessage(code)
                                {
                                    Content = new StringContent(value, System.Text.Encoding.UTF8)
                                }));

            if (callback != null)
            {
                method.Callback(callback);
            }

            method.Verifiable("SendAsync must be called");

            mgr.SetHttpMessageHandler(handler.Object, true);
            return handler;
        }

        public static Mock<HttpMessageHandler> MockRemoteLicenseException(LicenseManagerSingleton mgr, WebExceptionStatus status)
        {
            var ex = new HttpRequestException("Mock failure", new WebException("Mock failure", status));
            var handler = new Mock<HttpMessageHandler>();
            var method = handler.Protected()
                                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                                    ItExpr.IsAny<CancellationToken>())
                                .ThrowsAsync(ex);

            method.Verifiable("SendAsync must be called");

            mgr.SetHttpMessageHandler(handler.Object, true);
            return handler;
        }
    }
}
;