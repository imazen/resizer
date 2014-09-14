using System;
using System.Text;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;
using ImageResizer.Plugins.SqlReader;
using System.Collections.Specialized;
using System.IO;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Reflection;
using ImageResizer.Plugins;
using ImageResizer.Plugins.RemoteReader;

namespace ImageResizer.AllPlugins.Tests
{
    public class RemoteReaderTest 
    {
        private static string pathPrefix = @"remote";

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

        [Fact]
        public void FileExistsWithNullQueryString()
        {
            // Arrange
            bool expected = false;
            string virtualPath = Path.Combine(pathPrefix, "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");
            IVirtualImageProvider target = RemoteReaderPlugin.Current;

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

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

        [Fact]
        public void FileExistsWithoutVirtualPath()
        {
            // Arrange
            bool expected = false;
            string virtualPath = "{89A5100C-48F2-4024-AF9E-6AE662F720A2}";
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        [Fact]
        public void FileExistCheckForModifiedFilesFileNotExisting()
        {
            // Arrange
            bool expected = false;
            string virtualPath = Path.Combine(pathPrefix, "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            bool actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        //[Fact]
        //public void FileExistsCheckForModifiedFilesFileExisting()
        //{
        //    // Arrange
        //    bool expected = true;
        //    string virtualPath = Path.Combine(pathPrefix, id.ToString("X"));
        //    IVirtualImageProvider target = new RemoteReaderPlugin();

        //    // Act
        //    bool actual = target.FileExists(virtualPath, null);

        //    // Assert
        //    Assert.StrictEqual<bool>(expected, actual);
        //}

        [Fact]
        public void GetFile()
        {
            // Arrange
            string virtualPath = Path.Combine(pathPrefix, "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = target.GetFile(virtualPath, new NameValueCollection());

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
        }

        [Fact]
        public void GetFileWithoutVirtualPathPrefix()
        {
            // Arrange
            string virtualPath = "{89A5100C-48F2-4024-AF9E-6AE662F720A2}";
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = target.GetFile(virtualPath, new NameValueCollection());

            // Assert
            Assert.Null(actual);
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
            var actual = target.GetFile(string.Empty, null);

            // Assert
            Assert.Null(actual);
        }

        //[Fact]
        //public void Open()
        //{
        //    // Arrange
        //    string virtualPath = Path.Combine(pathPrefix, id.ToString("X"));
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
            string virtualPath = Path.Combine(pathPrefix, "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var target = reader.GetFile(virtualPath, new NameValueCollection());

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }
    }
}