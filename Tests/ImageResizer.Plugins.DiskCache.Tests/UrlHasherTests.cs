using System;
using Xunit;
using ImageResizer.Plugins.DiskCache;

#pragma warning disable 612 // Suppress obsolete warning for UrlHasher
namespace ImageResizer.Plugins.DiskCache.Tests {
    public class UrlHasherTests {

        [Fact]
        public void Hash_SameInput_SameOutput() {
            var hasher = new UrlHasher();
            string hash1 = hasher.hash("test/image.jpg?width=100", 0, "\\");
            string hash2 = hasher.hash("test/image.jpg?width=100", 0, "\\");
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Hash_DifferentInput_DifferentOutput() {
            var hasher = new UrlHasher();
            string hash1 = hasher.hash("test/image1.jpg", 0, "\\");
            string hash2 = hasher.hash("test/image2.jpg", 0, "\\");
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Hash_NoSubfolders_NoDirSeparator() {
            var hasher = new UrlHasher();
            string hash = hasher.hash("test", 0, "\\");
            Assert.DoesNotContain("\\", hash);
        }

        [Fact]
        public void Hash_WithSubfolders_ContainsDirSeparator() {
            var hasher = new UrlHasher();
            string hash = hasher.hash("test", 32, "\\");
            Assert.Contains("\\", hash);
        }

        [Fact]
        public void Hash_IsHexEncoded() {
            var hasher = new UrlHasher();
            string hash = hasher.hash("test", 0, "\\");
            // SHA256 produces 32 bytes = 64 hex chars
            Assert.Equal(64, hash.Length);
            foreach (char c in hash) {
                Assert.True((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'),
                    $"Unexpected char '{c}' in hash");
            }
        }

        [Fact]
        public void Hash_WithSubfolders_ProducesSubfolderPrefix() {
            var hasher = new UrlHasher();
            string hash = hasher.hash("test", 256, "/");
            string[] parts = hash.Split('/');
            Assert.Equal(2, parts.Length);
            Assert.True(parts[0].Length > 0, "Subfolder prefix should not be empty");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(256)]
        [InlineData(1024)]
        public void Hash_VariousSubfolderCounts_Succeeds(int subfolders) {
            var hasher = new UrlHasher();
            string hash = hasher.hash("test/path", subfolders, "\\");
            Assert.NotNull(hash);
            Assert.Contains("\\", hash);
        }

        [Fact]
        public void Hash_EmptyString_Succeeds() {
            var hasher = new UrlHasher();
            string hash = hasher.hash("", 0, "\\");
            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length);
        }
    }
}
#pragma warning restore 612
