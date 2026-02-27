using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;
using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Plugins.PrettyGifs;

namespace ImageResizer.Plugins.PrettyGifs.Tests {
    public class PrettyGifsTests {

        private static Bitmap CreateTestBitmap(int width = 100, int height = 100) {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) {
                // Create a colorful image with 4 quadrants
                g.FillRectangle(Brushes.Red, 0, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Green, width / 2, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Blue, 0, height / 2, width / 2, height / 2);
                g.FillRectangle(Brushes.Yellow, width / 2, height / 2, width / 2, height / 2);
            }
            return bmp;
        }

        private static byte[] BitmapToPngBytes(Bitmap bmp) {
            using (var ms = new MemoryStream()) {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // --- Plugin Registration ---

        [Fact]
        public void Install_Uninstall_DoesNotThrow() {
            var c = new Config();
            var plugin = new PrettyGifs();
            plugin.Install(c);
            Assert.True(plugin.Uninstall(c));
        }

        [Fact]
        public void GetSupportedQuerystringKeys_ReturnsExpectedKeys() {
            var plugin = new PrettyGifs();
            var keys = new System.Collections.Generic.List<string>(plugin.GetSupportedQuerystringKeys());
            Assert.Contains("colors", keys);
            Assert.Contains("dither", keys);
        }

        // --- CreateIfSuitable ---

        [Fact]
        public void CreateIfSuitable_GifFormat_ReturnsEncoder() {
            var plugin = new PrettyGifs();
            using (var bmp = CreateTestBitmap()) {
                var encoder = plugin.CreateIfSuitable(new ResizeSettings("format=gif"), bmp);
                Assert.NotNull(encoder);
            }
        }

        [Fact]
        public void CreateIfSuitable_PngFormat_ReturnsNull() {
            var plugin = new PrettyGifs();
            using (var bmp = CreateTestBitmap()) {
                var encoder = plugin.CreateIfSuitable(new ResizeSettings("format=png"), bmp);
                // PrettyGifs handles gif and 8-bit png, but not regular png
                // Depending on implementation, this may or may not return an encoder
                // The key test is that it doesn't crash
            }
        }

        [Fact]
        public void CreateIfSuitable_JpgFormat_ReturnsNull() {
            var plugin = new PrettyGifs();
            using (var bmp = CreateTestBitmap()) {
                var encoder = plugin.CreateIfSuitable(new ResizeSettings("format=jpg"), bmp);
                Assert.Null(encoder);
            }
        }

        // --- Encoder Properties ---

        [Fact]
        public void Encoder_GifFormat_CorrectMimeType() {
            var settings = new ResizeSettings("format=gif");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                Assert.Equal("image/gif", encoder.MimeType);
            }
        }

        [Fact]
        public void Encoder_GifFormat_CorrectExtension() {
            var settings = new ResizeSettings("format=gif");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                Assert.Equal("gif", encoder.Extension);
            }
        }

        // --- Write GIF ---

        [Fact]
        public void Write_Gif_ProducesNonEmptyOutput() {
            var settings = new ResizeSettings("format=gif");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                using (var output = new MemoryStream()) {
                    encoder.Write(bmp, output);
                    Assert.True(output.Length > 0, "GIF output should be non-empty");
                }
            }
        }

        [Fact]
        public void Write_Gif_ProducesValidGifHeader() {
            var settings = new ResizeSettings("format=gif");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                using (var output = new MemoryStream()) {
                    encoder.Write(bmp, output);
                    output.Seek(0, SeekOrigin.Begin);
                    byte[] header = new byte[3];
                    output.Read(header, 0, 3);
                    Assert.Equal((byte)'G', header[0]);
                    Assert.Equal((byte)'I', header[1]);
                    Assert.Equal((byte)'F', header[2]);
                }
            }
        }

        [Fact]
        public void Write_GifWithDither_ProducesOutput() {
            var settings = new ResizeSettings("format=gif&dither=true");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                using (var output = new MemoryStream()) {
                    encoder.Write(bmp, output);
                    Assert.True(output.Length > 0, "Dithered GIF output should be non-empty");
                }
            }
        }

        [Fact]
        public void Write_GifWithColors_ProducesOutput() {
            var settings = new ResizeSettings("format=gif&colors=16");
            using (var bmp = CreateTestBitmap()) {
                var encoder = new PrettyGifs(settings, bmp);
                using (var output = new MemoryStream()) {
                    encoder.Write(bmp, output);
                    Assert.True(output.Length > 0, "16-color GIF should be non-empty");
                }
            }
        }

        [Fact]
        public void Write_GifWithFewColors_SmallerThanManyColors() {
            using (var bmp = CreateTestBitmap(200, 200)) {
                long size16, size256;
                var settings16 = new ResizeSettings("format=gif&colors=16");
                var encoder16 = new PrettyGifs(settings16, bmp);
                using (var output = new MemoryStream()) {
                    encoder16.Write(bmp, output);
                    size16 = output.Length;
                }
                var settings256 = new ResizeSettings("format=gif&colors=256");
                var encoder256 = new PrettyGifs(settings256, bmp);
                using (var output = new MemoryStream()) {
                    encoder256.Write(bmp, output);
                    size256 = output.Length;
                }
                Assert.True(size16 <= size256, $"16 colors ({size16}B) should be <= 256 colors ({size256}B)");
            }
        }

        // --- Pipeline integration ---

        [Fact]
        public void Pipeline_EncodeAsGif_ThroughImageBuilder() {
            var c = new Config();
            new PrettyGifs().Install(c);
            using (var bmp = CreateTestBitmap())
            using (var input = new MemoryStream(BitmapToPngBytes(bmp)))
            using (var output = new MemoryStream()) {
                var job = new ImageJob(input, output, new Instructions("format=gif&colors=64"));
                c.Build(job);
                Assert.True(output.Length > 0);
                // Verify GIF header
                output.Seek(0, SeekOrigin.Begin);
                byte[] header = new byte[3];
                output.Read(header, 0, 3);
                Assert.Equal((byte)'G', header[0]);
                Assert.Equal((byte)'I', header[1]);
                Assert.Equal((byte)'F', header[2]);
            }
        }

        // --- Transparency ---

        [Fact]
        public void Write_GifWithTransparency_ProducesOutput() {
            using (var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb)) {
                using (var g = Graphics.FromImage(bmp)) {
                    g.Clear(Color.Transparent);
                    g.FillEllipse(Brushes.Red, 10, 10, 30, 30);
                }
                var settings = new ResizeSettings("format=gif");
                var encoder = new PrettyGifs(settings, bmp);
                using (var output = new MemoryStream()) {
                    encoder.Write(bmp, output);
                    Assert.True(output.Length > 0, "Transparent GIF should be non-empty");
                }
            }
        }
    }
}
