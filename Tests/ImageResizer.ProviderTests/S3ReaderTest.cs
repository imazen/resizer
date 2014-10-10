using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.S3Reader2;
using ImageResizer.Storage;
using NSubstitute;
using Xunit;

namespace ImageResizer.ProviderTests {
    /// <summary>
    /// Test the functionality of the <see cref="S3Reader2"/> class.
    /// </summary>
    /// <remarks>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="S3VirtualPathProvider"/>. Also The method 
    /// implementations of <see cref="IVirtualFile"/>.
    /// </remarks>
    public class S3ReaderTest {
        private IMetadataCache model;

        private const string PathPrefix = "~/s3/";

        private const string Filename = "resizer-downloads/examples/fountain-small.jpg";

        private const string Settings = "<resizer><plugins><add name=\"S3Reader2\" buckets=\"resizer-downloads,resizer-images,resizer-web\" vpp=\"false\" /></plugins></resizer>";

        /// <summary>
        /// Initializes a new instance of the <see cref="S3ReaderTest"/> class.
        /// </summary>
        public S3ReaderTest() {
            HttpContext.Current = new HttpContext(
                new HttpRequest(string.Empty, "http://tempuri.org", string.Empty),
                new HttpResponse(new StringWriter(CultureInfo.InvariantCulture)));

            object cachedValue = null;
            this.model = Substitute.For<IMetadataCache>();
            this.model.Get(Arg.Any<string>()).Returns(x => cachedValue);
            this.model.When(x => x.Put(Arg.Any<string>(), Arg.Any<object>())).Do(x => cachedValue = x[1]);
        }

        /// <summary>
        /// Instantiate a new  <see cref="S3Reader2"/> object and test for success.
        /// </summary>
        [Fact]
        public void SettingsConstructor() {
            // Arrange

            // Act
            var target = new S3Reader2(new NameValueCollection());

            // Assert
            Assert.NotNull(target);
            Assert.IsType<S3Reader2>(target);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithNullQueryString() {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithNullVirtualPath() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;

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
        public void FileExistsWithEmptyVirtualPath() {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;

            // Act
            var actual = target.FileExists(string.Empty, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does not include
        /// the PathPrefix.
        /// </summary>
        [Fact]
        public void FileExistsWithoutVirtualPath() {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Filename;

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. The call is
        /// forced to check the database.
        /// </summary>
        [Fact]
        public void FileExistNotFastModeFileNotExisting() {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((S3Reader2)target).LazyExistenceCheck = false;
            ((S3Reader2)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "resizer-downloads/examples/fountain-xxxx.jpg");

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. The call is
        /// forced to check the database.
        /// </summary>
        [Fact]
        public void FileExistsNotFastModeFileExisting() {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((S3Reader2)target).LazyExistenceCheck = false;
            ((S3Reader2)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. The call is
        /// forced to check the database. Check Caching.
        /// </summary>
        [Fact]
        public void FileExistsNotFastModeFileExistingCheckCaching() {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            var targetAsVpp = (S3Reader2)target;
            targetAsVpp.LazyExistenceCheck = false;
            targetAsVpp.MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Ask for a file to be put in the cache.
            bool dummy = target.FileExists(virtualPath, null);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
            this.model.Received(2).Get(Arg.Is<string>(x => x == virtualPath));
            this.model.Received(1).Put(Arg.Is<string>(x => x == virtualPath), Arg.Any<object>());
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do not 
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidFastMode() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, "resizer-downloads/examples/fountain-xxxx.jpg");

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do  
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidNotFastMode() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((S3Reader2)target).LazyExistenceCheck = false;
            ((S3Reader2)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "resizer-downloads/examples/fountain-xxxx.jpg");

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do not check
        /// the database.
        /// </summary>
        [Fact]
        public void GetFileValidFastMode() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do check
        /// the database.
        /// </summary>
        [Fact]
        public void GetFileValidNotFastMode() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((S3Reader2)target).LazyExistenceCheck = false;
            ((S3Reader2)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does not include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void GetFileWithoutVirtualPathPrefix() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Filename;

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// Call the GetFile method with a null value for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void GetFileWithNullVirtualPath() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = Assert.Throws<NullReferenceException>(() => target.GetFile(null, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<NullReferenceException>(actual);
        }

        /// <summary>
        /// Call the GetFile method with an empty string for the virtualPath parameter. 
        /// </summary>
        [Fact]
        public void GetFileWithEmptyVirtualPath() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = target.GetFile(string.Empty, null);

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does exist.
        /// </summary>
        [Fact]
        public void OpenValidId() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);
            var target = reader.GetFile(virtualPath, null);

            // Act
            var actual = target.Open();

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Stream>(actual);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does not exist.
        /// </summary>
        [Fact]
        public void OpenInvalidId() {
            // Arrange
            var rs = new ResizerSection(Settings);
            var c = new Config(rs);
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, "resizer-downloads/examples/fountain-xxxx.jpg");
            var target = reader.GetFile(virtualPath, null);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }
    }
}