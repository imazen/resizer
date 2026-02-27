using System;
using System.Collections.Specialized;
using Xunit;
using ImageResizer.Plugins.RemoteReader;
using ImageResizer.Util;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.RemoteReader.Tests {
    public class RemoteReaderTests {

        private RemoteReaderPlugin CreateInstalledPlugin(string signingKey = "test-signing-key-that-is-long-enough") {
            var c = new Config();
            c.setConfigXmlText(
                "<resizer><remotereader signingKey=\"" + signingKey + "\" /></resizer>");
            var plugin = new RemoteReaderPlugin();
            plugin.Install(c);
            return plugin;
        }

        // --- SignDataWithKey ---

        [Fact]
        public void SignDataWithKey_SameInput_SameOutput() {
            var plugin = new RemoteReaderPlugin();
            string sig1 = plugin.SignDataWithKey("test-data", "secret-key");
            string sig2 = plugin.SignDataWithKey("test-data", "secret-key");
            Assert.Equal(sig1, sig2);
        }

        [Fact]
        public void SignDataWithKey_DifferentData_DifferentOutput() {
            var plugin = new RemoteReaderPlugin();
            string sig1 = plugin.SignDataWithKey("data1", "secret-key");
            string sig2 = plugin.SignDataWithKey("data2", "secret-key");
            Assert.NotEqual(sig1, sig2);
        }

        [Fact]
        public void SignDataWithKey_DifferentKeys_DifferentOutput() {
            var plugin = new RemoteReaderPlugin();
            string sig1 = plugin.SignDataWithKey("test-data", "key1");
            string sig2 = plugin.SignDataWithKey("test-data", "key2");
            Assert.NotEqual(sig1, sig2);
        }

        [Fact]
        public void SignDataWithKey_ProducesUrlSafeString() {
            var plugin = new RemoteReaderPlugin();
            string sig = plugin.SignDataWithKey("test-data", "secret-key");
            Assert.DoesNotContain("=", sig);
            Assert.DoesNotContain("+", sig);
            Assert.DoesNotContain("/", sig);
        }

        // --- CreateSignedUrl ---

        [Fact]
        public void CreateSignedUrl_ContainsBase64Url() {
            var plugin = CreateInstalledPlugin();
            string url = plugin.CreateSignedUrl("http://example.com/image.jpg", "width=100");
            Assert.Contains("urlb64=", url);
        }

        [Fact]
        public void CreateSignedUrl_ContainsHmac() {
            var plugin = CreateInstalledPlugin();
            string url = plugin.CreateSignedUrl("http://example.com/image.jpg", "width=100");
            Assert.Contains("hmac=", url);
        }

        [Fact]
        public void CreateSignedUrl_ContainsRemotePrefix() {
            var plugin = CreateInstalledPlugin();
            string url = plugin.CreateSignedUrl("http://example.com/image.jpg", "width=100");
            Assert.Contains("/remote", url);
        }

        [Fact]
        public void CreateSignedUrl_PreservesQueryParams() {
            var plugin = CreateInstalledPlugin();
            var settings = new NameValueCollection();
            settings["width"] = "100";
            settings["height"] = "200";
            string url = plugin.CreateSignedUrl("http://example.com/image.jpg", settings);
            Assert.Contains("width=100", url);
            Assert.Contains("height=200", url);
        }

        // --- CreateSignedUrlWithKey ---

        [Fact]
        public void CreateSignedUrlWithKey_ProducesValidUrl() {
            var plugin = new RemoteReaderPlugin();
            string url = plugin.CreateSignedUrlWithKey(
                "http://example.com/image.jpg",
                "width=100",
                "my-custom-key");
            Assert.Contains("urlb64=", url);
            Assert.Contains("hmac=", url);
        }

        [Fact]
        public void CreateSignedUrlWithKey_DifferentKeys_DifferentHmacs() {
            var plugin = new RemoteReaderPlugin();
            string url1 = plugin.CreateSignedUrlWithKey("http://example.com/image.jpg", "width=100", "key1");
            string url2 = plugin.CreateSignedUrlWithKey("http://example.com/image.jpg", "width=100", "key2");
            Assert.NotEqual(url1, url2);
        }

        // --- IsRemotePath ---

        [Fact]
        public void IsRemotePath_ValidRemotePath_ReturnsTrue() {
            var plugin = new RemoteReaderPlugin();
            Assert.True(plugin.IsRemotePath("/remote.jpg"));
        }

        [Fact]
        public void IsRemotePath_ValidRemotePathWithSlash_ReturnsTrue() {
            var plugin = new RemoteReaderPlugin();
            Assert.True(plugin.IsRemotePath("/remote/something"));
        }

        [Fact]
        public void IsRemotePath_NonRemotePath_ReturnsFalse() {
            var plugin = new RemoteReaderPlugin();
            Assert.False(plugin.IsRemotePath("/images/test.jpg"));
        }

        [Fact]
        public void IsRemotePath_ExactPrefix_ReturnsFalse() {
            var plugin = new RemoteReaderPlugin();
            Assert.False(plugin.IsRemotePath("/remote"));
        }

        // --- ParseRequest ---

        [Fact]
        public void ParseRequest_WithSignedUrl_ReturnsSignedRequest() {
            var plugin = CreateInstalledPlugin();
            string remoteUrl = "http://example.com/image.jpg";
            var settings = new NameValueCollection();
            settings["width"] = "100";
            string signedUrl = plugin.CreateSignedUrl(remoteUrl, settings);

            // Extract querystring from signed URL
            int qsStart = signedUrl.IndexOf('?');
            string path = signedUrl.Substring(0, qsStart);
            var query = PathUtils.ParseQueryString(signedUrl);

            var args = plugin.ParseRequest(path, query);
            Assert.NotNull(args);
            Assert.True(args.SignedRequest);
            Assert.Equal(remoteUrl, args.RemoteUrl);
        }

        [Fact]
        public void ParseRequest_NonRemotePath_ReturnsNull() {
            var plugin = CreateInstalledPlugin();
            var result = plugin.ParseRequest("/images/test.jpg", new NameValueCollection());
            Assert.Null(result);
        }

        [Fact]
        public void ParseRequest_InvalidHmac_Throws() {
            var plugin = CreateInstalledPlugin();
            var query = new NameValueCollection();
            query[RemoteReaderPlugin.Base64UrlKey] = PathUtils.ToBase64U("http://example.com/image.jpg");
            query[RemoteReaderPlugin.HmacKey] = "invalid-hmac-value";

            Assert.Throws<ImageProcessingException>(() =>
                plugin.ParseRequest("/remote.jpg", query));
        }

        // --- FileExists ---

        [Fact]
        public void FileExists_RemotePath_ReturnsTrue() {
            var plugin = new RemoteReaderPlugin();
            Assert.True(plugin.FileExists("/remote.jpg", new NameValueCollection()));
        }

        [Fact]
        public void FileExists_NonRemotePath_ReturnsFalse() {
            var plugin = new RemoteReaderPlugin();
            Assert.False(plugin.FileExists("/images/test.jpg", new NameValueCollection()));
        }

        // --- Static keys ---

        [Fact]
        public void Base64UrlKey_IsUrlb64() {
            Assert.Equal("urlb64", RemoteReaderPlugin.Base64UrlKey);
        }

        [Fact]
        public void HmacKey_IsHmac() {
            Assert.Equal("hmac", RemoteReaderPlugin.HmacKey);
        }

        // --- Properties ---

        [Fact]
        public void AllowedRedirects_DefaultIs5() {
            var plugin = new RemoteReaderPlugin();
            Assert.Equal(5, plugin.AllowedRedirects);
        }

        [Fact]
        public void SkipUriValidation_DefaultIsFalse() {
            var plugin = new RemoteReaderPlugin();
            Assert.False(plugin.SkipUriValidation);
        }

        [Fact]
        public void UserAgent_DefaultIsImageResizer() {
            var plugin = new RemoteReaderPlugin();
            Assert.Equal("ImageResizer", plugin.UserAgent);
        }
    }
}
