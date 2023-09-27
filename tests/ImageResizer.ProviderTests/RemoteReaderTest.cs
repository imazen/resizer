// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.RemoteReader;
using ImageResizer.Util;
using Xunit;

namespace ImageResizer.ProviderTests
{
    public abstract class RemoteReaderTestBase
    {
        /// <summary>
        ///     A GUID that can be used to represent a file that does not exist.
        /// </summary>
        protected static readonly Guid dummyDatabaseRecordId = Guid.NewGuid();

        protected static readonly string pathPrefix = "/remote/farm7.static.flickr.com/6021/";

        /// <summary>
        ///     Gets a settings object for a  <see cref="RemoteReaderPlugin" />.
        /// </summary>
        protected NameValueCollection Settings => new NameValueCollection();
    }

    /// <summary>
    ///     Test the functionality of the <see cref="RemoteReaderPlugin" /> class.
    /// </summary>
    /// <remarks>
    ///     These tests exercise the methods from <see cref="IVirtualImageProvider" /> as
    ///     implemented by <see cref="RemoteReaderPlugin" />. Also The methods
    ///     implementations of <see cref="IVirtualFile" />.
    /// </remarks>
    public class RemoteReaderTest : RemoteReaderTestBase
    {
        /// <summary>
        ///     Instantiate a new  <see cref="RemoteReaderPlugin" /> object and test for success.
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
        ///     Call the FileExists method with a null value for the queryString parameter.
        /// </summary>
        /// <remarks>
        ///     The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithNullQueryString()
        {
            // Arrange
            var expected = true;
            var virtualPath = Path.Combine(pathPrefix, dummyDatabaseRecordId.ToString("B"));
            IVirtualImageProvider target = RemoteReaderPlugin.Current;

            // Act
            var actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        ///     Call the FileExists method with a null value for the virtualPath parameter.
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
        ///     Call the FileExists method with an empty string for the virtualPath parameter.
        /// </summary>
        [Fact]
        public void FileExistsWithEmptyVirtualPath()
        {
            // Arrange
            var expected = false;
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = target.FileExists(string.Empty, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        ///     Call the FileExists method with a virtualPath that does not include
        ///     the PathPrefix.
        /// </summary>
        [Fact]
        public void FileExistsWithoutVirtualPath()
        {
            // Arrange
            var expected = false;
            var virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual = target.FileExists(virtualPath, null);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        ///     Call the GetFile method without any signing data.
        /// </summary>
        [Fact]
        public void GetFileNotSigned()
        {
            // Arrange
            var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var c = Config.Current;
            ((IPlugin)target).Install(c);
            var settings = Settings;

            // Act
            var actual = Assert.Throws<ImageProcessingException>(() => target.GetFile(virtualPath, settings));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ImageProcessingException>(actual);
        }

        /// <summary>
        ///     Call the GetFile method without any signing data. With added domain
        ///     in the white-list.
        /// </summary>
        [Fact]
        public void GetFileNotSignedWhitelisted()
        {
            // Arrange
            var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"false\"><allow domain=\"farm7.static.flickr.com\" /></remotereader></resizer>");
            var c = new Config(rs);
            var target = new RemoteReaderPlugin();
            target.Install(c);
            target.AllowRemoteRequest += delegate(object sender, RemoteRequestEventArgs args)
            {
                args.DenyRequest = false;
            };
            var settings = Settings;

            // Act
            var actual = ((IVirtualImageProvider)target).GetFile(virtualPath, settings);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
            Assert.Equal(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        ///     Call the GetFile method without any signing data. With added event
        ///     handler to allow the call.
        /// </summary>
        [Fact]
        public void GetFileNotSignedAllowRemoteRequest()
        {
            // Arrange
            var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            var c = new Config();
            var target = new RemoteReaderPlugin();
            target.Install(c);
            target.AllowRemoteRequest += delegate(object sender, RemoteRequestEventArgs args)
            {
                args.DenyRequest = false;
            };
            var settings = Settings;

            // Act
            var actual = ((IVirtualImageProvider)target).GetFile(virtualPath, settings);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
            Assert.Equal(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        ///     Call the GetFile method with full signing data.
        /// </summary>
        [Fact]
        public void GetFileSigned()
        {
            // Arrange
            var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((IPlugin)target).Install(c);
            var settings = Settings;
            settings["hmac"] = "k_RU-UFkOaA";
            settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvNTk1OTg1NDE3OF8xYzJlYzZiZDc3X2IuanBn";

            // Act
            var actual = target.GetFile(virtualPath, settings);

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<IVirtualFile>(actual);
            Assert.Equal(virtualPath, actual.VirtualPath);
        }

        /// <summary>
        ///     Call the GetFile method with full signing data, but the signing key omitted.
        /// </summary>
        [Fact]
        public void GetFileSignedWithoutSigningKey()
        {
            // Arrange
            var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            IVirtualImageProvider target = new RemoteReaderPlugin();
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((IPlugin)target).Install(c);
            var settings = Settings;
            settings["hmac"] = "k_RU-UFkOaA";
            settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvNTk1OTg1NDE3OF8xYzJlYzZiZDc3X2IuanBn";

            // Act
            var actual = Assert.Throws<ImageProcessingException>(() => target.GetFile(virtualPath, settings));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ImageProcessingException>(actual);
        }

        /// <summary>
        ///     Call the GetFile method with the virtual path prefix omitted.
        /// </summary>
        [Fact]
        public void GetFileWithoutVirtualPathPrefix()
        {
            // Arrange
            var virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual =
                Assert.Throws<FileNotFoundException>(() => target.GetFile(virtualPath, new NameValueCollection()));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        ///     Call the GetFile method without any path data.
        /// </summary>
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

        /// <summary>
        ///     Call the GetFile method without any path data.
        /// </summary>
        [Fact]
        public void GetFileWithEmptyVirtualPath()
        {
            // Arrange
            IVirtualImageProvider target = new RemoteReaderPlugin();

            // Act
            var actual =
                Assert.Throws<FileNotFoundException>(() => target.GetFile(string.Empty, new NameValueCollection()));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }

        /// <summary>
        ///     Call the Open method with a virtualPath to a file that
        ///     does exist.
        /// </summary>
        /// <remarks>
        ///     Requires a file to be present at http://farm7.static.flickr.com/6021/5959854178_1c2ec6bd77_b.jpg
        /// </remarks>
        [Fact]
        public void Open()
        {
            // Arrange
            var signingKey = "ag383ht23sag#laf#lafF#oyfafqewt;2twfqw";
            var remoteUrl = "https://i.zr.io/ri/tractor-tiny.jpg";
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"" + signingKey + "\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((RemoteReaderPlugin)reader).Install(c);
            var settings = Settings;
            var signedUrl = ((RemoteReaderPlugin)reader).CreateSignedUrlWithKey(remoteUrl, "", signingKey);

            var virtualPath = signedUrl.Substring(0, signedUrl.IndexOf('?'));
            var queryStr = signedUrl.Substring(signedUrl.IndexOf('?'));

            
            var signedQuery = new Instructions(queryStr);
            var target = reader.GetFile(virtualPath, signedQuery);

          

            // Act
            var actual = target.Open();

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Stream>(actual);
            Assert.Equal(virtualPath, target.VirtualPath);
        }

        /// <summary>
        ///     Call the Open method with a virtualPath to a file that
        ///     does not exist.
        /// </summary>
        [Fact]
        public void OpenInvalidId()
        {
            // Arrange
            var virtualPath = pathPrefix + "NoFileExists.jpg";
            IVirtualImageProvider reader = new RemoteReaderPlugin();
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"ag383ht23sag#laf#lafF#oyfafqewt;2twfqw\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((RemoteReaderPlugin)reader).Install(c);
            var settings = Settings;
            settings["hmac"] = "1uiNwp7bpsk";
            settings["urlb64"] = "aHR0cDovL2Zhcm03LnN0YXRpYy5mbGlja3IuY29tLzYwMjEvTm9GaWxlRXhpc3RzLmpwZw";
            var target = reader.GetFile(virtualPath, settings);

            // Act
            var actual = Assert.Throws<FileNotFoundException>(() => target.Open());

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<FileNotFoundException>(actual);
        }
    }

    /// <summary>
    ///     Test the functionality of the <see cref="RemoteReaderPlugin" /> class.
    /// </summary>
    /// <remarks>
    ///     These tests exercise the methods from <see cref="IVirtualImageProviderAsync" /> as
    ///     implemented by <see cref="RemoteReaderPlugin" />. Also The methods
    ///     implementations of <see cref="IVirtualFileAsync" />.
    /// </remarks>
    public class RemoteReaderAsyncTest : RemoteReaderTestBase
    {
        /// <summary>
        ///     Call the GetFile method with the virtual path prefix omitted.
        /// </summary>
        [Fact]
        public Task GetFileWithoutVirtualPathPrefix()
        {
            // Arrange
            var virtualPath = dummyDatabaseRecordId.ToString("B");
            IVirtualImageProviderAsync target = new RemoteReaderPlugin();

            // Act / Assert
            return Assert.ThrowsAsync<FileNotFoundException>(() =>
                target.GetFileAsync(virtualPath, new NameValueCollection()));
        }

        /// <summary>
        ///     Call the Open method with a virtualPath to a file that
        ///     does exist.
        /// </summary>
        /// <remarks>
        ///     Requires a file to be present at https://i.zr.io/ri/tractor-tiny.jpg
        /// </remarks>
        [Fact]
        public async Task OpenAsync()
        {
            // Arrange
            //var virtualPath = pathPrefix + "5959854178_1c2ec6bd77_b.jpg";
            var signingKey = "ag383ht23sag#laf#lafF#oyfafqewt;2twfqw";
            var remoteUrl = "https://i.zr.io/ri/tractor-tiny.jpg";
            IVirtualImageProviderAsync reader = new RemoteReaderPlugin();
            var rs = new ResizerSection(
                "<resizer><remotereader signingKey=\"" + signingKey + "\" allowAllSignedRequests=\"true\" /></resizer>");
            var c = new Config(rs);
            ((RemoteReaderPlugin)reader).Install(c);
            var settings = Settings;
            var signedUrl = ((RemoteReaderPlugin)reader).CreateSignedUrlWithKey(remoteUrl, "", signingKey);

            var virtualPath = signedUrl.Substring(0, signedUrl.IndexOf('?'));
            var queryStr = signedUrl.Substring(signedUrl.IndexOf('?'));

            
            var signedQuery = new Instructions(queryStr);
            var target = await reader.GetFileAsync(virtualPath, signedQuery);

            // Act
            var actual = await target.OpenAsync();

            // Assert
            Assert.NotNull(actual);
            Assert.IsAssignableFrom<Stream>(actual);
            Assert.Equal(virtualPath, target.VirtualPath);
        }
    }
}