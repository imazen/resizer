using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;

namespace ImageResizer.Plugins.LicenseVerifier.Tests {
    public static class Helpers {
        internal delegate bool WaitCondition();
        internal static void WaitForCondition(WaitCondition condition) {
            const int totalTimeout = 60000;
            const int step = 1000;
            int currentTimeout = 0;
            while (condition() == false && currentTimeout < totalTimeout) {
                Thread.Sleep(step);
                currentTimeout += step;
            }

            if (currentTimeout >= totalTimeout)
                throw new Exception("Timeout reached while waiting on a thread.");
        }

        /// <summary>
        /// Generates a key 2048-bit keypair and returns the xml fragment containing it
        /// </summary>
        /// <returns></returns>
        internal static string GenerateKeyPairXml() {
            using (var r = new RSACryptoServiceProvider(2048))
                return r.ToXmlString(true);
        }

        /// <summary>
        /// Strips the private information from the given key pair
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        internal static string StripPrivateKey(string pair) {
            using (var r = new RSACryptoServiceProvider(2048)) {
                r.FromXmlString(pair);
                return r.ToXmlString(false);
            }
        }

        internal static string GenerateValidXmlResponseFromKeyHub(string keyPair, IList<DomainLicense> domainLicenses) {
            var responseXml = new XmlDocument();
            var root = responseXml.CreateElement("licenses");
            responseXml.AppendChild(root);

            foreach (var domainLicense in domainLicenses) {
                var encryptedLicense = Convert.ToBase64String(domainLicense.SerializeAndEncrypt(keyPair));
                var licenseElement = responseXml.CreateElement("license");
                licenseElement.AppendChild(responseXml.CreateTextNode(encryptedLicense));
                root.AppendChild(licenseElement);
            }

            return responseXml.OuterXml;
        }

        internal static DomainLicense GenerateDomainLicense(IList<Guid> features) {
            return new DomainLicense(
                "domain.com",
                "John Doe",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(24),
                features);
        }
    }
}
