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
            string virtualPath = pathPrefix + dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var c = Config.Current;
            ((RemoteReaderPlugin)target).Install(c);
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

        //[Fact]
        //public void Open()
        //{
        //    // Arrange
        //    string virtualPath = Path.Combine(pathPrefix, id.ToString("B"));
        //    IVirtualImageProvider reader = new RemoteReaderPlugin();
        //    var target = reader.GetFile(virtualPath, null);

        //    // Act
        //    var actual = target.Open();

        //    // Assert
        //    Assert.NotNull(actual);
        //    Assert.IsAssignableFrom<Stream>(actual);
        //}

        [Fact]
        public void OpenInvalidId()
        {
            // Arrange
            string virtualPath = pathPrefix + dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var target = reader.GetFile(virtualPath, new NameValueCollection());

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                settings["hmac"] = "a2099ba2099b";
                settings["urlb64"] = "ag383ht23sag#laf#lafF#oyfafqewt;2twfqw";

                return settings;

            }
        }
    }
}