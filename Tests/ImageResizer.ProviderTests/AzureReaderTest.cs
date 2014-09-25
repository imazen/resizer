using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Reflection;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.AzureReader2;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace ImageResizer.ProviderTests {
    /// <summary>
    /// Test the functionality of the <see cref="AzureReader2Plugin"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="AzureVirtualPathProvider"/>. Also The method 
    /// implementations of <see cref="IVirtualFile"/>.
    /// </para>
    /// <para>
    /// These tests require Microsoft Azure 2.4 to be installed on the machine 
    /// and port 10000 to be free for its use. See 
    /// http://stackoverflow.com/questions/23318350/azure-storage-emulator-error-and-does-not-start
    /// for any problems.
    /// </para>
    /// </remarks>
    public class AzureReaderTest {
        private const string PathPrefix = "/azure/image-resizer/";

        private const string Filename = "rose-leaf.jpg";

        private const string ConfigXml = "<resizer><plugins><add name=\"AzureReader2\" connectionString=\"UseDevelopmentStorage=true\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureReaderTest"/> class.
        /// </summary>
        public AzureReaderTest() {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();
            this.CreateFileInDatabase();
        }

        /// <summary>
        /// Instantiate a new  <see cref="AzureReader2Plugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void NameValueConstructor() {
            // Arrange
            var settings = this.Settings;

            // Act
            var target = new AzureReader2Plugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<AzureReader2Plugin>(target);
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithNullVirtualPath() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;

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
        public void FileExistsWithoutVirtualPath() {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            string virtualPath = Filename;

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = false;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = false;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do not 
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidFastMode() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = true;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            IVirtualFile actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<AzureFile>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do  
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidNotFastMode() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = false;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = true;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<AzureFile>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do check
        /// the database.
        /// </summary>
        [Fact]
        public void GetFileValidNotFastMode() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)target).LazyExistenceCheck = false;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<AzureFile>(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does not include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void GetFileWithoutVirtualPathPrefix() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
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
            var rs = new ResizerSection(ConfigXml);
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
            var rs = new ResizerSection(ConfigXml);
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
            var rs = new ResizerSection(ConfigXml);
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)reader).LazyExistenceCheck = true;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");
            var target = reader.GetFile(virtualPath, null);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does not exist.
        /// </summary>
        [Fact]
        public void OpenInvalidContainer() {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            ((AzureVirtualPathProvider)reader).LazyExistenceCheck = true;
            string virtualPath = Path.Combine(PathPrefix.Substring(0, PathPrefix.Length - 2) + "/", "fountain-xxxx.jpg");
            var target = reader.GetFile(virtualPath, null);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// Gets a settings object for a  <see cref="AzureReader2Plugin"/>.
        /// </summary>
        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                settings["connectionstring"] = "UseDevelopmentStorage=true";
                settings["blobstorageendpoint"] = "http://127.0.0.1:10000/devstoreaccount1/";
                settings["endpoint"] = "http://127.0.0.1:10000/devstoreaccount1/";
                settings["prefix"] = "~/azure";
                settings["lazyExistenceCheck"] = "false";
                settings["vpp"] = "true";

                return settings;
            }
        }

        /// <summary>
        /// Create an entry in the database.
        /// </summary>
        private void CreateFileInDatabase() {
            string name = "ImageResizer.ProviderTests.rose-leaf.jpg";
            using (var image = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))) {
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                    var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                    var blobClient = storageAccount.CreateCloudBlobClient();

                    // Get and create the container
                    var blobContainer = blobClient.GetContainerReference("image-resizer");
                    blobContainer.CreateIfNotExists();

                    // upload a text blob
                    var blob = blobContainer.GetBlockBlobReference("rose-leaf.jpg");
                    byte[] data = ms.ToArray();
                    blob.UploadFromByteArray(ms.ToArray(), 0, data.Length);
                }
            }
        }
    }
}