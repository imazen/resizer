using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;
using ImageResizer.ExtensionMethods;
using ImageResizer.Util;
using Imageflow.Fluent;
using Imazen.Common.Issues;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Adapts legacy watermark configuration to Imageflow's watermark API
    /// </summary>
    internal class WatermarkAdapter
    {
        private readonly Config config;
        private readonly WatermarkConfigurationParser parser;
        private readonly PositionConverter positionConverter;

        private readonly Dictionary<string, WatermarkConfiguration> configurations;
        private readonly List<IIssue> issues;

        public WatermarkAdapter(Config config)
        {
            this.config = config;
            this.parser = new WatermarkConfigurationParser(config);
            this.positionConverter = new PositionConverter();
            this.configurations = new Dictionary<string, WatermarkConfiguration>(StringComparer.OrdinalIgnoreCase);
            this.issues = new List<IIssue>();

            // Parse watermark configuration
            var watermarksNode = config.getConfigXml().queryFirst("watermarks");
            if (watermarksNode != null)
            {
                configurations = parser.ParseWatermarks(watermarksNode);
                issues.AddRange(parser.GetIssues());
            }

        }
        /// <summary>
        /// Gets all configuration issues
        /// </summary>
        public IEnumerable<IIssue> GetIssues() => issues;

        /// <summary>
        /// Gets watermark options for the specified watermark names
        /// </summary>
        public List<(WatermarkOptions options, string path, Instructions preprocessing, string cacheKey)> GetWatermarkOptions(string[] watermarkNames)
        {
            var results = new List<(WatermarkOptions options, string path, Instructions preprocessing, string cacheKey)>();

            foreach (var name in watermarkNames)
            {
                if (configurations.ContainsKey(name))
                {
                    // Named watermark
                    var config = configurations[name];
                    foreach (var layer in config.Layers)
                    {
                        if (!layer.IsTextLayer)
                        {
                            results.Add((layer.Options, layer.Path, layer.PreprocessingQuery, config.CacheKey));
                        }
                    }
                }
            }

            return results;
        }

        public string[] ParseWatermarkNames(string watermarkStr)
        {
            if (string.IsNullOrEmpty(watermarkStr))
                return Array.Empty<string>();

            return watermarkStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrEmpty(s))
                             .ToArray();
        }

        /// <summary>
        /// Gets default resample hints for ImageResizer 4 compatibility
        /// </summary>
        private ResampleHints GetDefaultResampleHints()
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

        /// <summary>
        /// Determines the fit mode based on layer configuration
        /// </summary>
        private WatermarkConstraintMode GetFitMode(LegacyLayerInfo layer)
        {
            if (layer.Fill) return WatermarkConstraintMode.Fit;
            if (layer.Width != null || layer.Height != null) return WatermarkConstraintMode.Within;
            return WatermarkConstraintMode.Within;
        }

      
    }
}