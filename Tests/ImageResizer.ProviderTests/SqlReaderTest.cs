using System;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web;
using ImageResizer.Plugins;
using ImageResizer.Plugins.SqlReader;
using ImageResizer.Storage;
using NSubstitute;
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
        private IMetadataCache model;

        /// <summary>
        /// A GUID that can be used to represent a file that does not exist.
        /// </summary>
        private static Guid dummyDatabaseRecordId = Guid.NewGuid();

        /// <summary>
        /// A GUID that can be used to represent a file that does exist.
        /// </summary>
        private Guid realDatabaseRecordId;

        private static bool databaseCreated;

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

            HttpContext.Current = new HttpContext(
                new HttpRequest(string.Empty, "http://tempuri.org", string.Empty),
                new HttpResponse(new StringWriter(CultureInfo.InvariantCulture)));

            this.model = Substitute.For<IMetadataCache>();
            this.model.Get(Arg.Any<string>()).Returns(x => null);

            // SQL Server does not have permissions on the executable folder in 
            // AppVeyor to attach a LocalDB database. So we create a database in Full
            // SQL Server as we do have sa privileges.
            if (Environment.GetEnvironmentVariable("APPVEYOR") == "True") {
                this.CreateDatabase();
            }

            this.realDatabaseRecordId = this.CreateFileInDatabase();
        }

        /// <summary>
        /// Instantiate a new  <see cref="SqlReaderPlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void SettingsConstructor() {
            // Arrange

            // Act
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B"));

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.LazyExistenceCheck = false;
            settings.MetadataCache = model;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B").Substring(0, 5));

            // Act
            var actual = Assert.Throws<ArgumentNullException>(() => target.FileExists(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ArgumentNullException>(actual);
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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.ImageIdType = System.Data.SqlDbType.NVarChar;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B") + ".jpg");

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.ImageIdType = System.Data.SqlDbType.Int;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, "22");

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.ImageIdType = System.Data.SqlDbType.Int;
            settings.LazyExistenceCheck = false;
            settings.MetadataCache = model;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, "TEST");

            // Act
            var actual = Assert.Throws<ArgumentNullException>(() => target.FileExists(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ArgumentNullException>(actual);
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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.LazyExistenceCheck = false;
            settings.MetadataCache = model;
            settings.ImageIdType = System.Data.SqlDbType.NVarChar;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, string.Empty);

            // Act
            var actual = Assert.Throws<SqlException>(() => target.FileExists(virtualPath, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<SqlException>(actual);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithNullVirtualPath() {
            // Arrange
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            string virtualPath = dummyDatabaseRecordId.ToString("B");

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.LazyExistenceCheck = false;
            settings.MetadataCache = model;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B"));

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, id.ToString("B"));

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B"));

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
        public void GetFileInvalidWithCheckForModifiedFiles() {
            // Arrange
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B"));

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
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
            Guid id = this.realDatabaseRecordId; 
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, id.ToString("B"));

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
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
            Guid id = this.realDatabaseRecordId; ////this.CreateFileInDatabase();
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)target;
            settings.CheckForModifiedFiles = true;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, id.ToString("B"));

            // Act
            var actual = target.GetFile(virtualPath, null);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Blob>(actual);
            Assert.Equal<string>(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        /// Call the GetFile method with a virtualPath that does not include
        /// the PathPrefix and a record id that does not exist. 
        /// </summary>
        [Fact]
        public void GetFileWithoutVirtualPathPrefix() {
            // Arrange
            string virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider target = this.CreateSqlReaderPlugin();

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
            IVirtualImageProvider reader = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)reader;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, id.ToString("B"));
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
            IVirtualImageProvider reader = this.CreateSqlReaderPlugin();
            var settings = (SqlReaderPlugin)reader;
            string virtualPath = Path.Combine(settings.VirtualFilesystemPrefix, dummyDatabaseRecordId.ToString("B"));
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
        private SqlReaderPlugin CreateSqlReaderPlugin() {
            var p = new SqlReaderPlugin();

            // This is for LocalDB 2012. If you are using LocalDB 2014 change "v11.0" to "MSSQLLocalDB". 
            p.ConnectionString = @"Server=(LocalDb)\v11.0;AttachDbFilename=|DataDirectory|database.mdf;Integrated Security=true;";

            // This is for full SQL Server.
            ////ConnectionString = @"Data Source=.;Integrated Security=true;Initial Catalog=Resizer;AttachDbFilename=|DataDirectory|database.mdf;",
            p.VirtualFilesystemPrefix = @"/databaseimages/";
            p.StripFileExtension = true;
            p.ImageIdType = System.Data.SqlDbType.UniqueIdentifier;
            p.ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
            p.ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
            p.CacheUnmodifiedFiles = true;
            p.RequireImageExtension = false;
            p.LazyExistenceCheck = true;


            if (Environment.GetEnvironmentVariable("APPVEYOR") == "True") {
                p.ConnectionString = @"Server=(local)\SQL2012SP1;User ID=sa;Password=Password12!;Database=Resizer;";
            }

            return p;

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
            using (SqlConnection conn = new SqlConnection(this.CreateSqlReaderPlugin().ConnectionString)) {
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

        /// <summary>
        /// Create A database in SQL Server
        /// </summary>
        /// <remarks>
        /// SQL Server does not have permissions on the executable folder in 
        /// AppVeyor to attach a LocalDB database. So we create a database in Full
        /// SQL Server as we do have sa privileges.
        /// </remarks>
        private void CreateDatabase() {
            using (SqlConnection conn = new SqlConnection(@"Server=(local)\SQL2012SP1;User ID=sa;Password=Password12!;")) {
                conn.Open();

                using (SqlCommand sc = new SqlCommand(
                    "USE [master]; SELECT COUNT([name]) FROM sys.databases WHERE [name] = N'Resizer'",
                    conn)) {
                    databaseCreated = (int)sc.ExecuteScalar() != 0;
                }

                if (!databaseCreated) {
                    using (SqlCommand sc = new SqlCommand(
                        "USE [master]; CREATE DATABASE [Resizer];",
                        conn)) {
                        sc.ExecuteNonQuery();
                    }

                    using (SqlCommand sc = new SqlCommand(
                        "USE [Resizer]; " +
                        "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON;" +
                        "CREATE TABLE [dbo].[Images]([ImageID] [uniqueidentifier] NOT NULL,[FileName] [nvarchar](256) NULL,[Extension] [varchar](50) NULL,[ContentLength] [int] NOT NULL,[Content] [varbinary](max) NULL,[ModifiedDate] [datetime] NULL,[CreatedDate] [datetime] NULL,CONSTRAINT [PK_Images2] PRIMARY KEY CLUSTERED ([ImageID] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]) ON [PRIMARY];" +
                        "SET ANSI_PADDING OFF;" +
                        "ALTER TABLE [dbo].[Images] ADD  CONSTRAINT [DF_Images_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate];",
                        conn)) {
                        sc.ExecuteNonQuery();
                    }

                    databaseCreated = true;
                }
            }
        }
    }
}