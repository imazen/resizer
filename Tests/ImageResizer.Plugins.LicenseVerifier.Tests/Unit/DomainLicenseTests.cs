using Should;
using System;
using System.Collections.Generic;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Unit {
    public class DomainLicenseTests {

        private Guid featureOne;
        private Guid featureTwo;
        private Guid featureThree;
        private string domain;
        private string ownerName;
        private DateTime issued;
        private DateTime expires;
        private IList<Guid> features;
        private DomainLicense domainLicense;

        public DomainLicenseTests() {
            
            domain = "domain.com";
            ownerName = "John Doe";
            issued = DateTime.UtcNow;
            expires = DateTime.UtcNow.AddHours(24);

            featureOne = Guid.NewGuid();
            featureTwo = Guid.NewGuid();
            featureThree = Guid.NewGuid();
            features = new List<Guid> {
                featureOne,
                featureTwo,
                featureThree
            };

            domainLicense = new DomainLicense(
                domain,
                ownerName,
                issued,
                expires,
                features
            );
        }
        
        public void Should_be_able_to_serialize_to_proper_form() {
            var expected = "Domain: " + domain + "\n";
            expected += "OwnerName: " + ownerName + "\n";
            expected += "Issued: " + issued + "\n";
            expected += "Expires: " + expires + "\n";
            expected += "Features: " + featureOne + "," + featureTwo + "," + featureThree + "\n";
            var serializedDomainLicense = domainLicense.SerializeUnencrypted();
            serializedDomainLicense.ShouldEqual(expected);
        }

        public void Should_be_able_to_generate_short_description() {
            var expected = ownerName + " - " + domain + " - " + issued.ToString() + " - " + expires.ToString() + " - ";
            foreach (var id in features)
                expected += id + " ";

            string shortDescription = domainLicense.GetShortDescription();

            shortDescription.ShouldEqual(expected.TrimEnd());
        }

        public void Should_be_able_to_encrypt_and_decrypt() {

            var keyPair = Helpers.GenerateKeyPairXml();

            byte[] encrypted = domainLicense.SerializeAndEncrypt(keyPair);

            var encryptedDomainLicense = new DomainLicense(encrypted, keyPair);
            var serializedDomainLicense = encryptedDomainLicense.SerializeUnencrypted();

            serializedDomainLicense.ShouldEqual(domainLicense.SerializeUnencrypted());
        }
    }
}
