using ImageResizer.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.LicenseVerifier.Tests
{

    class StringCacheEmpty : IPersistentStringCache
    {
        public string Get(string key)
        {
            return null;
        }

        public StringCachePutResult TryPut(string key, string value)
        {
            return StringCachePutResult.WriteFailed;
        }
    }

    class StringCacheMem : IPersistentStringCache
    {
        ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
        public StringCachePutResult TryPut(string key, string value)
        {
            string current;
            if (cache.TryGetValue(key, out current) && current == value)
            {
                return StringCachePutResult.Duplicate;
            }
            cache[key] = value;
            return StringCachePutResult.WriteComplete;
        }

        public string Get(string key)
        {
            string current;
            if (cache.TryGetValue(key, out current))
            {
                return current;
            }
            return null;
        }
    }

    class PublicKeys
    {
        public static RSADecryptPublic Test
        {
            get
            {
                var pubkey_modulus = "21403964489040138713896545869406851734432500305180577929806228393671667423170541918856531956008546071841016201645150244452266439995041173092354230946610429300967887006960186647111152810965360763586210200652502467947786453111507369142658284220331513416234497960844309808252643534631142917589553418044306073242485021092396181183125381004682521853943025560860753079004948017667604884278401445729443478586697229583656851019218046599746243419376456426788044497274378001221965538712352348475726349124652450874653832672820100829574087311416068166524423905971193163418806721436095962165082262760557869093554827824418663362349";
                var pubkey_exponent = "65537";
                return new RSADecryptPublic(BigInteger.Parse(pubkey_modulus), BigInteger.Parse(pubkey_exponent));
            }
        }
        public static RSADecryptPublic Production
        {
            get
            {
                var pubkey_modulus = "23949488589991837273662465276682907968730706102086698017736172318753209677546629836371834786541857453052840819693021342491826827766290334135101781149845778026274346770115575977554682930349121443920608458091578262535319494351868006252977941758848154879863365934717437651379551758086088085154566157115250553458305198857498335213985131201841998493838963767334138323078497945594454883498534678422546267572587992510807296283688571798124078989780633040004809178041347751023931122344529856055566400640640925760832450260419468881471181281199910469396775343083815780600723550633987799763107821157001135810564362648091574582493";
                var pubkey_exponent = "65537";
                return new RSADecryptPublic(BigInteger.Parse(pubkey_modulus), BigInteger.Parse(pubkey_exponent));
            }
        }
    }

    class LicenseStrings
    {
        public static readonly string EliteSubscriptionPlaceholder = ":S2luZDogaWQKSWQ6IDExNTE1MzE2MgpTZWNyZXQ6IDFxZ2dxMTJ0MnF3Z3dnNGMyZDJkcXdmd2VxZncKSXNQdWJsaWM6IGZhbHNlCk1heFVuY2FjaGVkR3JhY2VNaW51dGVzOiA0ODA=:iJMbZFTUtC0PFl4mooTaLR1gXHLY7aFEXQvGUFbdHmwsA0M/NLq2CBIhujNgSvdQy5jWP5ylIBZCppIHDgiewfo1SZxLbQ424i8QLvrskUXPlau/1sQdmhOmjELDbcYslSujkbRIqzgIWJtw6IMxQwM+O/R+mdG4J+G1E81ERkpR4G/1Eu0DIxrNg0yn8Z13Qe5qjLwvBhdv9coSPXFEdlg7QhVWw4QuUl1GkxUC+qBTxVI2yYyQJtqFokLJOXlzRJUL21PZOw5BeBrzGkesq4XHcKrqGKGbuBQver6TjTL9jougNUY2HfKBuORfJwttwSip/Fr4A7CnYNGDajm0Fw==";
        public static readonly string EliteSubscriptionRemote = "ImageResizer Elite Subscription:SWQ6IDExNTE1MzE2MgpLaW5kOiBzdWJzY3JpcHRpb24KT3duZXI6IEFjbWUgQ29ycApJc3N1ZWQ6IDIwMTctMDQtMTlUMDM6MTE6NDJaCkV4cGlyZXM6IDIwMTctMTAtMTdUMDM6MTE6NDJaCklzUHVibGljOiB0cnVlClByb2R1Y3Q6IEltYWdlUmVzaXplciBFbGl0ZSBTdWJzY3JpcHRpb24KRmVhdHVyZXM6IFI0RWxpdGUgUjRDcmVhdGl2ZSBSNFBlcmZvcm1hbmNlClJlc3RyaWN0aW9uczogT25seSBmb3IgdGVzdGluZzsgbm90IGxlZ2FsIGZvciBwcm9kdWN0aW9uIHVzZS4=:P1m3QpHFHQvEgkozPCMzQjba8phkW3vgKp/Zrzk5auHTfwd02c8gf/4HPglquk0wMr7TEUm69AyjhWElZsx2lBfcYPHk+N6IM2K202Wvic+2WFwBpHvD6Mf7ZDEk2J+MKcY6awowJ0KuyQoRmec4CIzLUuER8OrucvZ/plZqBOehIybPLafbsk109kXCLQT8AIbcpP0hs/7H+CoYV9mir0tdz+rA1y0IBzWPStP1FMeGnT2JPdyjKwbi+N0Blsy/z832qil0Jhbscbk5o9rfKJpaQLihgnjiCTE3WIH7ZWZ2jguHaFtIkkw7+A+byx6kZhEfUz+pKZcqF4x1fpwfoA==";
    }

    internal class LicensedPlugin : ILicensedPlugin, IPlugin
    {
        string[] codes;
        ILicenseManager mgr;
        public LicensedPlugin(ILicenseManager mgr, params string[] codes)
        {
            this.codes = codes;
            this.mgr = mgr;
        }
        public IEnumerable<string> LicenseFeatureCodes
        {
            get
            {
                return codes;
            }
        }

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            mgr.Monitor(c);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }


}
