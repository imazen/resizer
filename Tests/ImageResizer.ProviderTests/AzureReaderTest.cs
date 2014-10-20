using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Reflection;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.AzureReader2;
using ImageResizer.Storage;
using Microsoft.WindowsAzure.Storage;
using NSubstitute;
using Xunit;

namespace ImageResizer.ProviderTests {
    /// <summary>
    /// Test the functionality of the <see cref="AzureReader2Plugin"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="AzureReader2Plugin"/>. Also The method 
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
        private IMetadataCache model;

        private const string PathPrefix = "/azure/image-resizer/";

        private const string Filename = "rose-leaf.jpg";

        private const string ConfigXml = "<resizer><plugins><add name=\"AzureReader2\" prefix=\"/azure/\" connectionString=\"UseDevelopmentStorage=true\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureReaderTest"/> class.
        /// </summary>
        public AzureReaderTest() {
            CloudStorageEmulatorShepherd.Start();
            this.CreateFileInDatabase();

            this.model = Substitute.For<IMetadataCache>();
            this.model.Get(Arg.Any<string>()).Returns(x => null);
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
        /// Test <see cref="AzureReader2Plugin"/> install capabilities.
        /// This plugin can be installed more than once.
        /// </summary>
        [Fact]
        public void InstallTwiceTest() {
            // Arrange
            var settings = this.Settings;
            var c = new Config();
            var target = new AzureReader2Plugin(settings);

            // Act
            var dummy = target.Install(c);
            var actual = target.Install(c);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<AzureReader2Plugin>(actual);
        }

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> install capabilities.
        /// Can only be installed more than once.
        /// </summary>
        [Fact]
        public void InstallTwiceUninstallOnceTest() {
            // Arrange
            var settings = this.Settings;
            var c = new Config();
            var target = new AzureReader2Plugin(settings);

            // Act
            var dummy = target.Install(c);
            var wasUninstalled = target.Uninstall(c);
            var actual = target.Install(c);

            // Assert
            Assert.True(wasUninstalled);
            Assert.NotNull(actual);
            Assert.IsType<AzureReader2Plugin>(actual);
        }

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> RegisterAsVirtualPathProvider property.
        /// </summary>
        [Fact]
        public void ExposeAsVppTest() {
            // Arrange
            bool expected = false; // Default is true, force property to change.
            var settings = this.Settings;
            var target = new AzureReader2Plugin(settings);

            // Act
            target.ExposeAsVpp = expected;
            var actual = target.ExposeAsVpp;

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        ///// <summary>
        ///// Test <see cref="AzureReader2Plugin"/> Issues.
        ///// Simple constructor should not create any issues.
        ///// </summary>
        //[Fact]
        //public void WithoutIssuesTest() {
        //    // Arrange
        //    int expected = 0; // No issues to report.
        //    var settings = this.Settings;
        //    var target = new AzureReader2Plugin(settings);

        //    // Act
        //    //var actual = target.GetIssues().ToList().Count;

        //    // Assert
        //    //Assert.Equal<int>(expected, actual);
        //}

        ///// <summary>
        ///// Test <see cref="AzureReader2Plugin"/> Issues.
        ///// Config.Install(...) should create one issues.
        ///// </summary>
        //[Fact]
        //public void WithIssuesTest() {
        //    // Arrange
        //    int expected = 1; // Should generate one issue.
        //    var rs = new ResizerSection(ConfigXml);
        //    var c = new Config(rs);
        //    IVirtualImageProvider target = c.Plugins.Get<AzureReader2Plugin>();

        //    // Act
        //    var actual = 0;
        //    foreach (var provider in c.Plugins.GetAll<IIssueProvider>()) {
        //        if (provider is AzureReader2Plugin) {
        //            actual = provider.GetIssues().ToList().Count;
        //        }
        //    }

        //    // Assert
        //    Assert.Equal<int>(expected, actual);
        //}

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> constructor and install capabilities.
        /// Should not uninstall if not installed. Failure is silent.
        /// </summary>
        [Fact]
        public void UninstallWithoutInstallingTest() {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var settings = this.Settings;
            var target = new AzureReader2Plugin(settings);

            // Act
            bool actual = target.Uninstall(c);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> constructor and install capabilities.
        /// Omit the connection string from the configuration.
        /// </summary>
        [Fact]
        public void InstallWithoutBlobStorageConnectionStringTest() {
            // Arrange
            var settings = this.Settings;
            settings.Remove("connectionstring");
            var c = new Config();
            var target = new AzureReader2Plugin(settings);

            // Act
            var actual = Assert.Throws<InvalidOperationException>(() => target.Install(c));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<InvalidOperationException>(actual);
        }

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> constructor and install capabilities.
        /// Send an end point URL without trailing slash.
        /// </summary>
        [Fact]
        public void InstallWithoutBlobStorageEndpointTrailingSlashConnectionTest() {
            // Arrange
            var settings = this.Settings;
            settings["blobstorageendpoint"] = settings["blobstorageendpoint"].Substring(0, settings["blobstorageendpoint"].Length - 1);
            var c = new Config();
            var target = new AzureReader2Plugin(settings);

            // Act
            var actual = target.Install(c);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IPlugin>(actual);
        }

        /// <summary>
        /// Test <see cref="AzureReader2Plugin"/> constructor and install capabilities.
        /// Omit the end point URL from the configuration.
        /// </summary>
        [Fact]
        public void InstallWithoutBlobStorageEndpointTest() {
            // Arrange
            var settings = this.Settings;
            settings.Remove("blobstorageendpoint");
            settings.Remove("endpoint");
            var c = new Config();
            var target = new AzureReader2Plugin(settings);

            // Act
            var actual = target.Install(c);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<AzureReader2Plugin>(actual);
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();

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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();

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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. The call is
        /// not forced to check the database.
        /// </summary>
        [Fact]
        public void FileExistFastModeFileNotExisting() {
            // Arrange
            bool expected = true; // Fast Mode assumes the file exists.
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = true;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. The call is
        /// not forced to check the database.
        /// </summary>
        [Fact]
        public void FileExistsFastModeFileExisting() {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = true;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. The call is
        /// forced to check the database. The connection string is not valid
        /// and should generate a StorageException.
        /// </summary>
        [Fact]
        public void FileExistsNotFastModeFileExistingStorageException() {
            // Arrange
            var rs = new ResizerSection("<resizer><plugins><add name=\"AzureReader2\" prefix=\"/azure/\" connectionString=\"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>");
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = Assert.Throws<StorageException>(() => target.FileExists(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<StorageException>(actual);
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = true;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            IVirtualFile actual = target.GetFile(virtualPath, null);

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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do check
        /// the database. The connection string should generate a StorageExcetion.
        /// </summary>
        [Fact]
        public void GetFileInvalidNotFastModeStorageException() {
            // Arrange
            var rs = new ResizerSection("<resizer><plugins><add name=\"AzureReader2\" prefix=\"/azure/\" connectionString=\"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>");
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, "fountain-xxxx.jpg");

            // Act
            var actual = Assert.Throws<StorageException>(() => target.GetFile(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<StorageException>(actual);
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = true;
            ((AzureReader2Plugin)target).MetadataCache = model;
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
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
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
        /// the database. The connection string should generate a StorageExcetion.
        /// </summary>
        [Fact]
        public void GetFileValidNotFastModeStorageException() {
            // Arrange
            var rs = new ResizerSection("<resizer><plugins><add name=\"AzureReader2\" prefix=\"/azure/\" connectionString=\"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>");
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)target).LazyExistenceCheck = false;
            ((AzureReader2Plugin)target).MetadataCache = model;
            string virtualPath = Path.Combine(PathPrefix, Filename);

            // Act
            var actual = Assert.Throws<StorageException>(() => target.GetFile(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<StorageException>(actual);
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
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
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)reader).LazyExistenceCheck = true;
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
            IVirtualImageProvider reader = (IVirtualImageProvider)c.Plugins.Get<AzureReader2Plugin>();
            ((AzureReader2Plugin)reader).LazyExistenceCheck = true;
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