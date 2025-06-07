using System;
using System.Collections.Generic;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;
using ImageResizer.Util;
using Imageflow.Fluent;
using Imazen.Common.Issues;
using System.Linq;
using ImageResizer;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Parses watermark configuration from XML
    /// </summary>
    internal class WatermarkConfigurationParser
    {
        private readonly Config config;
        private readonly List<IIssue> issues = new List<IIssue>();
        private Instructions defaultImageQuery = new Instructions("scache=true");
        private WatermarkLayerConfig otherImagesConfig;

        private WatermarkSettingsExtractor settingsExtractor;

        public WatermarkConfigurationParser(Config config)
        {
            this.config = config;
            this.settingsExtractor = new WatermarkSettingsExtractor();
        }

        public IEnumerable<IIssue> GetIssues() => issues;
        public WatermarkLayerConfig GetOtherImagesConfig() => otherImagesConfig;

        public Dictionary<string, WatermarkConfiguration> ParseWatermarks(Node watermarksNode)
        {
            var watermarks = new Dictionary<string, WatermarkConfiguration>(StringComparer.OrdinalIgnoreCase);
            
            if (watermarksNode == null) return watermarks;

            // Parse default image query
            if (!string.IsNullOrEmpty(watermarksNode.Attrs["defaultImageQuery"]))
            {
                defaultImageQuery = new Instructions(watermarksNode.Attrs["defaultImageQuery"]);
            }

            // Parse child nodes
            foreach (var node in watermarksNode.Children ?? Enumerable.Empty<Node>())
            {
                try
                {
                    switch (node.Name.ToLowerInvariant())
                    {
                        case "image":
                            ParseImageLayer(node, watermarks);
                            break;
                        case "text":
                            ParseTextLayer(node);
                            break;
                        case "group":
                            ParseGroup(node, watermarks);
                            break;
                        case "otherimages":
                            ParseOtherImages(node);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AddIssue(IssueSeverity.Error, 
                        $"Failed to parse {node.Name}",
                        ex.Message,
                        node.ToString());
                }
            }

            return watermarks;
        }

        private void ParseImageLayer(Node node, Dictionary<string, WatermarkConfiguration> watermarks)
        {
            var name = node.Attrs["name"];
            if (string.IsNullOrEmpty(name))
            {
                AddIssue(IssueSeverity.Error,
                    "Missing name attribute",
                    "The name attribute for each watermark must be specified",
                    node.ToString());
                return;
            }

            if (watermarks.ContainsKey(name))
            {
                AddIssue(IssueSeverity.Error,
                    $"Duplicate watermark name: {name}",
                    "Watermark names must be unique",
                    node.ToString());
                return;
            }

            var layer = ParseLayerConfig(node, false);
            if (layer != null)
            {
                watermarks[name] = new WatermarkConfiguration
                {
                    Name = name,
                    Layers = new List<WatermarkLayerConfig> { layer }
                };
            }
        }

        private void ParseTextLayer(Node node)
        {
            AddIssue(IssueSeverity.Error,
                "Text watermarks are not supported in Imageflow",
                "Please convert text watermarks to image watermarks",
                node.ToString());
        }

        private void ParseGroup(Node node, Dictionary<string, WatermarkConfiguration> watermarks)
        {
            var name = node.Attrs["name"];
            if (string.IsNullOrEmpty(name))
            {
                AddIssue(IssueSeverity.Error,
                    "Missing name attribute",
                    "The name attribute for each watermark group must be specified",
                    node.ToString());
                return;
            }

            if (watermarks.ContainsKey(name))
            {
                AddIssue(IssueSeverity.Error,
                    $"Duplicate watermark name: {name}",
                    "Watermark names must be unique",
                    node.ToString());
                return;
            }

            var layers = new List<WatermarkLayerConfig>();
            foreach (var childNode in node.Children ?? Enumerable.Empty<Node>())
            {
                if (childNode.Name.Equals("image", StringComparison.OrdinalIgnoreCase))
                {
                    var layer = ParseLayerConfig(childNode, false);
                    if (layer != null) layers.Add(layer);
                }
                else if (childNode.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    ParseTextLayer(childNode);
                }
            }

            if (layers.Count > 0)
            {
                watermarks[name] = new WatermarkConfiguration
                {
                    Name = name,
                    Layers = layers
                };
            }
        }

        private void ParseOtherImages(Node node)
        {
            otherImagesConfig = ParseLayerConfig(node, true);
        }

        private WatermarkLayerConfig ParseLayerConfig(Node node, bool isOtherImages)
        {
            var attrs = new Instructions(node.Attrs);
            
            // Parse legacy layer info
            var legacyInfo = new LegacyLayerInfo
            {
                Left = DistanceUnit.TryParse(attrs["left"]),
                Top = DistanceUnit.TryParse(attrs["top"]),
                Right = DistanceUnit.TryParse(attrs["right"]),
                Bottom = DistanceUnit.TryParse(attrs["bottom"]),
                Width = DistanceUnit.TryParse(attrs["width"]),
                Height = DistanceUnit.TryParse(attrs["height"]),
                RelativeTo = attrs["relativeTo"] ?? "image",
                Align = attrs.Get<ContentAlignment>("align", ContentAlignment.MiddleCenter),
                Fill = attrs.Get<bool>("fill", false),
                DrawAs = attrs["drawAs"] ?? "overlay",
                ImageQuery = attrs["imageQuery"]
            };

            // Validate configuration
            ValidateLayerConfig(node, legacyInfo);

            // Parse path and query
            var path = attrs["path"];
            var imageQuery = CombineImageQueries(attrs["imageQuery"], path);
            
            // Convert position
            var positionConverter = new PositionConverter();
            var (fitBox, positionIssues) = positionConverter.ConvertPosition(legacyInfo);
            issues.AddRange(positionIssues);

            // Create watermark options
            var options = new WatermarkOptions
            {
                FitBox = fitBox,
                FitMode = GetFitMode(legacyInfo),
                Gravity = ConstraintGravityHelper.FromContentAlignment(legacyInfo.Align),
                Hints = settingsExtractor.ExtractResampleHints(imageQuery)
            };
            var (watermarkOptions, preprocessing) = settingsExtractor.ExtractWatermarkSettings(options, imageQuery);

            return new WatermarkLayerConfig
            {
                Path = isOtherImages ? path : PathUtils.RemoveQueryString(path),
                PreprocessingQuery = preprocessing,
                Options = watermarkOptions,
                IsTextLayer = false,
                LegacyInfo = legacyInfo
            };
        }

        private void ValidateLayerConfig(Node node, LegacyLayerInfo layer)
        {
            // Background layer check
            if (layer.DrawAs.Equals("background", StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(IssueSeverity.Error,
                    "Background watermarks are not supported in Imageflow",
                    $"Layer uses drawAs='background'",
                    node.ToString());
            }

            // RelativeTo validation
            var validRelativeTo = new[] { "image", "canvas" };
            if (!validRelativeTo.Contains(layer.RelativeTo, StringComparer.OrdinalIgnoreCase))
            {
                AddIssue(IssueSeverity.Error,
                    $"RelativeTo value '{layer.RelativeTo}' is not supported",
                    "Only 'image' and 'canvas' are supported in Imageflow",
                    node.ToString());
            }

            // Mixed units warning
            if (HasMixedUnits(layer))
            {
                AddIssue(IssueSeverity.Warning,
                    "Mixing pixels and percentages in positioning",
                    "This may produce unexpected results when mapping to Imageflow",
                    node.ToString());
            }

            // Complex positioning warning
            if (HasComplexPositioning(layer))
            {
                AddIssue(IssueSeverity.Warning,
                    "Complex positioning detected",
                    "Specifying all of left/right/width or top/bottom/height may not map precisely",
                    node.ToString());
            }
        }

        private bool HasMixedUnits(LegacyLayerInfo layer)
        {
            var positions = new[] { layer.Left, layer.Top, layer.Right, layer.Bottom };
            var hasPercentages = positions.Any(p => p?.Type == DistanceUnit.Units.Percentage);
            var hasPixels = positions.Any(p => p?.Type == DistanceUnit.Units.Pixels);
            return hasPercentages && hasPixels;
        }

        private bool HasComplexPositioning(LegacyLayerInfo layer)
        {
            bool hasHorizontalComplex = layer.Left != null && layer.Right != null && layer.Width != null;
            bool hasVerticalComplex = layer.Top != null && layer.Bottom != null && layer.Height != null;
            return hasHorizontalComplex || hasVerticalComplex;
        }

        private Instructions CombineImageQueries(string configImageQuery, string path)
        {
            var pathSettings = new Instructions(PathUtils.ParseQueryString(path));
            var imageQuerySettings = new Instructions(configImageQuery ?? string.Empty);
            var merged = new Instructions(defaultImageQuery);
            foreach (var key in pathSettings.AllKeys)
            {
                merged[key] = pathSettings[key];    
            }
            foreach (var key in imageQuerySettings.AllKeys)
            {
                merged[key] = imageQuerySettings[key];
            }
            return merged;
        }

        private WatermarkConstraintMode GetFitMode(LegacyLayerInfo layer)
        {
            if (layer.Fill) return WatermarkConstraintMode.Fit;
            if (layer.Width != null || layer.Height != null) return WatermarkConstraintMode.Within;
            return WatermarkConstraintMode.Within;
        }

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

        private void AddIssue(IssueSeverity severity, string summary, string details, string affectedNode)
        {
            issues.Add(new WatermarkConfigIssue
            {
                Severity = severity,
                Summary = summary,
                Details = details,
                AffectedNode = affectedNode
            });
        }
    }

    /// <summary>
    /// Converts legacy position information to Imageflow constraint boxes
    /// </summary>
    internal class PositionConverter
    {
        public (IWatermarkConstraintBox box, List<IIssue> issues) ConvertPosition(LegacyLayerInfo layer)
        {
            var issues = new List<IIssue>();
            
            // Determine if all values are pixels or if any are percentages
            var positions = new[] { layer.Left, layer.Top, layer.Right, layer.Bottom };
            var nonNullPositions = positions.Where(p => p != null).ToArray();
            
            if (nonNullPositions.Length == 0)
            {
                // No positioning specified, use default centered margins
                return (new WatermarkMargins
                {
                    RelativeTo = MapRelativeTo(layer.RelativeTo),
                    Left = 0,
                    Top = 0,
                    Right = 0,
                    Bottom = 0
                }, issues);
            }

            var hasPercentages = nonNullPositions.Any(p => p.Type == DistanceUnit.Units.Percentage);
            var hasPixels = nonNullPositions.Any(p => p.Type == DistanceUnit.Units.Pixels);
            
            if (hasPercentages && hasPixels)
            {
                issues.Add(new WatermarkConfigIssue
                {
                    Severity = IssueSeverity.Warning,
                    Summary = "Mixed pixel and percentage units",
                    Details = "Converting all values to percentages for Imageflow compatibility"
                });
                return (ConvertToFitBox(layer), issues);
            }
            
            if (hasPercentages)
            {
                return (ConvertToFitBox(layer), issues);
            }
            else
            {
                return (ConvertToMargins(layer), issues);
            }
        }

        private WatermarkFitBox ConvertToFitBox(LegacyLayerInfo layer)
        {
            var fitBox = new WatermarkFitBox
            {
                RelativeTo = MapRelativeTo(layer.RelativeTo)
            };
            
            // Calculate X1, Y1, X2, Y2 from layer positions
            // Default to full container if not specified
            fitBox.X1 = GetPercentageValue(layer.Left, 0);
            fitBox.Y1 = GetPercentageValue(layer.Top, 0);
            fitBox.X2 = layer.Right != null ? 100 - GetPercentageValue(layer.Right, 0) : 100;
            fitBox.Y2 = layer.Bottom != null ? 100 - GetPercentageValue(layer.Bottom, 0) : 100;
            
            return fitBox;
        }

        private WatermarkMargins ConvertToMargins(LegacyLayerInfo layer)
        {
            var margins = new WatermarkMargins
            {
                RelativeTo = MapRelativeTo(layer.RelativeTo)
            };
            
            // Extract pixel values, defaulting to 0
            margins.Left = (uint)GetPixelValue(layer.Left, 0);
            margins.Top = (uint)GetPixelValue(layer.Top, 0);
            margins.Right = (uint)GetPixelValue(layer.Right, 0);
            margins.Bottom = (uint)GetPixelValue(layer.Bottom, 0);
            
            return margins;
        }

        private WatermarkAlign MapRelativeTo(string relativeTo)
        {
            switch (relativeTo?.ToLowerInvariant())
            {
                case "canvas":
                    return WatermarkAlign.Canvas;
                case "image":
                case null:
                    return WatermarkAlign.Image;
                default:
                    return WatermarkAlign.Image;
            }
        }

        private float GetPercentageValue(DistanceUnit unit, float defaultValue)
        {
            if (unit == null) return defaultValue;
            
            if (unit.Type == DistanceUnit.Units.Percentage)
            {
                return (float)unit.Value;
            }
            
            // For pixel values in percentage context, we can't convert without knowing container size
            // This should have been caught by the mixed units check
            return defaultValue;
        }

        private double GetPixelValue(DistanceUnit unit, double defaultValue)
        {
            if (unit == null) return defaultValue;
            
            if (unit.Type == DistanceUnit.Units.Pixels)
            {
                return unit.Value;
            }
            
            // For percentage values in pixel context, we can't convert without knowing container size
            // This should have been caught by the mixed units check
            return defaultValue;
        }
    }

      /// <summary>
    /// Helper class for creating constraint gravity from ContentAlignment
    /// </summary>
    internal static class ConstraintGravityHelper
    {
        public static ConstraintGravity FromContentAlignment(ContentAlignment align)
        {
            switch (align)
            {
                case ContentAlignment.TopLeft:
                    return new ConstraintGravity(0, 0);
                case ContentAlignment.TopCenter:
                    return new ConstraintGravity(50, 0);
                case ContentAlignment.TopRight:
                    return new ConstraintGravity(100, 0);
                case ContentAlignment.MiddleLeft:
                    return new ConstraintGravity(0, 50);
                case ContentAlignment.MiddleCenter:
                    return new ConstraintGravity(50, 50);
                case ContentAlignment.MiddleRight:
                    return new ConstraintGravity(100, 50);
                case ContentAlignment.BottomLeft:
                    return new ConstraintGravity(0, 100);
                case ContentAlignment.BottomCenter:
                    return new ConstraintGravity(50, 100);
                case ContentAlignment.BottomRight:
                    return new ConstraintGravity(100, 100);
                default:
                    return new ConstraintGravity(); // defaults to center (50, 50)
            }
        }
    }
}
