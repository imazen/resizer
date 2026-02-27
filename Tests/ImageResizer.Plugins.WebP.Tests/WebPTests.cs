// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageResizer.Configuration;
using Imazen.WebP;
using Xunit;

namespace ImageResizer.Plugins.WebP.Tests {

    public class WebPTests {

        private static Bitmap CreateTestBitmap(int width, int height) {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) {
                g.Clear(Color.CornflowerBlue);
                g.FillRectangle(Brushes.Red, 0, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Green, width / 2, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Blue, 0, height / 2, width / 2, height / 2);
                g.FillRectangle(Brushes.Yellow, width / 2, height / 2, width / 2, height / 2);
            }
            return bmp;
        }

        [Fact]
        public void SimpleEncoder_EncodesToNonEmptyStream() {
            using (var bmp = CreateTestBitmap(100, 100))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(bmp, ms, 90);
                Assert.True(ms.Length > 0, "Encoded WebP stream should not be empty");
            }
        }

        [Fact]
        public void SimpleEncoder_LosslessEncodesToNonEmptyStream() {
            using (var bmp = CreateTestBitmap(100, 100))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(bmp, ms, -1);
                Assert.True(ms.Length > 0, "Lossless encoded WebP stream should not be empty");
            }
        }

        [Fact]
        public void SimpleDecoder_RoundtripsLossy() {
            using (var original = CreateTestBitmap(100, 100))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(original, ms, 90);
                var bytes = ms.ToArray();

                using (var decoded = new SimpleDecoder().DecodeFromBytes(bytes, bytes.Length)) {
                    Assert.Equal(100, decoded.Width);
                    Assert.Equal(100, decoded.Height);
                }
            }
        }

        [Fact]
        public void SimpleDecoder_RoundtripsLossless() {
            using (var original = CreateTestBitmap(100, 100))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(original, ms, -1);
                var bytes = ms.ToArray();

                using (var decoded = new SimpleDecoder().DecodeFromBytes(bytes, bytes.Length)) {
                    Assert.Equal(100, decoded.Width);
                    Assert.Equal(100, decoded.Height);
                }
            }
        }

        [Fact]
        public void SimpleDecoder_LosslessPreservesExactPixels() {
            using (var original = CreateTestBitmap(50, 50))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(original, ms, -1);
                var bytes = ms.ToArray();

                using (var decoded = new SimpleDecoder().DecodeFromBytes(bytes, bytes.Length)) {
                    // Spot-check corner pixels
                    var origPixel = original.GetPixel(0, 0);
                    var decodedPixel = decoded.GetPixel(0, 0);
                    Assert.Equal(origPixel.R, decodedPixel.R);
                    Assert.Equal(origPixel.G, decodedPixel.G);
                    Assert.Equal(origPixel.B, decodedPixel.B);
                }
            }
        }

        [Fact]
        public void WebPEncoderPlugin_WritesWebPStream() {
            var encoder = new WebPEncoder.WebPEncoderPlugin();
            using (var bmp = CreateTestBitmap(100, 100))
            using (var ms = new MemoryStream()) {
                encoder.Write(bmp, ms);
                Assert.True(ms.Length > 100, "Plugin-encoded WebP should produce substantial output");
            }
        }

        [Fact]
        public void WebPEncoderPlugin_PropertiesAreCorrect() {
            var encoder = new WebPEncoder.WebPEncoderPlugin();
            Assert.Equal("image/webp", encoder.MimeType);
            Assert.Equal("webp", encoder.Extension);
            Assert.True(encoder.SupportsTransparency);
        }

        [Fact]
        public void WebPEncoderPlugin_CreateIfSuitable_ReturnsEncoderForWebPFormat() {
            var encoder = new WebPEncoder.WebPEncoderPlugin();
            var settings = new ResizeSettings("format=webp");
            var result = encoder.CreateIfSuitable(settings, null);
            Assert.NotNull(result);
        }

        [Fact]
        public void WebPEncoderPlugin_CreateIfSuitable_ReturnsNullForJpeg() {
            var encoder = new WebPEncoder.WebPEncoderPlugin();
            var settings = new ResizeSettings("format=jpg");
            var result = encoder.CreateIfSuitable(settings, null);
            Assert.Null(result);
        }

        [Fact]
        public void WebPDecoderPlugin_DecodesWebPStream() {
            // Encode a bitmap to WebP bytes
            byte[] webpBytes;
            using (var bmp = CreateTestBitmap(80, 60))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(bmp, ms, 90);
                webpBytes = ms.ToArray();
            }

            // Decode through the plugin's DecodeStream
            var plugin = new WebPDecoder.WebPDecoderPlugin();
            var settings = new ResizeSettings("decoder=webp");
            using (var input = new MemoryStream(webpBytes))
            using (var decoded = plugin.DecodeStream(input, settings, "test.webp")) {
                Assert.NotNull(decoded);
                Assert.Equal(80, decoded.Width);
                Assert.Equal(60, decoded.Height);
            }
        }

        [Fact]
        public void ImageBuilder_EncodeToWebP_ProducesValidOutput() {
            var c = new Config(new ResizerSection());
            new WebPEncoder.WebPEncoderPlugin().Install(c);
            new WebPDecoder.WebPDecoderPlugin().Install(c);

            using (var bmp = CreateTestBitmap(200, 150))
            using (var output = new MemoryStream()) {
                c.CurrentImageBuilder.Build(bmp, output, new ResizeSettings("format=webp&quality=80"));
                Assert.True(output.Length > 0, "ImageBuilder should produce WebP output");

                // Verify the output is valid WebP by decoding it
                var bytes = output.ToArray();
                using (var decoded = new SimpleDecoder().DecodeFromBytes(bytes, bytes.Length)) {
                    Assert.True(decoded.Width > 0);
                    Assert.True(decoded.Height > 0);
                }
            }
        }

        [Fact]
        public void ImageBuilder_ResizeAndEncodeToWebP() {
            var c = new Config(new ResizerSection());
            new WebPEncoder.WebPEncoderPlugin().Install(c);
            new WebPDecoder.WebPDecoderPlugin().Install(c);

            using (var bmp = CreateTestBitmap(200, 200))
            using (var output = new MemoryStream()) {
                c.CurrentImageBuilder.Build(bmp, output, new ResizeSettings("width=50&format=webp"));
                var bytes = output.ToArray();

                using (var decoded = new SimpleDecoder().DecodeFromBytes(bytes, bytes.Length)) {
                    Assert.Equal(50, decoded.Width);
                    Assert.Equal(50, decoded.Height);
                }
            }
        }

        [Fact]
        public void ImageBuilder_DecodeWebP_ResizeToJpeg() {
            var c = new Config(new ResizerSection());
            new WebPEncoder.WebPEncoderPlugin().Install(c);
            new WebPDecoder.WebPDecoderPlugin().Install(c);

            // Create a WebP source
            byte[] webpBytes;
            using (var bmp = CreateTestBitmap(200, 150))
            using (var ms = new MemoryStream()) {
                new SimpleEncoder().Encode(bmp, ms, 90);
                webpBytes = ms.ToArray();
            }

            // Decode WebP → resize → encode as JPEG through the pipeline
            using (var input = new MemoryStream(webpBytes))
            using (var output = new MemoryStream()) {
                c.CurrentImageBuilder.Build(input, output, new ResizeSettings("width=100&format=jpg"), false);
                Assert.True(output.Length > 0, "Should produce JPEG output from WebP input");

                // Verify it's a valid JPEG
                output.Position = 0;
                using (var img = Image.FromStream(output)) {
                    Assert.Equal(100, img.Width);
                }
            }
        }
    }
}
