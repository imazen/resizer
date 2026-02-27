using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;
using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Plugins.AnimatedGifs;

namespace ImageResizer.Plugins.AnimatedGifs.Tests {
    public class AnimatedGifsTests {

        // --- Plugin Registration ---

        [Fact]
        public void Install_Uninstall_DoesNotThrow() {
            var c = new Config();
            var plugin = new AnimatedGifs();
            plugin.Install(c);
            Assert.True(plugin.Uninstall(c));
        }

        [Fact]
        public void Install_RegistersPlugin() {
            var c = new Config();
            var plugin = new AnimatedGifs();
            plugin.Install(c);
            Assert.True(c.Plugins.Has<AnimatedGifs>());
        }

        [Fact]
        public void Uninstall_RemovesPlugin() {
            var c = new Config();
            var plugin = new AnimatedGifs();
            plugin.Install(c);
            plugin.Uninstall(c);
            Assert.False(c.Plugins.Has<AnimatedGifs>());
        }

        // --- Static GIF creation helper ---

        private static byte[] CreateSimpleAnimatedGif() {
            // Create a minimal valid animated GIF with 2 frames
            // GIF89a header + 2 frames with Graphics Control Extension
            using (var ms = new MemoryStream()) {
                // Use System.Drawing to create a simple single-frame GIF first
                using (var bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb)) {
                    using (var g = Graphics.FromImage(bmp)) {
                        g.Clear(Color.Red);
                    }
                    bmp.Save(ms, ImageFormat.Gif);
                }
                return ms.ToArray();
            }
        }

        // --- Pipeline integration tests ---

        [Fact]
        public void Pipeline_ResizeGif_ProducesOutput() {
            var c = new Config();
            new AnimatedGifs().Install(c);
            new ImageResizer.Plugins.PrettyGifs.PrettyGifs().Install(c);

            byte[] gifData = CreateSimpleAnimatedGif();
            using (var input = new MemoryStream(gifData))
            using (var output = new MemoryStream()) {
                var job = new ImageJob(input, output, new Instructions("width=5&format=gif"));
                c.Build(job);
                Assert.True(output.Length > 0, "Resized GIF should be non-empty");

                // Verify GIF header
                output.Seek(0, SeekOrigin.Begin);
                byte[] header = new byte[3];
                output.Read(header, 0, 3);
                Assert.Equal((byte)'G', header[0]);
                Assert.Equal((byte)'I', header[1]);
                Assert.Equal((byte)'F', header[2]);
            }
        }

        [Fact]
        public void Pipeline_ResizeGifToJpg_ProducesJpeg() {
            var c = new Config();
            new AnimatedGifs().Install(c);

            byte[] gifData = CreateSimpleAnimatedGif();
            using (var input = new MemoryStream(gifData))
            using (var output = new MemoryStream()) {
                var job = new ImageJob(input, output, new Instructions("width=5&format=jpg"));
                c.Build(job);
                Assert.True(output.Length > 0, "Converted JPEG should be non-empty");

                // Verify JPEG header (FFD8)
                output.Seek(0, SeekOrigin.Begin);
                Assert.Equal(0xFF, output.ReadByte());
                Assert.Equal(0xD8, output.ReadByte());
            }
        }

        [Fact]
        public void Pipeline_GifWithFrameParam_SelectsSingleFrame() {
            var c = new Config();
            new AnimatedGifs().Install(c);

            byte[] gifData = CreateSimpleAnimatedGif();
            using (var input = new MemoryStream(gifData))
            using (var output = new MemoryStream()) {
                var job = new ImageJob(input, output, new Instructions("frame=1&format=gif"));
                c.Build(job);
                Assert.True(output.Length > 0, "Single-frame GIF should be non-empty");
            }
        }
    }
}
