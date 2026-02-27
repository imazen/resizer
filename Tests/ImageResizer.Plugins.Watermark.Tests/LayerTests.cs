using System;
using System.Collections.Specialized;
using System.Drawing;
using Xunit;
using ImageResizer.Plugins.Watermark;

namespace ImageResizer.Plugins.Watermark.Tests {
    public class LayerTests {

        // --- Default values ---

        [Fact]
        public void DefaultConstructor_HasDefaults() {
            var layer = new Layer();
            Assert.Null(layer.Top);
            Assert.Null(layer.Left);
            Assert.Null(layer.Bottom);
            Assert.Null(layer.Right);
            Assert.Null(layer.Width);
            Assert.Null(layer.Height);
            Assert.Equal("image", layer.RelativeTo);
            Assert.Equal(Layer.LayerPlacement.Overlay, layer.DrawAs);
            Assert.Equal(ContentAlignment.MiddleCenter, layer.Align);
            Assert.False(layer.Fill);
        }

        // --- NameValueCollection constructor ---

        [Fact]
        public void NvcConstructor_ParsesTop() {
            var nvc = new NameValueCollection();
            nvc["top"] = "10px";
            var layer = new Layer(nvc);
            Assert.NotNull(layer.Top);
            Assert.Equal(10, layer.Top.Value);
            Assert.Equal(DistanceUnit.Units.Pixels, layer.Top.Type);
        }

        [Fact]
        public void NvcConstructor_ParsesPercentage() {
            var nvc = new NameValueCollection();
            nvc["left"] = "50%";
            var layer = new Layer(nvc);
            Assert.NotNull(layer.Left);
            Assert.Equal(50, layer.Left.Value);
            Assert.Equal(DistanceUnit.Units.Percentage, layer.Left.Type);
        }

        [Fact]
        public void NvcConstructor_ParsesAllPositions() {
            var nvc = new NameValueCollection();
            nvc["top"] = "10px";
            nvc["left"] = "20px";
            nvc["bottom"] = "30px";
            nvc["right"] = "40px";
            nvc["width"] = "50px";
            nvc["height"] = "60px";
            var layer = new Layer(nvc);
            Assert.Equal(10, layer.Top.Value);
            Assert.Equal(20, layer.Left.Value);
            Assert.Equal(30, layer.Bottom.Value);
            Assert.Equal(40, layer.Right.Value);
            Assert.Equal(50, layer.Width.Value);
            Assert.Equal(60, layer.Height.Value);
        }

        [Fact]
        public void NvcConstructor_ParsesRelativeTo() {
            var nvc = new NameValueCollection();
            nvc["relativeTo"] = "canvas";
            var layer = new Layer(nvc);
            Assert.Equal("canvas", layer.RelativeTo);
        }

        [Fact]
        public void NvcConstructor_ParsesFill() {
            var nvc = new NameValueCollection();
            nvc["fill"] = "true";
            var layer = new Layer(nvc);
            Assert.True(layer.Fill);
        }

        // --- CopyTo ---

        [Fact]
        public void CopyTo_CopiesAllProperties() {
            var source = new Layer();
            source.Top = new DistanceUnit(10, DistanceUnit.Units.Pixels);
            source.Left = new DistanceUnit(20, DistanceUnit.Units.Percentage);
            source.Bottom = new DistanceUnit(30, DistanceUnit.Units.Pixels);
            source.Right = new DistanceUnit(40, DistanceUnit.Units.Pixels);
            source.Width = new DistanceUnit(50, DistanceUnit.Units.Pixels);
            source.Height = new DistanceUnit(60, DistanceUnit.Units.Pixels);
            source.RelativeTo = "canvas";
            source.DrawAs = Layer.LayerPlacement.Background;
            source.Align = ContentAlignment.TopLeft;

            var dest = new Layer();
            source.CopyTo(dest);

            Assert.Equal(10, dest.Top.Value);
            Assert.Equal(20, dest.Left.Value);
            Assert.Equal(DistanceUnit.Units.Percentage, dest.Left.Type);
            Assert.Equal(30, dest.Bottom.Value);
            Assert.Equal(40, dest.Right.Value);
            Assert.Equal(50, dest.Width.Value);
            Assert.Equal(60, dest.Height.Value);
            Assert.Equal("canvas", dest.RelativeTo);
            Assert.Equal(Layer.LayerPlacement.Background, dest.DrawAs);
            Assert.Equal(ContentAlignment.TopLeft, dest.Align);
        }

        // --- GetDataHash ---

        [Fact]
        public void GetDataHash_SameProperties_SameHash() {
            var layer1 = new Layer();
            layer1.Top = new DistanceUnit(10, DistanceUnit.Units.Pixels);
            layer1.Left = new DistanceUnit(20, DistanceUnit.Units.Pixels);

            var layer2 = new Layer();
            layer2.Top = new DistanceUnit(10, DistanceUnit.Units.Pixels);
            layer2.Left = new DistanceUnit(20, DistanceUnit.Units.Pixels);

            Assert.Equal(layer1.GetDataHash(), layer2.GetDataHash());
        }

        [Fact]
        public void GetDataHash_DifferentProperties_DifferentHash() {
            var layer1 = new Layer();
            layer1.Top = new DistanceUnit(10, DistanceUnit.Units.Pixels);

            var layer2 = new Layer();
            layer2.Top = new DistanceUnit(20, DistanceUnit.Units.Pixels);

            Assert.NotEqual(layer1.GetDataHash(), layer2.GetDataHash());
        }

        // --- Resolve ---

        [Fact]
        public void Resolve_NullValue_ReturnsNaN() {
            var layer = new Layer();
            double result = layer.Resolve(null, 0, 100, false);
            Assert.True(double.IsNaN(result));
        }

        [Fact]
        public void Resolve_PixelValue_AddsToRelative() {
            var layer = new Layer();
            var unit = new DistanceUnit(10, DistanceUnit.Units.Pixels);
            double result = layer.Resolve(unit, 50, 100, false);
            Assert.Equal(60, result); // 50 + 10
        }

        [Fact]
        public void Resolve_PercentageValue_CalculatesCorrectly() {
            var layer = new Layer();
            var unit = new DistanceUnit(50, DistanceUnit.Units.Percentage);
            double result = layer.Resolve(unit, 0, 200, false);
            Assert.Equal(100, result); // 0 + (50 * 200 / 100)
        }

        [Fact]
        public void Resolve_Inverted_SubtractsFromRelative() {
            var layer = new Layer();
            var unit = new DistanceUnit(10, DistanceUnit.Units.Pixels);
            double result = layer.Resolve(unit, 100, 200, true);
            Assert.Equal(90, result); // 100 - 10
        }

        [Fact]
        public void Resolve_Percentage_Inverted() {
            var layer = new Layer();
            var unit = new DistanceUnit(25, DistanceUnit.Units.Percentage);
            double result = layer.Resolve(unit, 200, 400, true);
            Assert.Equal(100, result); // 200 - (25 * 400 / 100)
        }

        // --- LayerPlacement enum ---

        [Fact]
        public void LayerPlacement_HasOverlayAndBackground() {
            Assert.Equal(0, (int)Layer.LayerPlacement.Overlay);
            Assert.Equal(1, (int)Layer.LayerPlacement.Background);
        }
    }
}
