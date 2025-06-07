using System;
using System.Linq;
using ImageResizer.Util;
using Imageflow.Fluent;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Optimizes watermark settings by extracting parameters that can be handled by WatermarkOptions
    /// </summary>
    internal class WatermarkSettingsExtractor
    {
        /// <summary>
        /// Extracts parameters from preprocessing that can be handled by WatermarkOptions
        /// </summary>
        public (WatermarkOptions options, Instructions preprocessing) ExtractWatermarkSettings(
            WatermarkOptions baseOptions, 
            Instructions originalPreprocessing)
        {
            if (originalPreprocessing == null || originalPreprocessing.Count == 0)
                return (baseOptions, originalPreprocessing);

            var options = baseOptions ?? new WatermarkOptions();
            var preprocessing = new Instructions(originalPreprocessing);

            // Extract opacity/alpha
            if (preprocessing["alpha"] != null)
            {
                options.Opacity = ParseAlpha(preprocessing["alpha"]);
                preprocessing.Remove("alpha");
            }

            // Extract scale mode that affects constraint mode
            // scale=both should always set FitMode to Fit, overriding any previous value
            if (preprocessing["scale"] == "both")
            {
                options.FitMode = WatermarkConstraintMode.Fit;
            }
            preprocessing.Remove("scale");

            // Extract all FastScaling parameters that can be mapped to ResampleHints
            options.Hints = ExtractResampleHints(preprocessing);

            // Ignore format parameter
            preprocessing.Remove("format");
            preprocessing.Remove("quality");

            

            return (options, preprocessing);
        }

        /// <summary>
        /// Determines which preprocessing operations are actually required
        /// </summary>
        public Instructions GetRequiredPreprocessing(Instructions preprocessing)
        {
            if (preprocessing == null || preprocessing.Count == 0)
                return null;

            var required = new Instructions();

            foreach (string key in preprocessing.AllKeys)
            {
                var value = preprocessing[key];
                
                // Skip parameters that don't require preprocessing
                if (CanHandleWithoutPreprocessing(key, value))
                    continue;

                required[key] = value;
            }

            return required.Count > 0 ? required : null;
        }

        /// <summary>
        /// Gets default resample hints for ImageResizer 4 compatibility
        /// </summary>
        public ResampleHints GetDefaultResampleHints()
        {
            return new ResampleHints
            {
                DownFilter = InterpolationFilter.Mitchell,
                SharpenPercent = 15,
                InterpolationColorspace = ScalingFloatspace.Srgb,
                ResampleWhen = ResampleWhen.Size_Differs_Or_Sharpening_Requested,
                SharpenWhen = SharpenWhen.Downscaling
            };
        }

        private bool HasResampleParameters(Instructions settings)
        {
            var resampleKeys = new[] {
                "quality", "down.filter", "up.filter", "f.sharpen", 
                "down.colorspace", "up.colorspace", "down.preserve",
                "down.speed", "up.speed", "fastscale",
                "f.ignorealpha"
            };
            
            return resampleKeys.Any(key => settings[key] != null);
        }

        internal ResampleHints ExtractResampleHints(Instructions preprocessing)
        {
            var hints = GetDefaultResampleHints();


            // Extract filter settings
            if (preprocessing["down.filter"] != null)
            {
                hints.DownFilter = ParseInterpolationFilter(preprocessing["down.filter"]);
                preprocessing.Remove("down.filter");
            }
            if (preprocessing["up.filter"] != null)
            {
                hints.UpFilter = ParseInterpolationFilter(preprocessing["up.filter"]);
                preprocessing.Remove("up.filter");
            }

            // Extract sharpening
            if (preprocessing["f.sharpen"] != null)
            {
                if (float.TryParse(preprocessing["f.sharpen"], out float sharpen))
                {
                    hints.SharpenPercent = sharpen;
                }
                preprocessing.Remove("f.sharpen");
            }

            // Extract colorspace settings
            if (preprocessing["down.colorspace"] != null)
            {
                var colorspace = preprocessing["down.colorspace"].ToLowerInvariant();
                switch (colorspace)
                {
                    case "linear":
                        hints.InterpolationColorspace = ScalingFloatspace.Linear;
                        break;
                    case "srgb":
                        hints.InterpolationColorspace = ScalingFloatspace.Srgb;
                        break;
                    // Note: "gamma" no longer supported, skip
                }
                preprocessing.Remove("down.colorspace");
                
            }

            // Extract preserve setting (affects colorspace)
            if (preprocessing["down.preserve"] != null)
            {
                // down.preserve affects the colorspace calculation
                // For now, we'll map extreme values to linear/srgb
                if (float.TryParse(preprocessing["down.preserve"], out float preserve))
                {
                    if (preserve >= -1 && preserve <= 1)
                    {
                        hints.InterpolationColorspace = ScalingFloatspace.Linear;
                    }
                    // else keep sRGB for shadow preservation
                }
                preprocessing.Remove("down.preserve");
            }

            // Drop speed settings
            preprocessing.Remove("down.speed");
            preprocessing.Remove("fastscale");
            

            return hints;
        }

        private InterpolationFilter? ParseInterpolationFilter(string filterName)
        {
            switch (filterName?.ToLowerInvariant())
            {
                case "robidoux":
                    return InterpolationFilter.Robidoux;
                case "robidouxsharp":
                    return InterpolationFilter.Robidoux_Sharp;
                case "robidouxfast":
                    return InterpolationFilter.Robidoux_Fast;
                case "ginseng":
                    return InterpolationFilter.Ginseng;
                case "ginsengsharp":
                    return InterpolationFilter.Ginseng_Sharp;
                case "lanczos":
                    return InterpolationFilter.Lanczos;
                case "lanczossharp":
                    return InterpolationFilter.Lanczos_Sharp;
                case "lanczos2":
                    return InterpolationFilter.Lanczos_2;
                case "lanczos2sharp":
                    return InterpolationFilter.Lanczos_2_Sharp;
                case "cubicfast":
                    return InterpolationFilter.Fastest;  // No CubicFast, use Fastest
                case "cubic":
                    return InterpolationFilter.Cubic;
                case "cubicsharp":
                    return InterpolationFilter.Cubic_Sharp;
                case "catmullrom":
                    return InterpolationFilter.Catmull_Rom;
                case "mitchell":
                    return InterpolationFilter.Mitchell;
                case "cubicbspline":
                    return InterpolationFilter.Cubic_B_Spline;
                case "hermite":
                    return InterpolationFilter.Hermite;
                case "jinc":
                    return InterpolationFilter.Jinc;
                case "triangle":
                    return InterpolationFilter.Triangle;
                case "linear":
                    return InterpolationFilter.Linear;
                case "box":
                    return InterpolationFilter.Box;
                case "ncubic":
                    return InterpolationFilter.N_Cubic;
                case "ncubicsharp":
                    return InterpolationFilter.N_Cubic_Sharp;
                case "fastest":
                    return InterpolationFilter.Fastest;
                default:
                    return null;
            }
        }

   

        private bool CanHandleWithoutPreprocessing(string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                // These are all handled by ResampleHints or WatermarkOptions
                case "alpha":
                case "quality":
                case "format":
                case "scale":
                case "cache":
                case "scache":
                case "process":
                case "down.filter":
                case "up.filter":
                case "f.sharpen":
                case "down.colorspace":
                case "up.colorspace":
                case "down.preserve":
                case "down.speed":
                case "up.speed":
                case "fastscale":
                    return true;
                    
                // Size constraints that Imageflow can handle natively
                case "width":
                case "height":
                case "maxwidth":
                case "maxheight":
                    // Only if no other complex operations
                    return true;
                    
                default:
                    return false;
            }
        }

        private bool IsFormatSupportedNatively(string format)
        {
            switch (format?.ToLowerInvariant())
            {
                case "jpg":
                case "jpeg":
                case "png":
                case "webp":
                    return true;
                default:
                    return false;
            }
        }

        private float ParseAlpha(string alphaStr)
        {
            if (float.TryParse(alphaStr, out float f))
            {
                if (f <= 1.0f) return f;      // Already in 0-1 range
                if (f <= 100.0f) return f / 100.0f;  // Percentage
                if (f <= 255.0f) return f / 255.0f;  // Byte value
            }
            return 1.0f; // Default to fully opaque
        }
    }
} 