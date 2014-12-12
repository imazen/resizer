using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Reflection;
using ImageResizer.Plugins;
using ImageResizer.Plugins.MongoReader;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Xunit;

namespace ImageResizer.ProviderTests {
    /// <summary>
    /// Test the functionality of the <see cref="MongoReaderPlugin"/> class.
    /// </summary>
    /// <remarks>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="MongoReaderPlugin"/>. Also The methods 
    /// implementations of <see cref="IVirtualFile"/>.
    /// </remarks>
    public class MongoReaderTest {
        /// <summary>
        /// A GUID that can be used to represent a file that does not exist.
        /// </summary>
        private static Guid dummyDatabaseRecordId = Guid.NewGuid();

        /// <summary>
        /// Instantiate a new  <see cref="MongoReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void NameValueConstructor() {
            // Arrange
            var settings = this.Settings;

            // Act
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<MongoReaderPlugin>(target);
        }

        /// <summary>
        /// Instantiate a new  <see cref="MongoReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void NameValueConstructorDefaultPrefix() {
            // Arrange
            var settings = this.Settings;
            settings.Remove("prefix");

            // Act
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<MongoReaderPlugin>(target);
        }

        /// <summary>
        /// Instantiate a new  <see cref="MongoReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void NameValueConstructorNoTrailingSlashPrefix() {
            // Arrange
            var settings = this.Settings;
            settings["prefix"] = settings["prefix"].Substring(0, settings["prefix"].Length - 1);

            // Act
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<MongoReaderPlugin>(target);
        }

        /// <summary>
        /// Instantiate a new  <see cref="MongoReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void ValueConstructor() {
            // Arrange
            var settings = this.Settings;
            var mongoUrl = new MongoUrl(settings["connectionString"]);

            // Using new client, server database initialization. Wordy but recommended.
            var mongoClient = new MongoClient(mongoUrl);
            var mongoServer = mongoClient.GetServer();
            var db = mongoServer.GetDatabase(mongoUrl.DatabaseName);
            var gridSettings = new MongoGridFSSettings();

            // Act
            IVirtualImageProvider target = new MongoReaderPlugin(
                settings["prefix"],
                db,
                gridSettings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<MongoReaderPlugin>(target);
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
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            var settings = this.Settings;
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            var settings = this.Settings;
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            var settings = this.Settings;
            string virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void FileExistFileNotExisting() {
            // Arrange
            bool expected = true;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, new NameValueCollection());

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. 
        /// </summary>
        [Fact]
        public void FileExistsFileExisting() {
            // Arrange
            string id = this.CreateFileInDatabase();
            bool expected = true;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], id);
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, new NameValueCollection());

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void GetFileInvalid() {
            // Arrange
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

            // Act
            var actual = target.GetFile(virtualPath, new NameValueCollection());

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
            Assert.Equal<string>(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. 
        /// </summary>
        [Fact]
        public void GetFileValid() {
            // Arrange
            string id = this.CreateFileInDatabase();
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], id);
            IVirtualImageProvider target = new MongoReaderPlugin(settings);
            var queryString = new NameValueCollection();

            // Act
            var actual = target.GetFile(virtualPath, queryString);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
            Assert.Equal<string>(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does not include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void GetFileWithoutVirtualPathPrefix() {
            // Arrange
            var settings = this.Settings;
            string virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            var settings = this.Settings;
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            var settings = this.Settings;
            IVirtualImageProvider target = new MongoReaderPlugin(settings);

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
            string id = this.CreateFileInDatabase();
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], id);
            IVirtualImageProvider reader = new MongoReaderPlugin(settings);

            var queryString = new NameValueCollection();
            queryString.Add("width", "2000");
            queryString.Add("height", "2000");
            queryString.Add("mode", "max");
            queryString.Add("format", "jpg");
            var target = reader.GetFile(virtualPath, queryString);

            // Act
            var actual = target.Open();

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Stream>(actual);
            Assert.Equal<string>(virtualPath, target.VirtualPath);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does not exist. Without id prefix.
        /// </summary>
        [Fact]
        public void OpenInvalidIdWithoutIdPrefix() {
            // Arrange
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], dummyDatabaseRecordId.ToString("N").Substring(0, 24) + ".jpg");
            IVirtualImageProvider reader = new MongoReaderPlugin(settings);

            var queryString = new NameValueCollection();
            var target = reader.GetFile(virtualPath, queryString);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does not exist. With id prefix. With invalid sid
        /// </summary>
        [Fact]
        public void OpenInvalidIdWithIdPrefixInvalidSid() {
            // Arrange
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], "id/" + dummyDatabaseRecordId.ToString("N") + ".jpg");
            IVirtualImageProvider reader = new MongoReaderPlugin(settings);

            var queryString = new NameValueCollection();
            var target = reader.GetFile(virtualPath, queryString);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// Call the Open method with a virtualPath to a database record that 
        /// does not exist. With id prefix.
        /// </summary>
        [Fact]
        public void OpenInvalidIdWithIdPrefix() {
            // Arrange
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], "id/" + dummyDatabaseRecordId.ToString("N").Substring(0, 24) + ".jpg");
            IVirtualImageProvider reader = new MongoReaderPlugin(settings);

            var queryString = new NameValueCollection();
            var target = reader.GetFile(virtualPath, queryString);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        /// Gets a settings object for a  <see cref="MongoReaderPlugin"/>.
        /// </summary>
        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                var mongodb = Environment.GetEnvironmentVariable("resizer_test_mongo_db");
                
                settings["prefix"] = "/gridfs/";
                if(mongodb == null)
                    settings["connectionString"] = "mongodb://test:test@staff.mongohq.com:10040/resizer2";
                else
                    settings["connectionString"] = mongodb;

                return settings;
            }
        }

        /// <summary>
        /// Create an entry in the database.
        /// </summary>
        /// <returns>The id of the entry created.</returns>
        private string CreateFileInDatabase() {
            string name = "ImageResizer.ProviderTests.rose-leaf.jpg";
            using (var image = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))) {
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                    // Upload the byte array to SQL
                    return this.StoreFile(ms, "rose-leaf.jpg");
                }
            }
        }

        /// <summary>
        /// Store an image in the database
        /// </summary>
        /// <param name="data">The bytes of the image.</param>
        /// <param name="fileName">The full name of the file.</param>
        /// <returns>The id of the record created.</returns>
        private string StoreFile(MemoryStream data, string fileName) {
            data.Seek(0, SeekOrigin.Begin);
            MongoGridFS g = new MongoReaderPlugin(this.Settings).GridFS;
            MongoGridFSFileInfo fi;

            // Resize to a memory stream, max 2000x2000 jpeg
            using (MemoryStream temp = new MemoryStream(4096)) {
                new ImageJob(data, temp, new Instructions("width=2000;height=2000;mode=max;format=jpg")).Build();

                // Reset the streams
                temp.Seek(0, SeekOrigin.Begin);

                MongoGridFSCreateOptions opts = new MongoGridFSCreateOptions();
                opts.ContentType = "image/jpeg";

                fi = g.Upload(temp, Path.GetFileName(fileName), opts);
            }

            return "id/" + fi.Id.AsObjectId.ToString() + ".jpg";
        }
    }
}