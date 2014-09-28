using System;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Reflection;
using ImageResizer.Plugins;
using ImageResizer.Plugins.SqlReader;
using Xunit;

namespace ImageResizer.ProviderTests {
    /// <summary>
    /// Test the functionality of the <see cref="SqlReaderPlugin"/> class.
    /// </summary>
    /// <remarks>
    /// These tests exercise the methods from <see cref="IVirtualImageProvider"/> as
    /// implemented by <see cref="SqlReaderPlugin"/>. Also The methods 
    /// implementations of <see cref="IVirtualFile"/>.
    /// </remarks>
    public class SqlReaderTest {
        /// <summary>
        /// A GUID that can be used to represent a file that does not exist.
        /// </summary>
        private static Guid dummyDatabaseRecordId = Guid.NewGuid();

        /// <summary>
        /// A GUID that can be used to represent a file that does exist.
        /// </summary>
        private Guid realDatabaseRecordId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlReaderTest"/> class.
        /// </summary>
        public SqlReaderTest() {
            // In unit tests the DataDirecry path used by connection strings is
            // null. We set the path here to ensure that connection strings 
            // that use DataDirectory function as expected.
            AppDomain.CurrentDomain.SetData(
                "DataDirectory",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));

            this.realDatabaseRecordId = this.CreateFileInDatabase();
        }

        /// <summary>
        /// Instantiate a new  <see cref="SqlReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void SettingsConstructor() {
            // Arrange
            var settings = this.Settings;

            // Act
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<SqlReaderPlugin>(target);
        }

        /// <summary>
        /// Instantiate a new  <see cref="SqlReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void NameValueConstructor() {
            // Arrange
            var settings = new NameValueCollection();

            // Act
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<SqlReaderPlugin>(target);
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
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with an invalid Guid for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithInvalidGuidId() {
            // Arrange
            bool expected = false;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B").Substring(0, 5));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a string image type id for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithStringImageIdType() {
            // Arrange
            bool expected = true;
            var settings = this.Settings;
            settings.ImageIdType = System.Data.SqlDbType.NVarChar;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B") + ".jpg");
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with an integer image type id for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithIntegerImageIdType() {
            // Arrange
            bool expected = true;
            var settings = this.Settings;
            settings.ImageIdType = System.Data.SqlDbType.Int;
            string virtualPath = Path.Combine(settings.PathPrefix, "22");
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with an invalid integer image type id for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithInvalidIntegerImageIdType() {
            // Arrange
            bool expected = false;
            var settings = this.Settings;
            settings.ImageIdType = System.Data.SqlDbType.Int;
            string virtualPath = Path.Combine(settings.PathPrefix, "TEST");
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithEmptyStringImageIdType() {
            // Arrange
            bool expected = false;
            var settings = this.Settings;
            settings.ImageIdType = System.Data.SqlDbType.NVarChar;
            string virtualPath = Path.Combine(settings.PathPrefix, string.Empty);
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
        public void FileExistCheckForModifiedFilesFileNotExisting() {
            // Arrange
            bool expected = false;
            var settings = this.Settings;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
        public void FileExistsCheckForModifiedFilesFileExisting() {
            // Arrange
            Guid id = this.realDatabaseRecordId; ////this.CreateFileInDatabase();
            bool expected = true;
            var settings = this.Settings;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.PathPrefix, id.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do not 
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidWithoutCheckForModifiedFiles() {
            // Arrange
            bool expected = true;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<DatabaseFile>(actual);
            Assert.Equal<bool>(expected, ((DatabaseFile)actual).Exists);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does not exist. Do  
        /// check the database.
        /// </summary>
        [Fact]
        public void GetFileInvalidWithCheckForModifiedFiles() {
            // Arrange
            bool expected = false;
            var settings = this.Settings;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<DatabaseFile>(actual);
            Assert.Equal<bool>(expected, ((DatabaseFile)actual).Exists);
            Assert.Equal<string>(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do not check
        /// the database.
        /// </summary>
        [Fact]
        public void GetFileValidWithoutCheckForModifiedFiles() {
            // Arrange
            bool expected = true;
            Guid id = this.realDatabaseRecordId; ////this.CreateFileInDatabase();
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings.PathPrefix, id.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<DatabaseFile>(actual);
            Assert.Equal<bool>(expected, ((DatabaseFile)actual).Exists);
            Assert.Equal<string>(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does include
        /// the PathPrefix and a record id that does exist. Do check
        /// the database.
        /// </summary>
        [Fact]
        public void GetFileValidWithCheckForModifiedFiles() {
            // Arrange
            bool expected = true;
            Guid id = this.realDatabaseRecordId; ////this.CreateFileInDatabase();
            var settings = this.Settings;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.PathPrefix, id.ToString("B"));
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<DatabaseFile>(actual);
            Assert.Equal<bool>(expected, ((DatabaseFile)actual).Exists);
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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            IVirtualImageProvider target = new SqlReaderPlugin(settings);

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
            Guid id = this.realDatabaseRecordId; ////this.CreateFileInDatabase();
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings.PathPrefix, id.ToString("B"));
            IVirtualImageProvider reader = new SqlReaderPlugin(settings);
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
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings.PathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider reader = new SqlReaderPlugin(settings);
            var target = reader.GetFile(virtualPath, null);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
            Assert.Equal<string>(virtualPath, target.VirtualPath);
        }

        /// <summary>
        /// Gets a settings object for a  <see cref="SqlReaderPlugin"/>.
        /// </summary>
        private SqlReaderSettings Settings {
            get {
                SqlReaderSettings s = new SqlReaderSettings {
                    // This is for LocalDB 2014. If you are using a previous version change "MSSQLLocalDB" to "v11.0"
#if DEBUG
                    ConnectionString = @"Server=(LocalDb)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|database.mdf;Integrated Security=true;",
                    
                    // This is for full SQL Server.
                    ////ConnectionString = @"Data Source=.;Integrated Security=true;Initial Catalog=Resizer;AttachDbFilename=|DataDirectory|database.mdf;",
#else
                    ConnectionString = @"Server=(local)\SQL2012SP1;User ID=sa;Password=Password12!;Database=Resizer;AttachDbFilename=|DataDirectory|database.mdf;",
#endif
                    PathPrefix = @"/databaseimages/",
                    StripFileExtension = true,
                    ImageIdType = System.Data.SqlDbType.UniqueIdentifier,
                    ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id",
                    ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id",
                    ImageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id",
                    CacheUnmodifiedFiles = true,
                    RequireImageExtension = false,
                    CheckForModifiedFiles = false
                };

                return s;
            }
        }

        /// <summary>
        /// Create an entry in the database.
        /// </summary>
        /// <returns>The id of the entry created.</returns>
        private Guid CreateFileInDatabase() {
            string name = "ImageResizer.ProviderTests.rose-leaf.jpg";
            using (var image = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))) {
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                    // Upload the byte array to SQL
                    return this.StoreFile(ms.ToArray(), ".jpg", "rose-leaf.jpg");
                }
            }
        }

        /// <summary>
        /// Store an image in the database
        /// </summary>
        /// <param name="data">The bytes of the image.</param>
        /// <param name="extension">The file extension indicating the file type.</param>
        /// <param name="fileName">THe full name of the file.</param>
        /// <returns>The id of the record created.</returns>
        private Guid StoreFile(byte[] data, string extension, string fileName) {
            Guid id = Guid.Empty;
            using (SqlConnection conn = new SqlConnection(this.Settings.ConnectionString)) {
                conn.Open();
                id = Guid.NewGuid();

                // Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id
                using (SqlCommand sc = new SqlCommand(
                    "INSERT INTO Images (ImageID, FileName, Extension, ContentLength, [Content]) " +
                    "VALUES (@id, @filename, @extension, @contentlength, @content)",
                    conn)) {
                    sc.Parameters.Add(new SqlParameter("id", id));
                    sc.Parameters.Add(new SqlParameter("filename", fileName));
                    sc.Parameters.Add(new SqlParameter("extension", extension));
                    sc.Parameters.Add(new SqlParameter("contentlength", data.Length));
                    sc.Parameters.Add(new SqlParameter("content", data));

                    sc.ExecuteNonQuery();
                }

                return id;
            }
        }
    }
}