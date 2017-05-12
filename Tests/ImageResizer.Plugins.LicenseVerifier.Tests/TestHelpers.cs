using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{
    class StringCacheEmpty : IPersistentStringCache
    {
        public string Get(string key) => null;

        public StringCachePutResult TryPut(string key, string value) => StringCachePutResult.WriteFailed;
    }

    class StringCacheMem : IPersistentStringCache
    {
        readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        public StringCachePutResult TryPut(string key, string value)
        {
            string current;
            if (cache.TryGetValue(key, out current) && current == value) {
                return StringCachePutResult.Duplicate;
            }
            cache[key] = value;
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
    }


    class LicenseStrings
    {
        public static readonly string Offlinev4DomainAcmeComCreative =
                "acme.com(R4Creative includes R4Creative R4Performance):S2luZDogdjQtZG9tYWluLW9mZmxpbmUKU2t1OiBSNENyZWF0aXZlCkRvbWFpbjogYWNtZS5jb20KT3duZXI6IEFjbWUgQ29ycApJc3N1ZWQ6IDIwMTctMDQtMjFUMDA6MDA6MDArMDA6MDAKRmVhdHVyZXM6IFI0Q3JlYXRpdmUgUjRQZXJmb3JtYW5jZQ==:eFLsTwUCEdQiEt34zdnzzxKFeEoAOrZoheE85LLYB9Pgx5wypsYpcG+58GlXUtgldbPyq+9e+m/ZeDhyqXPUkd6wk43EqUu07//20RE3XEWeKEGK1LBTNUJ6gfL9iPsA9qnSLpJNV7QLp9JxWI2VztvPUol9W5dORtUWtfzna+hujSQ5lym9vjVBaxsbsyRBS9x27lzGKUL+RoonHDYpeIolAnNu28WuBmFGQ3S3ALcNZ4dSjoapyAXQyEH07A5pQ/p18Vv5FqD24p7dh45BGMqJXLVuZli13kvdh812UQvKwyL223k9cEYiyV7F+YN6YHPL5/Ebrh1nYDC00/1b7A=="
            ;

        public static readonly string EliteSubscriptionPlaceholder =
                ":S2luZDogaWQKSWQ6IDExNTE1MzE2MgpTZWNyZXQ6IDFxZ2dxMTJ0MnF3Z3dnNGMyZDJkcXdmd2VxZncKSXNQdWJsaWM6IGZhbHNlCk1heFVuY2FjaGVkR3JhY2VNaW51dGVzOiA0ODA=:iJMbZFTUtC0PFl4mooTaLR1gXHLY7aFEXQvGUFbdHmwsA0M/NLq2CBIhujNgSvdQy5jWP5ylIBZCppIHDgiewfo1SZxLbQ424i8QLvrskUXPlau/1sQdmhOmjELDbcYslSujkbRIqzgIWJtw6IMxQwM+O/R+mdG4J+G1E81ERkpR4G/1Eu0DIxrNg0yn8Z13Qe5qjLwvBhdv9coSPXFEdlg7QhVWw4QuUl1GkxUC+qBTxVI2yYyQJtqFokLJOXlzRJUL21PZOw5BeBrzGkesq4XHcKrqGKGbuBQver6TjTL9jougNUY2HfKBuORfJwttwSip/Fr4A7CnYNGDajm0Fw=="
            ;

        public static readonly string EliteSubscriptionRemote =
                "ImageResizer Elite Subscription:SWQ6IDExNTE1MzE2MgpLaW5kOiBzdWJzY3JpcHRpb24KT3duZXI6IEFjbWUgQ29ycApJc3N1ZWQ6IDIwMTctMDQtMTlUMDM6MTE6NDJaCkV4cGlyZXM6IDIwMTctMTAtMTdUMDM6MTE6NDJaCklzUHVibGljOiB0cnVlClByb2R1Y3Q6IEltYWdlUmVzaXplciBFbGl0ZSBTdWJzY3JpcHRpb24KRmVhdHVyZXM6IFI0RWxpdGUgUjRDcmVhdGl2ZSBSNFBlcmZvcm1hbmNlClJlc3RyaWN0aW9uczogT25seSBmb3IgdGVzdGluZzsgbm90IGxlZ2FsIGZvciBwcm9kdWN0aW9uIHVzZS4=:P1m3QpHFHQvEgkozPCMzQjba8phkW3vgKp/Zrzk5auHTfwd02c8gf/4HPglquk0wMr7TEUm69AyjhWElZsx2lBfcYPHk+N6IM2K202Wvic+2WFwBpHvD6Mf7ZDEk2J+MKcY6awowJ0KuyQoRmec4CIzLUuER8OrucvZ/plZqBOehIybPLafbsk109kXCLQT8AIbcpP0hs/7H+CoYV9mir0tdz+rA1y0IBzWPStP1FMeGnT2JPdyjKwbi+N0Blsy/z832qil0Jhbscbk5o9rfKJpaQLihgnjiCTE3WIH7ZWZ2jguHaFtIkkw7+A+byx6kZhEfUz+pKZcqF4x1fpwfoA=="
            ;
    }

    class LicensedPlugin : ILicensedPlugin, IPlugin, ILicenseDiagnosticsProvider, IDiagnosticsProvider
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
                                   Clock);
            }
        }

        public LicensedPlugin(ILicenseManager mgr, ILicenseClock clock, params string[] codes)
        {
            this.codes = codes;
            this.mgr = mgr;
            Clock = clock ?? Clock;
        }

        public string ProvideDiagnostics() => Result.ProvideDiagnostics();

        public string ProvidePublicText() => Result.ProvidePublicDiagnostics();

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

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Result;
            return cache == null ? mgr.GetIssues() : mgr.GetIssues().Concat(cache.GetIssues());
        }
    }
}
