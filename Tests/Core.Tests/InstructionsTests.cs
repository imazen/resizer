using System;
using System.Collections.Specialized;
using Xunit;
using ImageResizer;

namespace ImageResizer.Core.Tests {
    public class InstructionsTests {

        [Fact]
        public void DefaultConstructor_AllPropertiesNull() {
            var i = new Instructions();
            Assert.Null(i.Width);
            Assert.Null(i.Height);
            Assert.Null(i.Mode);
            Assert.Null(i.Format);
        }

        [Fact]
        public void QuerystringConstructor_ParsesWidthHeight() {
            var i = new Instructions("width=100&height=200");
            Assert.Equal(100, i.Width);
            Assert.Equal(200, i.Height);
        }

        [Fact]
        public void QuerystringConstructor_ParsesMode() {
            var i = new Instructions("mode=crop");
            Assert.Equal(FitMode.Crop, i.Mode);
        }

        [Theory]
        [InlineData("mode=max", FitMode.Max)]
        [InlineData("mode=pad", FitMode.Pad)]
        [InlineData("mode=stretch", FitMode.Stretch)]
        [InlineData("mode=crop", FitMode.Crop)]
        public void Mode_ParsesAllValues(string qs, FitMode expected) {
            var i = new Instructions(qs);
            Assert.Equal(expected, i.Mode);
        }

        [Theory]
        [InlineData("anchor=topleft", AnchorLocation.TopLeft)]
        [InlineData("anchor=topcenter", AnchorLocation.TopCenter)]
        [InlineData("anchor=topright", AnchorLocation.TopRight)]
        [InlineData("anchor=middleleft", AnchorLocation.MiddleLeft)]
        [InlineData("anchor=middlecenter", AnchorLocation.MiddleCenter)]
        [InlineData("anchor=middleright", AnchorLocation.MiddleRight)]
        [InlineData("anchor=bottomleft", AnchorLocation.BottomLeft)]
        [InlineData("anchor=bottomcenter", AnchorLocation.BottomCenter)]
        [InlineData("anchor=bottomright", AnchorLocation.BottomRight)]
        public void Anchor_ParsesAllValues(string qs, AnchorLocation expected) {
            var i = new Instructions(qs);
            Assert.Equal(expected, i.Anchor);
        }

        [Theory]
        [InlineData("scale=downscaleonly", ScaleMode.DownscaleOnly)]
        [InlineData("scale=both", ScaleMode.Both)]
        [InlineData("scale=upscaleonly", ScaleMode.UpscaleOnly)]
        [InlineData("scale=upscalecanvas", ScaleMode.UpscaleCanvas)]
        public void Scale_ParsesAllValues(string qs, ScaleMode expected) {
            var i = new Instructions(qs);
            Assert.Equal(expected, i.Scale);
        }

        [Fact]
        public void Format_SetGet() {
            var i = new Instructions();
            i.Format = "png";
            Assert.Equal("png", i.Format);
        }

        [Fact]
        public void Width_SetGet() {
            var i = new Instructions();
            i.Width = 500;
            Assert.Equal(500, i.Width);
        }

        [Fact]
        public void Height_SetGet() {
            var i = new Instructions();
            i.Height = 300;
            Assert.Equal(300, i.Height);
        }

        [Fact]
        public void MaxWidth_MaxHeight_SetGet() {
            var i = new Instructions("maxwidth=800&maxheight=600");
            Assert.Equal("800", i["maxwidth"]);
            Assert.Equal("600", i["maxheight"]);
        }

        [Fact]
        public void JpegQuality_SetGet() {
            var i = new Instructions();
            i.JpegQuality = 85;
            Assert.Equal(85, i.JpegQuality);
        }

        [Fact]
        public void Rotate_SetGet() {
            var i = new Instructions("rotate=90");
            Assert.Equal(90.0, i.Rotate);
        }

        [Fact]
        public void CropRectangle_SetGet() {
            var i = new Instructions();
            i.CropRectangle = new double[] { 10, 20, 90, 80 };
            Assert.Equal(new double[] { 10, 20, 90, 80 }, i.CropRectangle);
        }

        [Fact]
        public void CropRectangle_FromQuerystring() {
            var i = new Instructions("crop=10,20,90,80");
            Assert.NotNull(i.CropRectangle);
            Assert.Equal(10, i.CropRectangle[0]);
            Assert.Equal(20, i.CropRectangle[1]);
            Assert.Equal(90, i.CropRectangle[2]);
            Assert.Equal(80, i.CropRectangle[3]);
        }

        [Fact]
        public void BackgroundColor_SetGet() {
            var i = new Instructions();
            i.BackgroundColor = "ff0000";
            Assert.Equal("ff0000", i["bgcolor"]);
        }

        [Fact]
        public void FinalFlip_SetGet() {
            var i = new Instructions();
            i.FinalFlip = FlipMode.X;
            Assert.Equal(FlipMode.X, i.FinalFlip);
        }

        [Theory]
        [InlineData("flip=x", FlipMode.X)]
        [InlineData("flip=y", FlipMode.Y)]
        [InlineData("flip=xy", FlipMode.XY)]
        [InlineData("flip=none", FlipMode.None)]
        public void FinalFlip_ParsesFromQuerystring(string qs, FlipMode expected) {
            var i = new Instructions(qs);
            Assert.Equal(expected, i.FinalFlip);
        }

        [Fact]
        public void SourceFlip_SetGet() {
            var i = new Instructions();
            i.SourceFlip = FlipMode.Y;
            Assert.Equal(FlipMode.Y, i.SourceFlip);
        }

        [Fact]
        public void ToQueryString_ProducesValidOutput() {
            var i = new Instructions();
            i.Width = 100;
            i.Height = 200;
            i.Format = "png";
            string qs = i.ToQueryString();
            Assert.Contains("width=100", qs);
            Assert.Contains("height=200", qs);
            Assert.Contains("format=png", qs);
        }

        [Fact]
        public void NvcConstructor_CopiesValues() {
            var nvc = new NameValueCollection();
            nvc["width"] = "100";
            nvc["format"] = "jpg";
            var i = new Instructions(nvc);
            Assert.Equal(100, i.Width);
            Assert.Equal("jpg", i.Format);
        }

        [Fact]
        public void Margin_SetGet() {
            var i = new Instructions("margin=10");
            Assert.NotNull(i.Margin);
        }

        [Fact]
        public void Padding_SetGet() {
            var i = new Instructions("paddingWidth=5");
            Assert.NotNull(i.Padding);
        }

        [Fact]
        public void BorderWidth_SetGet() {
            var i = new Instructions("borderWidth=2");
            Assert.NotNull(i.Border);
        }

        [Fact]
        public void SemicolonSyntax_Works() {
            var i = new Instructions("width=100;height=200;format=png");
            Assert.Equal(100, i.Width);
            Assert.Equal(200, i.Height);
            Assert.Equal("png", i.Format);
        }

        [Fact]
        public void CropXUnits_CropYUnits() {
            var i = new Instructions("crop=10,10,90,90&cropxunits=100&cropyunits=100");
            Assert.NotNull(i.CropRectangle);
        }

        [Fact]
        public void Trim_Whitespace_Keys() {
            var i = new Instructions("width=100&height=200");
            Assert.Null(i["nonexistent"]);
        }

        [Fact]
        public void Process_SetGet() {
            var i = new Instructions("process=always");
            Assert.Equal("always", i["process"]);
        }
    }
}
