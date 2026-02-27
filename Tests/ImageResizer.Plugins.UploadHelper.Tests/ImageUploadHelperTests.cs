using System;
using System.IO;
using Xunit;
using ImageResizer;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.UploadHelper.Tests {
    public class ImageUploadHelperTests {

        private static ImageUploadHelper CreateHelper() {
            return new ImageUploadHelper(new Config());
        }

        // --- GetExtension ---

        [Fact]
        public void GetExtension_SimpleFilename_ReturnsExtension() {
            var helper = CreateHelper();
            Assert.Equal("jpg", helper.GetExtension("photo.jpg"));
        }

        [Fact]
        public void GetExtension_PathWithDirs_ReturnsExtension() {
            var helper = CreateHelper();
            Assert.Equal("png", helper.GetExtension("/images/photo.png"));
        }

        [Fact]
        public void GetExtension_MultipleExtensions_ReturnsLast() {
            var helper = CreateHelper();
            Assert.Equal("zip", helper.GetExtension("archive.txt.zip"));
        }

        [Fact]
        public void GetExtension_NoExtension_ReturnsNull() {
            var helper = CreateHelper();
            Assert.Null(helper.GetExtension("noext"));
        }

        [Fact]
        public void GetExtension_WithQuerystring_ReturnsExtension() {
            var helper = CreateHelper();
            Assert.Equal("jpg", helper.GetExtension("photo.jpg?width=100"));
        }

        [Fact]
        public void GetExtension_WithFragment_ReturnsExtension() {
            var helper = CreateHelper();
            Assert.Equal("png", helper.GetExtension("image.png#section"));
        }

        [Fact]
        public void GetExtension_WithSpace_StopsAtSpace() {
            // Space after extension dot means space is the last separator, not dot
            var helper = CreateHelper();
            Assert.Null(helper.GetExtension("photo.jpg something"));
        }

        [Fact]
        public void GetExtension_BackslashPath_ReturnsExtension() {
            var helper = CreateHelper();
            Assert.Equal("gif", helper.GetExtension("C:\\images\\photo.gif"));
        }

        [Fact]
        public void GetExtension_EndsWithDot_ReturnsEmpty() {
            var helper = CreateHelper();
            Assert.Equal("", helper.GetExtension("file."));
        }

        // --- NormalizeExtension ---

        [Theory]
        [InlineData("jpeg", "jpg")]
        [InlineData("jpe", "jpg")]
        [InlineData("jif", "jpg")]
        [InlineData("jfif", "jpg")]
        [InlineData("jfi", "jpg")]
        [InlineData("exif", "jpg")]
        [InlineData("tiff", "tif")]
        [InlineData("tff", "tif")]
        public void NormalizeExtension_MapsAliases(string input, string expected) {
            var helper = CreateHelper();
            Assert.Equal(expected, helper.NormalizeExtension(input));
        }

        [Fact]
        public void NormalizeExtension_UnknownExtension_ReturnsLowered() {
            var helper = CreateHelper();
            Assert.Equal("png", helper.NormalizeExtension("PNG"));
        }

        [Fact]
        public void NormalizeExtension_Null_ReturnsNull() {
            var helper = CreateHelper();
            Assert.Null(helper.NormalizeExtension(null));
        }

        [Fact]
        public void NormalizeExtension_AlreadyNormalized_ReturnsSame() {
            var helper = CreateHelper();
            Assert.Equal("jpg", helper.NormalizeExtension("jpg"));
        }

        [Fact]
        public void NormalizeExtension_MixedCase_Normalizes() {
            var helper = CreateHelper();
            Assert.Equal("jpg", helper.NormalizeExtension("JPEG"));
        }

        // --- IsExtensionWhitelisted ---

        [Fact]
        public void IsExtensionWhitelisted_WithCustomList_ReturnsTrueForMatch() {
            var helper = CreateHelper();
            Assert.True(helper.IsExtensionWhitelisted("jpg", new string[] { "jpg", "png" }));
        }

        [Fact]
        public void IsExtensionWhitelisted_WithCustomList_ReturnsFalseForMismatch() {
            var helper = CreateHelper();
            Assert.False(helper.IsExtensionWhitelisted("exe", new string[] { "jpg", "png" }));
        }

        [Fact]
        public void IsExtensionWhitelisted_CaseInsensitive() {
            var helper = CreateHelper();
            Assert.True(helper.IsExtensionWhitelisted("JPG", new string[] { "jpg", "png" }));
        }

        // --- GenerateSafeImageName ---

        [Fact]
        public void GenerateSafeImageName_NoInputs_UsesDefaultExtension() {
            var helper = CreateHelper();
            string name = helper.GenerateSafeImageName(null, null, ".unknown", new string[] { "jpg" });
            Assert.EndsWith(".unknown", name);
            Assert.Equal(32 + ".unknown".Length, name.Length); // GUID 'N' format is 32 chars
        }

        [Fact]
        public void GenerateSafeImageName_WithOriginalPath_UsesPathExtension() {
            var helper = CreateHelper();
            string name = helper.GenerateSafeImageName(null, "photo.jpg", ".unknown", new string[] { "jpg" });
            Assert.EndsWith("jpg", name);
        }

        [Fact]
        public void GenerateSafeImageName_UnrecognizedThrows_WhenExtensionNull() {
            var helper = CreateHelper();
            Assert.Throws<ArgumentException>(() =>
                helper.GenerateSafeImageName(null, "file.exe", null, new string[] { "jpg" }));
        }

        [Fact]
        public void GenerateSafeImageName_GeneratesUniqueNames() {
            var helper = CreateHelper();
            string name1 = helper.GenerateSafeImageName(null, "photo.jpg", ".unknown", new string[] { "jpg" });
            string name2 = helper.GenerateSafeImageName(null, "photo.jpg", ".unknown", new string[] { "jpg" });
            Assert.NotEqual(name1, name2); // GUID-based, always unique
        }

        // --- GuessFileTypeBySignature ---

        [Fact]
        public void GuessFileTypeBySignature_NonSeekableStream_Throws() {
            var helper = CreateHelper();
            using (var ms = new NonSeekableStream()) {
                Assert.Throws<ArgumentException>(() => helper.GuessFileTypeBySignature(ms));
            }
        }

        [Fact]
        public void GuessFileTypeBySignature_EmptyStream_ReturnsNull() {
            // With no IFileSignatureProvider plugins, returns null
            var helper = CreateHelper();
            using (var ms = new MemoryStream(new byte[0])) {
                var result = helper.GuessFileTypeBySignature(ms);
                Assert.Null(result);
            }
        }

        // --- GetWhitelistedExtension ---

        [Fact]
        public void GetWhitelistedExtension_ByPath_ReturnsExtension() {
            var helper = CreateHelper();
            string ext = helper.GetWhitelistedExtension(null, "photo.png", new string[] { "jpg", "png", "gif" });
            Assert.Equal("png", ext);
        }

        [Fact]
        public void GetWhitelistedExtension_NonWhitelisted_ReturnsNull() {
            var helper = CreateHelper();
            string ext = helper.GetWhitelistedExtension(null, "file.exe", new string[] { "jpg", "png" });
            Assert.Null(ext);
        }

        [Fact]
        public void GetWhitelistedExtension_NormalizesExtension() {
            var helper = CreateHelper();
            string ext = helper.GetWhitelistedExtension(null, "photo.jpeg", new string[] { "jpg", "png" });
            Assert.Equal("jpg", ext);
        }

        [Fact]
        public void GetWhitelistedExtension_NullPath_ReturnsNull() {
            var helper = CreateHelper();
            string ext = helper.GetWhitelistedExtension(null, null, new string[] { "jpg" });
            Assert.Null(ext);
        }

        // --- IsUploadedFileAnImage ---

        [Fact]
        public void IsUploadedFileAnImage_Null_ReturnsFalse() {
            var helper = CreateHelper();
            Assert.False(helper.IsUploadedFileAnImage(null));
        }

        [Fact]
        public void IsUploadedFileAnImage_ObjectWithoutProperties_ReturnsFalse() {
            var helper = CreateHelper();
            Assert.False(helper.IsUploadedFileAnImage("not a file"));
        }

        [Fact]
        public void IsUploadedFileAnImage_MockUpload_RecognizesImage() {
            var helper = CreateHelper();
            var mock = new MockUploadFile("image.jpg", new MemoryStream(new byte[0]));
            // Uses custom whitelist so we don't need pipeline registration
            Assert.True(helper.IsUploadedFileAnImage(mock, new string[] { "jpg", "png" }));
        }

        [Fact]
        public void IsUploadedFileAnImage_MockUpload_RejectsNonImage() {
            var helper = CreateHelper();
            var mock = new MockUploadFile("document.exe", new MemoryStream(new byte[0]));
            Assert.False(helper.IsUploadedFileAnImage(mock, new string[] { "jpg", "png" }));
        }

        // --- Helper classes ---

        private class NonSeekableStream : Stream {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => 0;
            public override long Position { get => 0; set { } }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => 0;
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) { }
            public override void Write(byte[] buffer, int offset, int count) { }
        }

        private class MockUploadFile {
            public string FileName { get; set; }
            public Stream InputStream { get; set; }
            public MockUploadFile(string fileName, Stream inputStream) {
                FileName = fileName;
                InputStream = inputStream;
            }
        }
    }
}
