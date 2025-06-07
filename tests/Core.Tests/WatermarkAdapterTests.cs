using System;
using System.Linq;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;
using ImageResizer.Plugins.Imageflow.Watermarks;
using Imageflow.Fluent;
using Imazen.Common.Issues;
using Xunit;

namespace ImageResizer.Tests
{
    public class WatermarkAdapterTests
    {
        private Config CreateConfigWithXml(string watermarksXml)
        {
            var xml = $@"<resizer>{watermarksXml}</resizer>";
            var config = new Config(new ResizerSection(xml));
            return config;
        }

        [Fact]
        public void ParseSimpleImageWatermark()
        {
            var xml = @"
                <watermarks>
                    <image name=""logo"" 
                           path=""~/watermarks/logo.png"" 
                           right=""10px"" 
                           bottom=""10px"" 
                           width=""100px"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "logo" });
            
            Assert.Single(options);
            var (watermarkOptions, path, preprocessing, cacheKey) = options[0];
            
            Assert.Equal("~/watermarks/logo.png", path);
            Assert.NotNull(watermarkOptions.FitBox);
            Assert.IsType<WatermarkMargins>(watermarkOptions.FitBox);
            
            var margins = (WatermarkMargins)watermarkOptions.FitBox;
            Assert.Equal(10u, margins.Right);
            Assert.Equal(10u, margins.Bottom);
            Assert.Equal(0u, margins.Left);
            Assert.Equal(0u, margins.Top);
            Assert.Equal(WatermarkAlign.Image, margins.RelativeTo);
        }

        [Fact]
        public void ParsePercentageBasedWatermark()
        {
            var xml = @"
                <watermarks>
                    <image name=""centered"" 
                           path=""~/watermark.png"" 
                           left=""25%"" 
                           top=""25%"" 
                           right=""25%"" 
                           bottom=""25%"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "centered" });
            
            Assert.Single(options);
            var (watermarkOptions, _, _, cacheKey) = options[0];
            
            Assert.IsType<WatermarkFitBox>(watermarkOptions.FitBox);
            var fitBox = (WatermarkFitBox)watermarkOptions.FitBox;
            
            Assert.Equal(25f, fitBox.X1);
            Assert.Equal(25f, fitBox.Y1);
            Assert.Equal(75f, fitBox.X2); // 100 - 25
            Assert.Equal(75f, fitBox.Y2); // 100 - 25
        }

        [Fact]
        public void ParseWatermarkWithOpacity()
        {
            var xml = @"
                <watermarks>
                    <image name=""transparent"" 
                           path=""~/logo.png"" 
                           imageQuery=""alpha=0.5&amp;width=200"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "transparent" });
            
            Assert.Single(options);
            var (watermarkOptions, _, preprocessing, cacheKey) = options[0];
            
            Assert.Equal(0.5f, watermarkOptions.Opacity);
            Assert.NotNull(preprocessing);
            Assert.Equal("200", preprocessing["width"]);
            Assert.Null(preprocessing["alpha"]); // Should be removed after extraction
        }

        [Fact]
        public void ParseWatermarkGroup()
        {
            var xml = @"
                <watermarks>
                    <group name=""composite"">
                        <image path=""~/logo1.png"" left=""10px"" top=""10px"" />
                        <image path=""~/logo2.png"" right=""10px"" bottom=""10px"" />
                    </group>
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "composite" });
            
            Assert.Equal(2, options.Count);
            Assert.Equal("~/logo1.png", options[0].path);
            Assert.Equal("~/logo2.png", options[1].path);
        }

        [Fact]
        public void ParseOtherImagesConfiguration()
        {
            var xml = @"
                <watermarks>
                    <otherimages path=""~/watermarks"" 
                                 right=""20px"" 
                                 bottom=""20px"" 
                                 width=""50px"" 
                                 height=""50px"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            // Test legacy file-based watermark
            var options = adapter.GetWatermarkOptions(new[] { "legacy.png" });
            
            Assert.Single(options);
            var (watermarkOptions, path, _, cacheKey) = options[0];
            
            Assert.Equal("~/watermarks/legacy.png", path);
            Assert.IsType<WatermarkMargins>(watermarkOptions.FitBox);
        }

        [Fact]
        public void TextLayerGeneratesError()
        {
            var xml = @"
                <watermarks>
                    <text name=""copyright"" 
                          text=""© 2024 Company"" 
                          font=""Arial"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Text watermarks are not supported") && 
                i.Severity == IssueSeverity.Error);
            
            // Should return empty options for text watermark
            var options = adapter.GetWatermarkOptions(new[] { "copyright" });
            Assert.Empty(options);
        }

        [Fact]
        public void BackgroundLayerGeneratesError()
        {
            var xml = @"
                <watermarks>
                    <image name=""bg"" 
                           path=""~/bg.png"" 
                           drawAs=""background"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Background watermarks are not supported") && 
                i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void InvalidRelativeToGeneratesError()
        {
            var xml = @"
                <watermarks>
                    <image name=""invalid"" 
                           path=""~/logo.png"" 
                           relativeTo=""padding"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("RelativeTo value 'padding' is not supported") && 
                i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void MixedUnitsGeneratesWarning()
        {
            var xml = @"
                <watermarks>
                    <image name=""mixed"" 
                           path=""~/logo.png"" 
                           left=""10px"" 
                           top=""20%"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Mixing pixels and percentages") && 
                i.Severity == IssueSeverity.Warning);
        }

        [Fact]
        public void ComplexPositioningGeneratesWarning()
        {
            var xml = @"
                <watermarks>
                    <image name=""complex"" 
                           path=""~/logo.png"" 
                           left=""10px"" 
                           right=""10px"" 
                           width=""100px"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Complex positioning detected") && 
                i.Severity == IssueSeverity.Warning);
        }

        [Fact]
        public void DuplicateNameGeneratesError()
        {
            var xml = @"
                <watermarks>
                    <image name=""logo"" path=""~/logo1.png"" />
                    <image name=""logo"" path=""~/logo2.png"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Duplicate watermark name") && 
                i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void MissingNameGeneratesError()
        {
            var xml = @"
                <watermarks>
                    <image path=""~/logo.png"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var issues = adapter.GetIssues().ToList();
            
            Assert.Contains(issues, i => 
                i.Summary.Contains("Missing name attribute") && 
                i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void DefaultImageQueryIsApplied()
        {
            var xml = @"
                <watermarks defaultImageQuery=""scache=true&amp;bgcolor=white"">
                    <image name=""logo"" path=""~/logo.jpg"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "logo" });
            
            Assert.Single(options);
            var (_, _, preprocessing, cacheKey) = options[0];
            
            Assert.Equal("true", preprocessing["scache"]);
            Assert.Equal("white", preprocessing["bgcolor"]);
        }

        [Fact]
        public void ContentAlignmentMapping()
        {
            var xml = @"
                <watermarks>
                    <image name=""tl"" path=""~/logo.png"" align=""TopLeft"" />
                    <image name=""br"" path=""~/logo.png"" align=""BottomRight"" />
                    <image name=""c"" path=""~/logo.png"" align=""MiddleCenter"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var tlOptions = adapter.GetWatermarkOptions(new[] { "tl" })[0].options;
            var brOptions = adapter.GetWatermarkOptions(new[] { "br" })[0].options;
            var cOptions = adapter.GetWatermarkOptions(new[] { "c" })[0].options;
            
            // TopLeft should map to {x:0, y:0}
            Assert.NotNull(tlOptions.Gravity);
            Assert.Equal(0f, tlOptions.Gravity.XPercent);
            Assert.Equal(0f, tlOptions.Gravity.YPercent);
            
            // BottomRight should map to {x:100, y:100}
            Assert.NotNull(brOptions.Gravity);
            Assert.Equal(100f, brOptions.Gravity.XPercent);
            Assert.Equal(100f, brOptions.Gravity.YPercent);
            
            // MiddleCenter should be {x:50, y:50}
            Assert.NotNull(cOptions.Gravity);
            Assert.Equal(50f, cOptions.Gravity.XPercent);
            Assert.Equal(50f, cOptions.Gravity.YPercent);
        }

        [Fact]
        public void FillModeMapping()
        {
            var xml = @"
                <watermarks>
                    <image name=""fill"" path=""~/logo.png"" fill=""true"" />
                    <image name=""sized"" path=""~/logo.png"" width=""100px"" />
                    <image name=""default"" path=""~/logo.png"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var fillOptions = adapter.GetWatermarkOptions(new[] { "fill" })[0].options;
            var sizedOptions = adapter.GetWatermarkOptions(new[] { "sized" })[0].options;
            var defaultOptions = adapter.GetWatermarkOptions(new[] { "default" })[0].options;
            
            Assert.Equal(WatermarkConstraintMode.Fit, fillOptions.FitMode);
            Assert.Equal(WatermarkConstraintMode.Within, sizedOptions.FitMode);
            Assert.Equal(WatermarkConstraintMode.Within, defaultOptions.FitMode);
        }

        [Fact]
        public void ResampleHintsAreSet()
        {
            var xml = @"
                <watermarks>
                    <image name=""logo"" path=""~/logo.png"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var options = adapter.GetWatermarkOptions(new[] { "logo" })[0].options;
            
            Assert.NotNull(options.Hints);
            Assert.Equal(InterpolationFilter.Mitchell, options.Hints.DownFilter);
            Assert.Equal(15f, options.Hints.SharpenPercent);
            Assert.Equal(ScalingFloatspace.Srgb, options.Hints.InterpolationColorspace);
            Assert.Equal(ResampleWhen.Size_Differs_Or_Sharpening_Requested, options.Hints.ResampleWhen);
            Assert.Equal(SharpenWhen.Downscaling, options.Hints.SharpenWhen);
        }

        [Fact]
        public void CanvasRelativeToMapping()
        {
            var xml = @"
                <watermarks>
                    <image name=""canvas"" path=""~/logo.png"" relativeTo=""canvas"" />
                    <image name=""image"" path=""~/logo.png"" relativeTo=""image"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var canvasOptions = adapter.GetWatermarkOptions(new[] { "canvas" })[0].options;
            var imageOptions = adapter.GetWatermarkOptions(new[] { "image" })[0].options;
            
            var canvasMargins = (WatermarkMargins)canvasOptions.FitBox;
            var imageMargins = (WatermarkMargins)imageOptions.FitBox;
            
            Assert.Equal(WatermarkAlign.Canvas, canvasMargins.RelativeTo);
            Assert.Equal(WatermarkAlign.Image, imageMargins.RelativeTo);
        }

        [Fact]
        public void FillPropertySetsConstraintMode()
        {
            var xml = @"
                <watermarks>
                    <image name=""withFill"" path=""~/logo.png"" fill=""true"" right=""10px"" bottom=""10px"" />
                    <image name=""withoutFill"" path=""~/logo.png"" fill=""false"" right=""10px"" bottom=""10px"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var withFillOptions = adapter.GetWatermarkOptions(new[] { "withFill" })[0].options;
            var withoutFillOptions = adapter.GetWatermarkOptions(new[] { "withoutFill" })[0].options;
            
            Assert.Equal(WatermarkConstraintMode.Fit, withFillOptions.FitMode.Value);
            Assert.Equal(WatermarkConstraintMode.Within, withoutFillOptions.FitMode.Value);
        }

        [Fact]
        public void PreprocessingOptimization()
        {
            var xml = @"
                <watermarks>
                    <image name=""optimized"" 
                           path=""~/logo.png"" 
                           imageQuery=""alpha=0.5&amp;quality=85&amp;scale=both&amp;format=png&amp;width=100"" 
                           right=""10px"" 
                           bottom=""10px"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            
            var watermarkConfigs = adapter.GetWatermarkOptions(new[] { "optimized" });
            var (options, path, preprocessing, cacheKey) = watermarkConfigs[0];
            
            // Verify that alpha, quality, scale, and format were extracted
            Assert.Equal(0.5f, options.Opacity.Value, 2);
            Assert.NotNull(options.Hints);
            Assert.Equal(WatermarkConstraintMode.Fit, options.FitMode.Value);
            
            // Verify that preprocessing only contains width now
            Assert.Contains("width", preprocessing.AllKeys);
            Assert.DoesNotContain("alpha", preprocessing.AllKeys);
            Assert.DoesNotContain("quality", preprocessing.AllKeys);
            Assert.DoesNotContain("scale", preprocessing.AllKeys);
            Assert.DoesNotContain("format", preprocessing.AllKeys);
        }

        [Fact]
        public void LegacyAlphaSupport()
        {
            var xml = @"
                <watermarks>
                    <image name=""alpha1"" path=""~/logo.png"" imageQuery=""alpha=0.5"" />
                    <image name=""alpha255"" path=""~/logo.png"" imageQuery=""alpha=128"" />
                </watermarks>";

            var config = CreateConfigWithXml(xml);
            var adapter = new WatermarkAdapter(config);
            
            var alpha1Options = adapter.GetWatermarkOptions(new[] { "alpha1" })[0].options;
            var alpha255Options = adapter.GetWatermarkOptions(new[] { "alpha255" })[0].options;
            
            // 0-1 range
            Assert.Equal(0.5f, alpha1Options.Opacity.Value, 2);
            // 0-255 range (128/255 ≈ 0.502)
            Assert.Equal(0.502f, alpha255Options.Opacity.Value, 3);
        }
    }
} 