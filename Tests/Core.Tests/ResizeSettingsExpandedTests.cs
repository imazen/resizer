using System;
using System.Collections.Specialized;
using System.Drawing;
using Xunit;
using ImageResizer;
using ImageResizer.Resizing;

namespace ImageResizer.Core.Tests {
    public class ResizeSettingsExpandedTests {

        // --- Constructor tests ---

        [Fact]
        public void EmptyConstructor_DefaultValues() {
            var s = new ResizeSettings();
            Assert.Equal(-1, s.Width);
            Assert.Equal(-1, s.Height);
            Assert.Equal(-1, s.MaxWidth);
            Assert.Equal(-1, s.MaxHeight);
            Assert.Equal(90, s.Quality);
            Assert.Equal(FitMode.None, s.Mode);
            Assert.Equal(ScaleMode.DownscaleOnly, s.Scale);
        }

        [Fact]
        public void TypedConstructor_SetsValues() {
            var s = new ResizeSettings(100, 200, FitMode.Crop, "png");
            Assert.Equal(100, s.Width);
            Assert.Equal(200, s.Height);
            Assert.Equal(FitMode.Crop, s.Mode);
            Assert.Equal("png", s.Format);
        }

        [Fact]
        public void TypedConstructor_NullFormat() {
            var s = new ResizeSettings(100, 200, FitMode.Max, null);
            Assert.Null(s.Format);
        }

        [Fact]
        public void NvcConstructor_CopiesValues() {
            var nvc = new NameValueCollection();
            nvc["width"] = "400";
            nvc["height"] = "300";
            var s = new ResizeSettings(nvc);
            Assert.Equal(400, s.Width);
            Assert.Equal(300, s.Height);
        }

        // --- Width/Height with aliases ---

        [Fact]
        public void Width_ReadsW_Alias() {
            var s = new ResizeSettings("w=150");
            Assert.Equal(150, s.Width);
        }

        [Fact]
        public void Height_ReadsH_Alias() {
            var s = new ResizeSettings("h=250");
            Assert.Equal(250, s.Height);
        }

        [Fact]
        public void Width_Set_RemovesWAlias() {
            var s = new ResizeSettings("w=150");
            s.Width = 200;
            Assert.Equal(200, s.Width);
            Assert.Null(s["w"]);
        }

        // --- Mode ---

        [Theory]
        [InlineData("mode=max", FitMode.Max)]
        [InlineData("mode=pad", FitMode.Pad)]
        [InlineData("mode=crop", FitMode.Crop)]
        [InlineData("mode=carve", FitMode.Carve)]
        [InlineData("mode=stretch", FitMode.Stretch)]
        public void Mode_ParsesAllFitModes(string qs, FitMode expected) {
            var s = new ResizeSettings(qs);
            Assert.Equal(expected, s.Mode);
        }

        // --- Rotate ---

        [Theory]
        [InlineData("rotate=0", 0.0)]
        [InlineData("rotate=90", 90.0)]
        [InlineData("rotate=-90", -90.0)]
        [InlineData("rotate=45.5", 45.5)]
        public void Rotate_ParsesValues(string qs, double expected) {
            var s = new ResizeSettings(qs);
            Assert.Equal(expected, s.Rotate);
        }

        // --- Anchor ---

        [Theory]
        [InlineData("anchor=topleft", ContentAlignment.TopLeft)]
        [InlineData("anchor=middlecenter", ContentAlignment.MiddleCenter)]
        [InlineData("anchor=bottomright", ContentAlignment.BottomRight)]
        public void Anchor_ParsesValues(string qs, ContentAlignment expected) {
            var s = new ResizeSettings(qs);
            Assert.Equal(expected, s.Anchor);
        }

        // --- Quality ---

        [Theory]
        [InlineData("quality=10", 10)]
        [InlineData("quality=100", 100)]
        [InlineData("quality=50", 50)]
        public void Quality_ParsesValues(string qs, int expected) {
            var s = new ResizeSettings(qs);
            Assert.Equal(expected, s.Quality);
        }

        [Fact]
        public void Quality_DefaultIs90() {
            var s = new ResizeSettings();
            Assert.Equal(90, s.Quality);
        }

        // --- Format ---

        [Fact]
        public void Format_SetGet() {
            var s = new ResizeSettings();
            s.Format = "png";
            Assert.Equal("png", s.Format);
        }

        [Fact]
        public void Format_FallsBackToThumbnail() {
            var s = new ResizeSettings("thumbnail=gif");
            Assert.Equal("gif", s.Format);
        }

        [Fact]
        public void Format_Set_RemovesThumbnail() {
            var s = new ResizeSettings("thumbnail=gif");
            s.Format = "png";
            Assert.Equal("png", s.Format);
            Assert.Null(s["thumbnail"]);
        }

        // --- Scale ---

        [Theory]
        [InlineData("scale=downscaleonly", ScaleMode.DownscaleOnly)]
        [InlineData("scale=both", ScaleMode.Both)]
        [InlineData("scale=upscaleonly", ScaleMode.UpscaleOnly)]
        [InlineData("scale=upscalecanvas", ScaleMode.UpscaleCanvas)]
        public void Scale_ParsesAllValues(string qs, ScaleMode expected) {
            var s = new ResizeSettings(qs);
            Assert.Equal(expected, s.Scale);
        }

        // --- Flip / SourceFlip ---

        [Fact]
        public void Flip_SetGet() {
            var s = new ResizeSettings();
            s.Flip = RotateFlipType.RotateNoneFlipX;
            Assert.Equal(RotateFlipType.RotateNoneFlipX, s.Flip);
        }

        [Fact]
        public void SourceFlip_SetGet() {
            var s = new ResizeSettings();
            s.SourceFlip = RotateFlipType.RotateNoneFlipY;
            Assert.Equal(RotateFlipType.RotateNoneFlipY, s.SourceFlip);
        }

        [Fact]
        public void SourceFlip_ReadsSourceFlipAlias() {
            var s = new ResizeSettings("sourceFlip=y");
            Assert.Equal(RotateFlipType.RotateNoneFlipY, s.SourceFlip);
        }

        // --- Colors ---

        [Fact]
        public void BackgroundColor_SetGet() {
            var s = new ResizeSettings();
            s.BackgroundColor = Color.Red;
            Assert.Equal(Color.Red.ToArgb(), s.BackgroundColor.ToArgb());
        }

        [Fact]
        public void BorderColor_SetGet() {
            var s = new ResizeSettings();
            s.BorderColor = Color.Blue;
            Assert.Equal(Color.Blue.ToArgb(), s.BorderColor.ToArgb());
        }

        [Fact]
        public void PaddingColor_SetGet() {
            var s = new ResizeSettings();
            s.PaddingColor = Color.Green;
            Assert.Equal(Color.Green.ToArgb(), s.PaddingColor.ToArgb());
        }

        // --- Crop coordinates ---

        [Fact]
        public void CropTopLeft_CropBottomRight_Roundtrip() {
            var s = new ResizeSettings();
            s.CropTopLeft = new PointF(10, 20);
            s.CropBottomRight = new PointF(90, 80);
            Assert.Equal(10f, s.CropTopLeft.X);
            Assert.Equal(20f, s.CropTopLeft.Y);
            Assert.Equal(90f, s.CropBottomRight.X);
            Assert.Equal(80f, s.CropBottomRight.Y);
        }

        [Fact]
        public void CropValues_FromQuerystring() {
            var s = new ResizeSettings("crop=10,20,90,80");
            Assert.Equal(10f, s.CropTopLeft.X);
            Assert.Equal(20f, s.CropTopLeft.Y);
            Assert.Equal(90f, s.CropBottomRight.X);
            Assert.Equal(80f, s.CropBottomRight.Y);
        }

        [Fact]
        public void CropXUnits_CropYUnits_SetGet() {
            var s = new ResizeSettings();
            s.CropXUnits = 100;
            s.CropYUnits = 100;
            Assert.Equal(100, s.CropXUnits);
            Assert.Equal(100, s.CropYUnits);
        }

        // --- Padding/Margin/Border ---

        [Fact]
        public void Padding_FromQuerystring() {
            var s = new ResizeSettings("paddingWidth=10");
            Assert.NotNull(s.Padding);
        }

        [Fact]
        public void Border_FromQuerystring() {
            var s = new ResizeSettings("borderWidth=2");
            Assert.NotNull(s.Border);
        }

        [Fact]
        public void Margin_FromQuerystring() {
            var s = new ResizeSettings("margin=5");
            Assert.NotNull(s.Margin);
        }

        // --- ToString ---

        [Fact]
        public void ToString_ProducesQueryString() {
            var s = new ResizeSettings(100, 200, FitMode.Crop, "png");
            string result = s.ToString();
            Assert.Contains("width=100", result);
            Assert.Contains("height=200", result);
            Assert.Contains("mode=crop", result.ToLowerInvariant());
            Assert.Contains("format=png", result);
        }

        [Fact]
        public void ToStringEncoded_UrlEncodes() {
            var s = new ResizeSettings();
            s["custom"] = "value with spaces";
            string result = s.ToStringEncoded();
            Assert.Contains("value+with+spaces", result);
        }

        // --- Normalize ---

        [Fact]
        public void Normalize_WToWidth() {
            var s = new ResizeSettings("w=100&h=200");
            s.Normalize();
            Assert.Equal("100", s["width"]);
            Assert.Equal("200", s["height"]);
            Assert.Null(s["w"]);
            Assert.Null(s["h"]);
        }

        [Fact]
        public void Normalize_WidthTakesPriorityOverW() {
            var s = new ResizeSettings("width=150&w=100");
            s.Normalize();
            Assert.Equal("150", s["width"]);
            Assert.Null(s["w"]);
        }

        [Fact]
        public void Normalize_ThumbnailToFormat() {
            var s = new ResizeSettings("thumbnail=gif");
            s.Normalize();
            Assert.Equal("gif", s["format"]);
            Assert.Null(s["thumbnail"]);
        }

        [Fact]
        public void Normalize_SourceFlipToSFlip() {
            var s = new ResizeSettings("sourceFlip=x");
            s.Normalize();
            Assert.Equal("x", s["sFlip"]);
            Assert.Null(s["sourceFlip"]);
        }

        // --- WasOneSpecified ---

        [Fact]
        public void WasOneSpecified_ReturnsTrueWhenPresent() {
            var s = new ResizeSettings("width=100");
            Assert.True(s.WasOneSpecified("width", "height"));
        }

        [Fact]
        public void WasOneSpecified_ReturnsFalseWhenMissing() {
            var s = new ResizeSettings();
            Assert.False(s.WasOneSpecified("width", "height"));
        }

        // --- SetDefaultImageFormat ---

        [Fact]
        public void SetDefaultImageFormat_SetsWhenEmpty() {
            var s = new ResizeSettings();
            s.SetDefaultImageFormat("jpg");
            Assert.Equal("jpg", s.Format);
        }

        [Fact]
        public void SetDefaultImageFormat_DoesNotOverwrite() {
            var s = new ResizeSettings("format=png");
            s.SetDefaultImageFormat("jpg");
            Assert.Equal("png", s.Format);
        }

        // --- Cache / Process ---

        [Fact]
        public void Cache_SetGet() {
            var s = new ResizeSettings();
            s.Cache = ServerCacheMode.Always;
            Assert.Equal(ServerCacheMode.Always, s.Cache);
        }

        [Fact]
        public void Process_SetGet() {
            var s = new ResizeSettings();
            s.Process = ProcessWhen.Always;
            Assert.Equal(ProcessWhen.Always, s.Process);
        }
    }
}
