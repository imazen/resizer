using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ImageResizer.Plugins.Licensing;
using static System.String;

namespace ImageResizer.Plugins.LicenseVerifier
{
    class LicenseBlob : ILicenseBlob
    {
        public string Original { get; private set; }
        public byte[] Signature { get; private set; }
        public byte[] Data { get; private set; }
        public ILicenseDetails Fields { get; private set; }

        public static LicenseBlob Deserialize(string license)
        {
            var parts = license.Split(':').Select(s => s.Trim()).ToList();
            if (parts.Count < 2) {
                throw new ArgumentException(
                    "Not enough ':' delimited segments in license key; failed to deserialize: \"" + license + "\"",
                    nameof(license));
            }

            var dataBytes = Convert.FromBase64String(parts[parts.Count - 2]);
            var b = new LicenseBlob {
                Original = license,
                Signature = Convert.FromBase64String(parts[parts.Count - 1]),
                Data = dataBytes,
                Fields = new LicenseDetails(System.Text.Encoding.UTF8.GetString(dataBytes))
            };
            // b.Info = System.Text.Encoding.UTF8.GetString(b.Data);
            // b.Comments = parts.Take(parts.Count - 2);
            return b;
        }

        public override string ToString() => Original;
    }

    class LicenseDetails : ILicenseDetails
    {
        public LicenseDetails(string plaintext)
        {
            var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in plaintext.Split('\n')) {
                var colon = line.IndexOf(':');
                if (colon < 1) {
                    continue; //Skip lines without a colon
                }
                var key = line.Substring(0, colon).Trim();
                var value = line.Substring(colon + 1).Trim();

                if (string.Equals(key, "issued", StringComparison.InvariantCultureIgnoreCase)) {
                    Issued = DateTimeOffset.Parse(value);
                } else if (string.Equals(key, "expires", StringComparison.InvariantCultureIgnoreCase)) {
                    Expires = DateTimeOffset.Parse(value);
                } else if (string.Equals(key, "subscriptionexpirationdate",
                    StringComparison.InvariantCultureIgnoreCase)) {
                    SubscriptionExpirationDate = DateTimeOffset.Parse(value);
                }
                pairs.Add(key, value);
            }
            Pairs = pairs;


            Id = (Get("Id") ?? Get("Domain"))?.Trim().ToLowerInvariant();
            if (IsNullOrEmpty(Id)) {
                throw new ArgumentException("No 'Id' or 'Domain' fields found! At least one of the two is required");
            }
            if (IsNullOrEmpty(this.GetSecret()) && this.IsRemotePlaceholder()) {
                throw new ArgumentException("Licenses of 'Kind: id' must contain 'Secret: somesecretkey'");
            }
        }

        public string Id { get; }
        public DateTimeOffset? Issued { get; }
        public DateTimeOffset? Expires { get; }
        public DateTimeOffset? SubscriptionExpirationDate { get; }
        public IReadOnlyDictionary<string, string> Pairs { get; }

        public string Get(string key)
        {
            string s;
            return Pairs.TryGetValue(key, out s) ? s : null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var pair in Pairs) {
                sb.AppendFormat("{0}: {1}\n", pair.Key, pair.Value);
            }
            return sb.ToString();
        }
    }

    static class StringIntParseExtensions
    {
        public static int? TryParseInt(this string s)
        {
            int temp;
            return IsNullOrWhiteSpace(s) ? null : (int.TryParse(s, out temp) ? (int?) temp : null);
        }
    }


    static class LicenseDetailsExtensions
    {
        public static bool IsRemotePlaceholder(this ILicenseDetails details)
            => "id".Equals(details?.Get("Kind"), StringComparison.OrdinalIgnoreCase);

        public static bool IsRevoked(this ILicenseDetails details)
            => "false".Equals(details?.Get("Valid"), StringComparison.OrdinalIgnoreCase);

        public static bool IsPublic(this ILicenseDetails details)
            => "true".Equals(details?.Get("IsPublic"), StringComparison.OrdinalIgnoreCase);

        public static bool MustBeFetched(this ILicenseDetails details)
            => "true".Equals(details?.Get("MustBeFetched"), StringComparison.OrdinalIgnoreCase);

        public static int? NetworkGraceMinutes(this ILicenseDetails details)
            => details?.Get("NetworkGraceMinutes").TryParseInt();

        public static int? CheckLicenseIntervalMinutes(this ILicenseDetails details)
            => details?.Get("CheckLicenseIntervalMinutes").TryParseInt();


        public static string GetSecret(this ILicenseDetails details)
            => details?.Get("Secret");

        public static string GetMessage(this ILicenseDetails details)
            => details?.Get("Message");

        public static string GetRestrictions(this ILicenseDetails details)
            => details?.Get("Restrictions");

        public static string GetExpiryMessage(this ILicenseDetails details)
            => details?.Get("ExpiryMessage");


        /// <summary>
        ///     Enumerates the feature code list. No case changes are performed
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetFeatures(this ILicenseDetails details)
            => details?.Get("Features")
                      ?.Split(new[] {' ', '\t', ','}, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .ToList() ?? Enumerable.Empty<string>();

        /// <summary>
        ///     Enumerates any/all values from "Domain" and "Domains" field, trimming and lowercasing all values.
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllDomains(this ILicenseDetails details)
        {
            var domains = details.Get("Domains")?.Split(' ', '\t', ',');
            var domain = details.Get("Domain");
            if (domain == null && domains == null) {
                return Enumerable.Empty<string>();
            }

            var list = new List<string>(1);
            if (domains != null) {
                list.AddRange(domains);
            }
            if (domain != null) {
                list.Add(domain);
            }
            return list.Where(s => !IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToLowerInvariant());
        }

        /// <summary>
        ///     Returns all valid license servers from the LicenseServers field
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetValidLicenseServers(this ILicenseDetails details)
        {
            return details.Get("LicenseServers")
                          ?
                          .Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries)
                          .Where(s =>
                          {
                              Uri t;
                              return Uri.TryCreate(s, UriKind.Absolute, out t) && t.Scheme == "https";
                          })
                          .Select(s => s.Trim()) ?? Enumerable.Empty<string>();
        }

        public static bool DataMatches(this ILicenseDetails me, ILicenseDetails other)
        {
            return me != null && other != null && me.Id == other.Id && me.Issued == other.Issued &&
                   me.Expires == other.Expires
                   && me.SubscriptionExpirationDate == other.SubscriptionExpirationDate &&
                   me.Pairs.All(pair => other.Get(pair.Key) == pair.Value);
        }
    }

    static class LicenseBlobExtensions
    {
        public static bool Revalidate(this ILicenseBlob b, IEnumerable<RSADecryptPublic> trustedKeys)
        {
            var ourCopy = LicenseBlob.Deserialize(b.Original);
            return ourCopy.VerifySignature(trustedKeys, null) && ourCopy.Fields.DataMatches(b.Fields);
        }

        public static bool VerifySignature(this ILicenseBlob b, IEnumerable<RSADecryptPublic> trustedKeys,
                                           StringBuilder log)
        {
            return trustedKeys.Any(p =>
            {
                var signature = b.Signature;
                var hash = new SHA512Managed().ComputeHash(b.Data);
                var decryptedBytes = p.DecryptPublic(signature);
                var valid = hash.SequenceEqual(decryptedBytes);

                log?.AppendLine("Using public exponent " + p.Exponent.ToString() + " and modulus " +
                                p.Modulus.ToString());
                log?.AppendLine("Encrypted bytes: " + BitConverter.ToString(signature).ToLower().Replace("-", ""));
                log?.AppendLine("Decrypted sha512: " +
                                BitConverter.ToString(decryptedBytes).ToLower().Replace("-", ""));
                log?.AppendLine("Expected sha512: " + BitConverter.ToString(hash).ToLower().Replace("-", ""));
                log?.AppendLine(valid ? "Success!" : "Not a match.");

                return valid;
            });
        }

        /// <summary>
        ///     Redacts the value of the 'Secret' field
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ToRedactedString(this ILicenseBlob b)
        {
            return Join("\n", b.Fields.Pairs.Select(
                pair => "secret".Equals(pair.Key, StringComparison.OrdinalIgnoreCase)
                    ? $"{pair.Key}: ****redacted****"
                    : $"{pair.Key}: {pair.Value}"));
        }
    }

    // ReSharper disable once InconsistentNaming
    class RSADecryptPublic
    {
        public BigInteger Modulus { get; }
        public BigInteger Exponent { get; }
        public int BlockSize { get; }

        public RSADecryptPublic(BigInteger modulus, BigInteger exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
            BlockSize = Modulus.ToByteArray().Length;
        }

        public byte[] DecryptPublic(byte[] inputBytes)
        {
            if (inputBytes.Length > BlockSize) {
                throw new ArgumentOutOfRangeException(nameof(inputBytes), "input too long for RSA cipher block size.");
            }
            var input = new BigInteger(inputBytes
                .Reverse()
                .Concat(Enumerable.Repeat<byte>(0, 1))
                .ToArray()); //Add a zero to prevent interpretation as twos-complement

            if (input.CompareTo(Modulus) >= 0) {
                throw new ArgumentOutOfRangeException(nameof(inputBytes), "input too large for RSA modulus.");
            }
            const int signaturePadding = 0; //1
            return BigInteger.ModPow(input, Exponent, Modulus)
                             .ToByteArray()
                             .Skip(signaturePadding)
                             .Reverse()
                             .SkipWhile(v => v != 0)
                             .Skip(1 + signaturePadding)
                             .ToArray();
        }
    }

    class ImazenPublicKeys
    {
        public static IReadOnlyCollection<RSADecryptPublic> Test { get; }
        public static IReadOnlyCollection<RSADecryptPublic> Production { get; }
        public static IReadOnlyCollection<RSADecryptPublic> All { get; }

        static ImazenPublicKeys()
        {
            Test = new[] {
                new RSADecryptPublic(
                    BigInteger.Parse(
                        "21403964489040138713896545869406851734432500305180577929806228393671667423170541918856531956008546071841016201645150244452266439995041173092354230946610429300967887006960186647111152810965360763586210200652502467947786453111507369142658284220331513416234497960844309808252643534631142917589553418044306073242485021092396181183125381004682521853943025560860753079004948017667604884278401445729443478586697229583656851019218046599746243419376456426788044497274378001221965538712352348475726349124652450874653832672820100829574087311416068166524423905971193163418806721436095962165082262760557869093554827824418663362349"),
                    BigInteger.Parse("65537"))
            };
            Production = new[] {
                new RSADecryptPublic(
                    BigInteger.Parse(
                        "23949488589991837273662465276682907968730706102086698017736172318753209677546629836371834786541857453052840819693021342491826827766290334135101781149845778026274346770115575977554682930349121443920608458091578262535319494351868006252977941758848154879863365934717437651379551758086088085154566157115250553458305198857498335213985131201841998493838963767334138323078497945594454883498534678422546267572587992510807296283688571798124078989780633040004809178041347751023931122344529856055566400640640925760832450260419468881471181281199910469396775343083815780600723550633987799763107821157001135810564362648091574582493"),
                    BigInteger.Parse("65537")),
                new RSADecryptPublic(
                    BigInteger.Parse(
                        "20966000569757071862106887100142448229133877611190126160168597284259733824510172534126967070490592659952430888203435031779696121874348777439846786968121542858840906429510085119585674950522992116110440180288728612219347325636018396716507682924594303420147925518492731883007123328081986113438120311956235689236820190735716844178839961449198918585485277306636638238163410140728079481083558191670535479781738412622557832581113291858559860935145319768483825412681366230852014952837750160226558508220374106696447994610354318517561059830141995002511253671974534953764078640650030953288533566233172651498868658899945417935381"),
                    BigInteger.Parse("65537"))
            };
            All = Production.Concat(Test).ToArray();
        }
    }

    class SignatureTestApp
    {
        static bool Test_Generic(StringBuilder log)
        {
            log?.AppendLine("Generic license decryption self-test");
            var exp = BigInteger.Parse("65537");
            var mod = BigInteger.Parse(
                "28178177427582259905122756905913963624440517746414712044433894631438407111916149031583287058323879921298234454158166031934230083094710974550125942791690254427377300877691173542319534371793100994953897137837772694304619234054383162641475011138179669415510521009673718000682851222831185756777382795378538121010194881849505437499638792289283538921706236004391184253166867653735050981736002298838523242717690667046044130539971131293603078008447972889271580670305162199959939004819206804246872436611558871928921860176200657026263241409488257640191893499783065332541392967986495144643652353104461436623253327708136399114561");
            const string licenseStr =
                "localhost:RG9tYWluOiBsb2NhbGhvc3QKT3duZXI6IEV2ZXJ5b25lCklzc3VlZDogMjAxNS0wMy0yOFQwOTozNjo1OVoKRmVhdHVyZXM6IFI0RWxpdGUgUjRDcmVhdGl2ZSBSNFBlcmZvcm1hbmNlCg==:h6D+kIXbF3qmvmW2gDpb+b4gdxBjnrkZLvSzXmEnqKAywNJNpTdFekpTOB4SwU14WbTeVyWwvFngHax7WuHBV+0WkQ5lDqKFaRW32vj8CJQeG8Wvnyj9PaNGaS/FpKhNjZbDEmh3qqirBp2NR0bpN4QbhP9NMy7+rOMo0nynAruwWvJKCnuf7mWWdb9a5uTZO9OUcSeS/tY8QaNeIhaCnhPe0Yx9qvOXe5nMnl10CR9ur+EtS54d1qzBGHqN/3oFhiB+xlqNELwz23qR4c8HxbTEyNarkG4CZx8CbbgJfHmPxAYGJTTBTPJ+cdah8MJR16Ta36cRZ2Buy8XYo/nf1g==";
            return Validate(licenseStr, mod, exp, log);
        }

        static bool Validate(string licenseStr, BigInteger mod, BigInteger exp, StringBuilder log)
        {
            var blob = LicenseBlob.Deserialize(licenseStr);
            log?.AppendLine("---------------------------------------------");
            log?.AppendLine("Parsed info: " + blob.Fields);
            log?.AppendLine("Plaintext hash: " + BitConverter
                                .ToString(new SHA512Managed().ComputeHash(blob.Data))
                                .ToLower()
                                .Replace("-", ""));
            return blob.VerifySignature(new[] {new RSADecryptPublic(mod, exp)}, log);
        }

        static string ReadStdin()
        {
            using (var s = Console.OpenStandardInput())
            using (var sr = new StreamReader(s, System.Text.Encoding.UTF8)) {
                return sr.ReadToEnd();
            }
        }

        // ReSharper disable once UnusedMember.Local
        static int Main(IReadOnlyList<string> args)
        {
            var mod = BigInteger.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[0])));
            var exp = BigInteger.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));

            var licenseStr = ReadStdin();
            var debug = args.ElementAtOrDefault(2) == "-d";
            int exitCode;

            if (debug) {
                var log = new StringBuilder();
                exitCode = Test_Generic(log) ? 0 : 4;

                if (!Validate(licenseStr, mod, exp, log)) {
                    exitCode = 2 ^ exitCode;
                }
                if (exitCode != 0) {
                    Console.WriteLine(log.ToString());
                }
            } else {
                exitCode = Validate(licenseStr, mod, exp, null) ? 0 : 1;
            }
            return exitCode;
        }
    }
}
