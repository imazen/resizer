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
                throw new ArgumentOutOfRangeException("input", "input too large for RSA cipher.");
            }
            var input = new BigInteger(input_bytes.Reverse().Concat(Enumerable.Repeat<byte>(0, 1)).ToArray()); //Add a zero to prevent interpretation as twos-complement

            if (input.CompareTo(Modulus) >= 0)
            {
                throw new ArgumentOutOfRangeException("input", "input too large for RSA cipher.");
            }
            int signature_padding = 0; //1
            return BigInteger.ModPow(input, Exponent, Modulus).ToByteArray().Skip(signature_padding).Reverse().SkipWhile(v => v != 0).Skip(1 + signature_padding).ToArray();
        }

    }


    internal class LicenseDetails : ILicenseDetails
    {
        public IEnumerable<string> GetFeatures()
        {
            return features;
        }
        public DateTime? Issued { get; private set; }
        public DateTime? Expires { get; private set; }
        public DateTime? NoReleasesAfter { get; private set; }
        public IReadOnlyDictionary<string, string> GetPairs()
        {
            return pairs;
        }

        private Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<string> features;

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
                else if (String.Equals(key, "no releases after", StringComparison.InvariantCultureIgnoreCase))
                {
                    NoReleasesAfter = DateTime.Parse(value);
                }
                else if (String.Equals(key, "features", StringComparison.InvariantCultureIgnoreCase))
                {
                    features = value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
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


    internal class LicenseValidator
    {


        public bool Validate(ILicenseBlob b, RSADecryptPublic p, StringBuilder log)
        {
            var signature = b.GetSignature();
            var hash = new SHA512Managed().ComputeHash(b.GetDataUTF8());
            var decrypted_bytes = p.DecryptPublic(signature);
            var valid = hash.SequenceEqual(decrypted_bytes);

            if (log != null)
            {
                log.AppendLine("Using public modulus: " + p.Modulus.ToString());
                log.AppendLine("Using public exponent: " + p.Exponent.ToString());
                log.AppendLine("Encrypted bytes: " + BitConverter.ToString(signature).ToLower().Replace("-", ""));
                log.AppendLine("Decrypted sha512: " + BitConverter.ToString(decrypted_bytes).ToLower().Replace("-", ""));
                log.AppendLine("Expected sha512: " + BitConverter.ToString(hash).ToLower().Replace("-", ""));
            }
            return valid;
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


            var decryptor = new RSADecryptPublic(mod, exp);

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
