using System;
using System.Collections.Specialized;
using Xunit;
using ImageResizer.Plugins.Encrypted;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Security.Tests {
    public class EncryptedPluginTests {

        [Fact]
        public void Constructor_SetsDefaultVirtualPrefix() {
            var plugin = new EncryptedPlugin();
            Assert.Contains("enc", plugin.VirtualPrefix);
        }

        [Fact]
        public void Constructor_WithPrefixAndKey() {
            var plugin = new EncryptedPlugin("~/secure/", "my-secret-key-at-least-32-chars!");
            Assert.Contains("secure", plugin.VirtualPrefix);
        }

        [Fact]
        public void VirtualPrefix_AutoAddsTrailingSlash() {
            var plugin = new EncryptedPlugin("~/myprefix", "my-secret-key-at-least-32-chars!");
            Assert.True(plugin.VirtualPrefix.EndsWith("/"), "VirtualPrefix should end with /");
        }

        [Fact]
        public void EncryptPathAndQuery_ProducesValidPath() {
            var plugin = new EncryptedPlugin("~/enc/", "my-secret-key-at-least-32-chars!");
            string encrypted = plugin.EncryptPathAndQuery("~/images/test.jpg", new NameValueCollection { { "width", "100" } });
            Assert.StartsWith(plugin.VirtualPrefix.TrimEnd('/'), encrypted);
            Assert.EndsWith(".ashx", encrypted);
        }

        [Fact]
        public void EncryptPathAndQuery_StringOverload() {
            var plugin = new EncryptedPlugin("~/enc/", "my-secret-key-at-least-32-chars!");
            string encrypted = plugin.EncryptPathAndQuery("~/images/test.jpg?width=100");
            Assert.StartsWith(plugin.VirtualPrefix.TrimEnd('/'), encrypted);
            Assert.EndsWith(".ashx", encrypted);
        }

        [Fact]
        public void EncryptPathAndQuery_DifferentInputs_DifferentOutputs() {
            var plugin = new EncryptedPlugin("~/enc/", "my-secret-key-at-least-32-chars!");
            string enc1 = plugin.EncryptPathAndQuery("~/images/test1.jpg?width=100");
            string enc2 = plugin.EncryptPathAndQuery("~/images/test2.jpg?width=200");
            Assert.NotEqual(enc1, enc2);
        }

        [Fact]
        public void EncryptPathAndQuery_SameInput_DifferentOutputs_DueToIV() {
            var plugin = new EncryptedPlugin("~/enc/", "my-secret-key-at-least-32-chars!");
            string enc1 = plugin.EncryptPathAndQuery("~/images/test.jpg?width=100");
            string enc2 = plugin.EncryptPathAndQuery("~/images/test.jpg?width=100");
            // Due to random IV, same input should produce different encrypted paths
            Assert.NotEqual(enc1, enc2);
        }

        [Fact]
        public void EncryptPathAndQuery_NonAppRelative_Throws() {
            var plugin = new EncryptedPlugin("~/enc/", "my-secret-key-at-least-32-chars!");
            Assert.Throws<ArgumentException>(() =>
                plugin.EncryptPathAndQuery("images/test.jpg?width=100"));
        }

        [Fact]
        public void Constructor_NvcArgs_SetsPrefix() {
            var args = new NameValueCollection();
            args["prefix"] = "~/mypath/";
            args["key"] = "a-sufficiently-long-encryption-key-for-testing!";
            var plugin = new EncryptedPlugin(args);
            Assert.Contains("mypath", plugin.VirtualPrefix);
        }
    }
}
