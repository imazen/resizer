using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.RemoteReader;
using Xunit;

namespace ImageResizer.AllPlugins.Tests
{
    /// <summary>
    /// Test the functionality of the <see cref="RemoteReaderPlugin"/> class.
    /// </summary>
    /// <remarks>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="RemoteReaderPlugin"/>. Also The methods 
    /// implementations of <see cref="IVirtualFile"/>.
    /// </remarks>
    public class RemoteReaderTest 
    {
        /// <summary>
        /// A GUID that can be used to represents a file that does not exist.
        /// </summary>
        private static Guid dummyDatabaseRecordId = Guid.NewGuid();

        private static string pathPrefix = "/remote/farm7.static.flickr.com/6021/";

        /// <summary>
        /// Instantiate a new  <see cref="RemoteReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void DefaultConstructor()
        {
            // Arrange

            // Act
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Assert
            Assert.NotNull(target);
            Assert.IsType<RemoteReaderPlugin>(target);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithNullQueryString()
        {
            // Arrange
            bool expected = true;
            string virtualPath = Path.Combine(pathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = RemoteReaderPlugin.Current;

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithNullVirtualPath()
        {
            // Arrange
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = Assert.Throws<NullReferenceException>(() => target.FileExists(null, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<NullReferenceException>(actual);
        }

        /// <summary>
        /// Call the FileExists method with an empty string for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithEmptyVirtualPath()
        {
            // Arrange
            bool expected = false;
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = target.FileExists(string.Empty, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does not include
        /// the PathPrefix.
        /// </summary>
        [Fact]
        public void FileExistsWithoutVirtualPath()
        {
            // Arrange
            bool expected = false;
            string virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        [Fact]
        public void GetFileNotSigned() {
            // Arrange
            string virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var c = Config.Current;
            ((IPlugin)target).Install(c);
            var settings = this.Settings;
            settings.Remove("hmac");
            settings.Remove("urlb64");

            // Act
            var actual = Assert.Throws<ImageProcessingException>(() => target.GetFile(virtualPath, settings));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ImageProcessingException>(actual);
        }

        [Fact]
        public void GetFileNotSignedWhiteListed() {
            // Arrange
            string virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            //IVirtualImageProvider target = new RemoteReaderPlugin();
            var c = new Config();
            RemoteReaderPlugin target = new RemoteReaderPlugin();
            target.Install(c);
            target.AllowRemoteRequest += delegate(object sender, RemoteRequestEventArgs args) {
                args.DenyRequest = false;
            }; 
            var settings = this.Settings;
            settings.Remove("hmac");
            settings.Remove("urlb64");

            // Act
            var actual = ((IVirtualImageProvider)target).GetFile(virtualPath, settings);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
        }

        [Fact]
        public void GetFileSigned()
        {
            // Arrange
            string virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var rs = new ResizerSection("<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((IPlugin)target).Install(c);
            var settings = this.Settings;

            // Act
            var actual = target.GetFile(virtualPath, settings);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
        }

        [Fact]
        public void GetFileWithoutVirtualPathPrefix()
        {
            // Arrange
            string virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.GetFile(virtualPath, new NameValueCollection()));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        [Fact]
        public void GetFileWithNullVirtualPath()
        {
            // Arrange
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = Assert.Throws<NullReferenceException>(() => target.GetFile(null, new NameValueCollection()));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<NullReferenceException>(actual);
        }

        [Fact]
        public void GetFileWithEmptyVirtualPath()
        {
            // Arrange
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.GetFile(string.Empty, new NameValueCollection()));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Requires a file to be present at http://farm7.static.flickr.com/6021/5959854178_1c2ec6bd77_b.jpg
        /// </remarks>
        [Fact]
        public void Open() {
            // Arrange
            string virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var rs = new ResizerSection("<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((RemoteReaderPlugin)reader).Install(c);
            var settings = this.Settings;
            settings["hmac"] = "k_RU-UFkOaA";
            settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvNTk1OTg1NDE3OF8xYzJlYzZiZDc3X2IuanBn";
            var target = reader.GetFile(virtualPath, settings);

            // Act
            var actual = target.Open();

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Stream>(actual);
        }

        [Fact]
        public void OpenInvalidId()
        {
            // Arrange
            string virtualPath = pathPrefix + dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var rs = new ResizerSection("<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((RemoteReaderPlugin)reader).Install(c);
            var settings = this.Settings;
            settings["hmac"] = "k_RU-UFkOaA";
            settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvNTk1OTg1NDE3OF8xYzJlYzZiZDc3X2IuanBn";
            var target = reader.GetFile(virtualPath, settings);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                settings["hmac"] = "k_RU-UFkOaA";
                settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvNTk1OTg1NDE3OF8xYzJlYzZiZDc3X2IuanBn";

                return settings;

            }
        }
    }
}