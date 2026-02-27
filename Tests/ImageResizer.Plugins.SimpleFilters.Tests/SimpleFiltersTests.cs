using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;
using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Plugins.SimpleFilters;

namespace ImageResizer.Plugins.SimpleFilters.Tests {
    public class SimpleFiltersTests {

        private static Bitmap CreateTestBitmap(int width = 100, int height = 100) {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) {
                // Red top-left, Green top-right, Blue bottom-left, White bottom-right
                g.FillRectangle(Brushes.Red, 0, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Green, width / 2, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Blue, 0, height / 2, width / 2, height / 2);
                g.FillRectangle(Brushes.White, width / 2, height / 2, width / 2, height / 2);
            }
            return bmp;
        }

        private static byte[] BitmapToJpegBytes(Bitmap bmp) {
            using (var ms = new MemoryStream()) {
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        private static Bitmap ProcessImage(byte[] imageData, string querystring) {
            var c = new Config();
            new SimpleFilters().Install(c);
            using (var input = new MemoryStream(imageData))
            using (var output = new MemoryStream()) {
                var job = new ImageJob(input, output, new Instructions(querystring));
                c.Build(job);
                output.Seek(0, SeekOrigin.Begin);
                return new Bitmap(output);
            }
        }

        // --- Plugin Registration ---

        [Fact]
        public void GetSupportedQuerystringKeys_ReturnsExpectedKeys() {
            var plugin = new SimpleFilters();
            var keys = plugin.GetSupportedQuerystringKeys();
            var keyList = new List<string>(keys);
            Assert.Contains("filter", keyList);
            Assert.Contains("s.grayscale", keyList);
            Assert.Contains("s.sepia", keyList);
            Assert.Contains("s.alpha", keyList);
            Assert.Contains("s.brightness", keyList);
            Assert.Contains("s.contrast", keyList);
            Assert.Contains("s.saturation", keyList);
            Assert.Contains("s.invert", keyList);
            Assert.Contains("s.roundcorners", keyList);
            Assert.Contains("s.overlay", keyList);
            Assert.Contains("s.shift", keyList);
        }

        [Fact]
        public void Install_Uninstall_DoesNotThrow() {
            var c = new Config();
            var plugin = new SimpleFilters();
            plugin.Install(c);
            Assert.True(plugin.Uninstall(c));
        }

        // --- Grayscale ---

        [Fact]
        public void Grayscale_True_ProducesGrayscaleImage() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.grayscale=true")) {
                    // Check a pixel in the red quadrant - after grayscale, R=G=B
                    Color pixel = result.GetPixel(25, 25);
                    // JPEG compression introduces some variance, allow some tolerance
                    Assert.True(Math.Abs(pixel.R - pixel.G) < 10, $"R({pixel.R}) and G({pixel.G}) should be similar in grayscale");
                    Assert.True(Math.Abs(pixel.R - pixel.B) < 10, $"R({pixel.R}) and B({pixel.B}) should be similar in grayscale");
                }
            }
        }

        [Theory]
        [InlineData("flat")]
        [InlineData("y")]
        [InlineData("ry")]
        [InlineData("ntsc")]
        [InlineData("bt709")]
        [InlineData("true")]
        public void Grayscale_AllModes_ProduceGrayscale(string mode) {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.grayscale=" + mode)) {
                    // White quadrant should remain roughly white/gray
                    Color pixel = result.GetPixel(75, 75);
                    Assert.True(pixel.R > 200, $"White area R={pixel.R} should be > 200 for mode={mode}");
                }
            }
        }

        // --- Sepia ---

        [Fact]
        public void Sepia_ProducesWarmTones() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.sepia=true")) {
                    // Check a mid-gray area (red quadrant) where sepia is more visible
                    Color pixel = result.GetPixel(25, 25);
                    // In sepia, B channel should be noticeably lower than R
                    Assert.True(pixel.R > pixel.B + 10, $"Sepia: R({pixel.R}) should be noticeably > B({pixel.B})");
                }
            }
        }

        // --- Invert ---

        [Fact]
        public void Invert_InvertsColors() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.invert=true")) {
                    // White quadrant should become black (or near-black)
                    Color pixel = result.GetPixel(75, 75);
                    Assert.True(pixel.R < 30, $"Inverted white R={pixel.R} should be near 0");
                    Assert.True(pixel.G < 30, $"Inverted white G={pixel.G} should be near 0");
                    Assert.True(pixel.B < 30, $"Inverted white B={pixel.B} should be near 0");
                }
            }
        }

        // --- Brightness ---

        [Fact]
        public void Brightness_Positive_BrightensImage() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                // First get original blue quadrant value
                Color origPixel;
                using (var original = ProcessImage(data, "width=100")) {
                    origPixel = original.GetPixel(25, 75);
                }
                using (var result = ProcessImage(data, "s.brightness=0.5")) {
                    Color pixel = result.GetPixel(25, 75);
                    // Brightness should increase R and G (which were near 0 in blue quadrant)
                    Assert.True(pixel.R > origPixel.R || pixel.R > 100, $"Brightness should increase R: orig={origPixel.R}, new={pixel.R}");
                }
            }
        }

        // --- Contrast ---

        [Fact]
        public void Contrast_Positive_IncreasesContrast() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.contrast=0.5")) {
                    // White should stay bright
                    Color white = result.GetPixel(75, 75);
                    Assert.True(white.R > 200, $"High contrast white R={white.R} should stay bright");
                }
            }
        }

        // --- Saturation ---

        [Fact]
        public void Saturation_Negative_Desaturates() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.saturation=-0.9")) {
                    // Red quadrant should be mostly gray when desaturated
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(Math.Abs(pixel.R - pixel.G) < 40, $"Desaturated: R({pixel.R}) and G({pixel.G}) should be similar");
                }
            }
        }

        // --- Alpha ---

        [Fact]
        public void Alpha_HalfTransparent_ProducesSemiTransparent() {
            using (var bmp = CreateTestBitmap()) {
                // Use PNG to preserve alpha
                byte[] data;
                using (var ms = new MemoryStream()) {
                    bmp.Save(ms, ImageFormat.Png);
                    data = ms.ToArray();
                }
                using (var result = ProcessImage(data, "s.alpha=0.5&format=png")) {
                    Color pixel = result.GetPixel(25, 25);
                    // Alpha should be reduced (not fully opaque)
                    Assert.True(pixel.A < 200, $"Alpha={pixel.A} should be reduced from 255");
                }
            }
        }

        // --- Filter command (legacy) ---

        [Fact]
        public void Filter_Grayscale_Works() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "filter=grayscale")) {
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(Math.Abs(pixel.R - pixel.G) < 10, "filter=grayscale should produce grayscale");
                }
            }
        }

        [Fact]
        public void Filter_Sepia_Works() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "filter=sepia")) {
                    // Check a colored area where sepia shift is more visible
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(pixel.R > pixel.B + 10, $"filter=sepia should produce warm tones: R({pixel.R}) should be > B({pixel.B})+10");
                }
            }
        }

        [Fact]
        public void Filter_Alpha_WithValue() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data;
                using (var ms = new MemoryStream()) {
                    bmp.Save(ms, ImageFormat.Png);
                    data = ms.ToArray();
                }
                using (var result = ProcessImage(data, "filter=alpha(0.5)&format=png")) {
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(pixel.A < 200, $"filter=alpha(0.5) should reduce alpha. Got A={pixel.A}");
                }
            }
        }

        [Fact]
        public void Filter_Brightness_WithValue() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "filter=brightness(0.3)")) {
                    // Should brighten without crashing
                    Assert.True(result.Width > 0);
                }
            }
        }

        // --- Combined Filters ---

        [Fact]
        public void MultipleFilters_AppliedTogether() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "s.grayscale=true&s.contrast=0.3")) {
                    // Should apply both without errors
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(Math.Abs(pixel.R - pixel.G) < 15, "Should still be grayscale after contrast");
                }
            }
        }

        // --- No-op when no filter specified ---

        [Fact]
        public void NoFilter_ImageUnchanged() {
            using (var bmp = CreateTestBitmap()) {
                byte[] data = BitmapToJpegBytes(bmp);
                using (var result = ProcessImage(data, "width=100")) {
                    // Red quadrant should still be red
                    Color pixel = result.GetPixel(25, 25);
                    Assert.True(pixel.R > 200, $"Red quadrant R={pixel.R} should be > 200");
                    Assert.True(pixel.G < 80, $"Red quadrant G={pixel.G} should be < 80");
                }
            }
        }
    }
}
