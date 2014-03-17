using Cryptography = ImageResizer.Plugins.LicenseVerifier.Cryptography;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier {
    public class DomainLicense {
        private string keyPairXml;

        public string Domain { get; private set; }
        public string OwnerName { get; private set; }
        public DateTime Issued { get; private set; }
        public DateTime Expires { get; private set; }
        public IList<Guid> Features { get; private set; }

        public DomainLicense(string domain, string ownerName, DateTime issued, DateTime expires, IList<Guid> features) {
            Domain = domain;
            OwnerName = ownerName;
            Issued = issued;
            Expires = expires;
            Features = features;
        }

        public DomainLicense(byte[] encryptedLicense, string keyPairXml) {
            this.keyPairXml = keyPairXml;
            string decrypted = Decrypt(encryptedLicense);
            string[] lines = decrypted.Split('\n');
            foreach (string l in lines) {
                int colon = l.IndexOf(':');
                if (colon < 1) continue;
                string key = l.Substring(0, colon).Trim().ToLowerInvariant();
                string value = l.Substring(colon + 1).Trim();

                switch (key) {
                    case "domain": Domain = value; break;
                    case "ownername": OwnerName = value; break;
                    case "issued": Issued = DateTime.Parse(value); break;
                    case "expires": Expires = DateTime.Parse(value); break;
                    case "features":
                        var ids = new List<Guid>();
                        string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string p in parts) {
                            ids.Add(new Guid(p));
                        }
                        Features = ids;
                        break;
                }
            }
        }

        public string SerializeUnencrypted() {
            return "Domain: " + Domain.Replace('\n', ' ') + "\n" +
                   "OwnerName: " + OwnerName.Replace('\n', ' ') + "\n" +
                   "Issued: " + Issued.ToString() + "\n" +
                   "Expires: " + Expires.ToString() + "\n" +
                   "Features: " + Join(Features) + "\n";
        }

        public string GetShortDescription() {
            var sb = new StringBuilder(OwnerName + " - " + Domain + " - " + Issued.ToString() + " - " + Expires.ToString() + " - ");
            foreach (var id in Features)
                sb.Append(id + " ");
            return sb.ToString().TrimEnd();
        }

        public byte[] SerializeAndEncrypt(string keyPairXml) {
            // Encrypt data using Crypto and generated key
            // Use RSA to encrypt key
            byte[] encrypted = Cryptography.Crypto.Encrypt(SerializeUnencrypted(), "pass-phrase", "salt-in-the-wound", 2, "@1B2c3D4e5F6g7H8", 256);

            //using (var r = new RSACryptoServiceProvider(2048)) {
            //    r.FromXmlString(keyPairXml);
            //    byte[] encryptedKey = r.Encrypt(key, false);
            //    System.IO.File.WriteAllText("encrypted.key", Convert.ToBase64String(encryptedKey));
            //}

            return encrypted;
        }

        private string Join(ICollection<Guid> items) {
            var sb = new StringBuilder();
            foreach (Guid g in items)
                sb.Append(g.ToString() + ",");
            return sb.ToString().TrimEnd(',');
        }

        private string Decrypt(byte[] encrypted) {
            //using (var r = new RSACryptoServiceProvider(2048)) {
            //    r.FromXmlString(keyPairXml);
            //    byte[] decryptedKey = r.Decrypt(encryptedKey, false);
            var encryptedString = Convert.ToBase64String(encrypted);
            return Cryptography.Crypto.Decrypt(encryptedString, "pass-phrase", "salt-in-the-wound", 2, "@1B2c3D4e5F6g7H8", 256);
            //}
        }
    }
}
