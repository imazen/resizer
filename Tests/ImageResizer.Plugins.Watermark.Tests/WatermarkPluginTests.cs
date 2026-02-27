using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ImageResizer.Plugins.Watermark;

namespace ImageResizer.Plugins.Watermark.Tests {
    public class WatermarkPluginTests {

        [Fact]
        public void Constructor_InitializesDefaults() {
            var plugin = new WatermarkPlugin();
            Assert.NotNull(plugin.DefaultImageQuery);
            Assert.NotNull(plugin.OtherImages);
        }

        [Fact]
        public void DefaultImageQuery_SetGet() {
            var plugin = new WatermarkPlugin();
            var settings = new ResizeSettings("quality=80");
            plugin.DefaultImageQuery = settings;
            Assert.Equal("80", plugin.DefaultImageQuery["quality"]);
        }

        [Fact]
        public void NamedWatermarks_DefaultsToNull() {
            var plugin = new WatermarkPlugin();
            Assert.Null(plugin.NamedWatermarks);
        }

        [Fact]
        public void NamedWatermarks_SetGet() {
            var plugin = new WatermarkPlugin();
            var dict = new Dictionary<string, IEnumerable<Layer>>();
            dict["test"] = new Layer[] { new Layer() };
            plugin.NamedWatermarks = dict;
            Assert.NotNull(plugin.NamedWatermarks);
            Assert.True(plugin.NamedWatermarks.ContainsKey("test"));
        }

        [Fact]
        public void GetSupportedQuerystringKeys_ContainsWatermark() {
            var plugin = new WatermarkPlugin();
            var keys = plugin.GetSupportedQuerystringKeys().ToList();
            Assert.Contains("watermark", keys);
        }

        [Fact]
        public void LicenseFeatureCodes_ReturnsExpectedCodes() {
            var plugin = new WatermarkPlugin();
            var codes = plugin.LicenseFeatureCodes.ToList();
            Assert.Contains("R_Creative", codes);
            Assert.Contains("R4Creative", codes);
            Assert.Contains("R4Watermark", codes);
        }

        [Fact]
        public void OtherImages_SetGet() {
            var plugin = new WatermarkPlugin();
            var layer = new ImageLayer(null);
            plugin.OtherImages = layer;
            Assert.Same(layer, plugin.OtherImages);
        }
    }
}
