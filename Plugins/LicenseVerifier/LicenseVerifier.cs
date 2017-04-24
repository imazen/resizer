using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ImageResizer.Plugins.LicenseVerifier
{
    internal class RSADecryptPublic
    {
        public RSADecryptPublic(BigInteger modulus, BigInteger exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
            BlockSize = Modulus.ToByteArray().Length;
        }
        public BigInteger Modulus { get; private set; }
        public BigInteger Exponent { get; private set; }
        public int BlockSize { get; private set; }
        public byte[] DecryptPublic(byte[] input_bytes)
        {
            if (input_bytes.Length > BlockSize)
            {
                throw new ArgumentOutOfRangeException("input", "input too large for RSA cipher block size.");
            }
            var input = new BigInteger(input_bytes.Reverse().Concat(Enumerable.Repeat<byte>(0, 1)).ToArray()); //Add a zero to prevent interpretation as twos-complement

            if (input.CompareTo(Modulus) >= 0)
            {
                throw new ArgumentOutOfRangeException("input", "input too large for RSA modulus.");
            }
            int signature_padding = 0; //1
            return BigInteger.ModPow(input, Exponent, Modulus).ToByteArray().Skip(signature_padding).Reverse().SkipWhile(v => v != 0).Skip(1 + signature_padding).ToArray();
        }

    }


    internal class LicenseDetails : ILicenseDetails
    {

        public DateTime? Issued { get; private set; }
        public DateTime? Expires { get; private set; }
        public DateTime? NoReleasesAfter { get; private set; }
        public IReadOnlyDictionary<string, string> GetPairs()
        {
            return pairs;
        }

        private Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public LicenseDetails(string plaintext)
        {
            string[] lines = plaintext.Split('\n');
            foreach (string l in lines)
            {
                int colon = l.IndexOf(':');
                if (colon < 1) continue;
                string key = l.Substring(0, colon).Trim();
                string value = l.Substring(colon + 1).Trim();

                if (String.Equals(key, "issued", StringComparison.InvariantCultureIgnoreCase))
                {
                    Issued = DateTime.Parse(value);
                }
                else if (String.Equals(key, "expires", StringComparison.InvariantCultureIgnoreCase))
                {
                    Expires = DateTime.Parse(value);
                }
                else if (String.Equals(key, "subscriptionexpirationdate", StringComparison.InvariantCultureIgnoreCase))
                {
                    NoReleasesAfter = DateTime.Parse(value);
                }
                pairs.Add(key, value);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var pair in pairs)
            {
                sb.AppendFormat("{0}: {1}\n", pair.Key, pair.Value);
            }
            return sb.ToString();
        }


        public string Get(string key)
        {
            string s;
            if (pairs.TryGetValue(key, out s))
            {
                return s;
            }
            else
            {
                return null;
            }
        }
    }

    internal class LicenseBlob : ILicenseBlob
    {
        private byte[] InfoUTF8 { get; set; }
        private string Info { get; set; }

        private LicenseDetails Details { get; set; }
        private byte[] Signature { get; set; }

        private IEnumerable<string> Comments { get; set; }

        public string Original { get; private set; }

        public static LicenseBlob Deserialize(string license)
        {
            var parts = license.Split(':').Select(s => s.Trim()).ToList();
            if (parts.Count < 2) throw new ArgumentException("Not enough segments in license key; failed to deserialize: \"" + license + "\"", "license");

            var b = new LicenseBlob();
            b.Original = license;
            b.Signature = Convert.FromBase64String(parts[parts.Count - 1]);
            b.InfoUTF8 = Convert.FromBase64String(parts[parts.Count - 2]);
            b.Info = System.Text.Encoding.UTF8.GetString(b.InfoUTF8);
            b.Comments = parts.Take(parts.Count - 2);
            b.Details = new LicenseDetails(b.Info);
            return b;
        }
        public override string ToString()
        {
            return Original;
        }

        public byte[] GetSignature()
        {
            return Signature;
        }

        public byte[] GetDataUTF8()
        {
            return InfoUTF8;
        }

        public ILicenseDetails GetParsed()
        {
            return Details;
        }
    }



    internal class ImazenPublicKeys
    {
        static ImazenPublicKeys()
        {
            {
                var pubkey_modulus = "21403964489040138713896545869406851734432500305180577929806228393671667423170541918856531956008546071841016201645150244452266439995041173092354230946610429300967887006960186647111152810965360763586210200652502467947786453111507369142658284220331513416234497960844309808252643534631142917589553418044306073242485021092396181183125381004682521853943025560860753079004948017667604884278401445729443478586697229583656851019218046599746243419376456426788044497274378001221965538712352348475726349124652450874653832672820100829574087311416068166524423905971193163418806721436095962165082262760557869093554827824418663362349";
                var pubkey_exponent = "65537";
                Test = new[] { new RSADecryptPublic(BigInteger.Parse(pubkey_modulus), BigInteger.Parse(pubkey_exponent)) };
            }
            {
                var pubkey_modulus = "23949488589991837273662465276682907968730706102086698017736172318753209677546629836371834786541857453052840819693021342491826827766290334135101781149845778026274346770115575977554682930349121443920608458091578262535319494351868006252977941758848154879863365934717437651379551758086088085154566157115250553458305198857498335213985131201841998493838963767334138323078497945594454883498534678422546267572587992510807296283688571798124078989780633040004809178041347751023931122344529856055566400640640925760832450260419468881471181281199910469396775343083815780600723550633987799763107821157001135810564362648091574582493";
                var pubkey_exponent = "65537";
                Production = new[] { new RSADecryptPublic(BigInteger.Parse(pubkey_modulus), BigInteger.Parse(pubkey_exponent)) };
            }
            All = Production.Concat(Test).ToArray();
        }

        public static IEnumerable<RSADecryptPublic> Test { get; private set; }
        
        public static IEnumerable<RSADecryptPublic> Production { get; private set; }

        public static IEnumerable<RSADecryptPublic> All { get; private set; }
    }

    internal class LicenseValidator
    {


        public bool Validate(ILicenseBlob b, IEnumerable<RSADecryptPublic> trustedKeys, StringBuilder log)
        {
            return trustedKeys.Any(p =>
            {
                var signature = b.GetSignature();
                var hash = new SHA512Managed().ComputeHash(b.GetDataUTF8());
                var decrypted_bytes = p.DecryptPublic(signature);
                var valid = hash.SequenceEqual(decrypted_bytes);

                if (log != null)
                {
                    log.AppendLine("Using public exponent " + p.Exponent.ToString() + " and modulus " + p.Modulus.ToString());
                    log.AppendLine("Encrypted bytes: " + BitConverter.ToString(signature).ToLower().Replace("-", ""));
                    log.AppendLine("Decrypted sha512: " + BitConverter.ToString(decrypted_bytes).ToLower().Replace("-", ""));
                    log.AppendLine("Expected sha512: " + BitConverter.ToString(hash).ToLower().Replace("-", ""));
                    log.AppendLine(valid ? "Success!" : "Not a match.");
                }
                return valid;
            });
        }

        static bool Test_Generic(StringBuilder log)
        {
            if (log != null) log.AppendLine("Generic license decryption self-test");
            var exp = BigInteger.Parse("65537");
            var mod = BigInteger.Parse("28178177427582259905122756905913963624440517746414712044433894631438407111916149031583287058323879921298234454158166031934230083094710974550125942791690254427377300877691173542319534371793100994953897137837772694304619234054383162641475011138179669415510521009673718000682851222831185756777382795378538121010194881849505437499638792289283538921706236004391184253166867653735050981736002298838523242717690667046044130539971131293603078008447972889271580670305162199959939004819206804246872436611558871928921860176200657026263241409488257640191893499783065332541392967986495144643652353104461436623253327708136399114561");
            var license_str = "localhost:RG9tYWluOiBsb2NhbGhvc3QKT3duZXI6IEV2ZXJ5b25lCklzc3VlZDogMjAxNS0wMy0yOFQwOTozNjo1OVoKRmVhdHVyZXM6IFI0RWxpdGUgUjRDcmVhdGl2ZSBSNFBlcmZvcm1hbmNlCg==:h6D+kIXbF3qmvmW2gDpb+b4gdxBjnrkZLvSzXmEnqKAywNJNpTdFekpTOB4SwU14WbTeVyWwvFngHax7WuHBV+0WkQ5lDqKFaRW32vj8CJQeG8Wvnyj9PaNGaS/FpKhNjZbDEmh3qqirBp2NR0bpN4QbhP9NMy7+rOMo0nynAruwWvJKCnuf7mWWdb9a5uTZO9OUcSeS/tY8QaNeIhaCnhPe0Yx9qvOXe5nMnl10CR9ur+EtS54d1qzBGHqN/3oFhiB+xlqNELwz23qR4c8HxbTEyNarkG4CZx8CbbgJfHmPxAYGJTTBTPJ+cdah8MJR16Ta36cRZ2Buy8XYo/nf1g==";

            return Validate(license_str, mod, exp, log);
        }

        static bool Validate(string license_str, BigInteger mod, BigInteger exp, StringBuilder log)
        {
            var blob = LicenseBlob.Deserialize(license_str);
            if (log != null) log.AppendLine("---------------------------------------------");
            if (log != null) log.AppendLine("Parsed info: " + blob.GetParsed().ToString());
            if (log != null) log.AppendLine("Plaintext hash: " + BitConverter.ToString(new SHA512Managed().ComputeHash(blob.GetDataUTF8())).ToLower().Replace("-", ""));


            var decryptor = new[] { new RSADecryptPublic(mod, exp) };

            return new LicenseValidator().Validate(blob, decryptor, log);
        }

        static int Main(string[] args)
        {



            var modulus = args[0];
            var exponent = args[1];

            var mod = BigInteger.Parse(UTF8Encoding.UTF8.GetString(Convert.FromBase64String(modulus)));
            var exp = BigInteger.Parse(UTF8Encoding.UTF8.GetString(Convert.FromBase64String(exponent)));


            string license_str = null;

            using (var s = System.Console.OpenStandardInput())
            using (var sr = new StreamReader(s, System.Text.Encoding.UTF8))
            {
                license_str = sr.ReadToEnd();
            }

            bool debug = args.Length > 2 && args[2] == "-d";
            StringBuilder log = debug ? new StringBuilder() : null;


            int result = (debug && !Test_Generic(log)) ? 4 : 0;

            if (!Validate(license_str, mod, exp, log))
            {
                result = 2 ^ result;
            }
            if (log != null && result != 0) Console.WriteLine(log.ToString());
            return result;
        }
    }
}
