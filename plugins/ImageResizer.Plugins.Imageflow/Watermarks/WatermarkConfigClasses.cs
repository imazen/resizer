using System;
using System.Collections.Generic;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Util;
using Imageflow.Fluent;
using Imazen.Common.Issues;
using System.Linq;
using ImageResizer;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Configuration for a watermark
    /// </summary>
    internal class WatermarkConfig
    {
        /// <summary>
        /// The name of the watermark
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path to the watermark image
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Watermark-specific options
        /// </summary>
        public WatermarkOptions Options { get; set; }

        /// <summary>
        /// Preprocessing settings for the watermark image
        /// </summary>
        public Instructions PreprocessingQuery { get; set; }
    }

    /// <summary>
    /// Represents a parsed watermark configuration
    /// </summary>
    internal class WatermarkConfiguration
    {
        public string Name { get; set; }
        public List<WatermarkLayerConfig> Layers { get; set; } = new List<WatermarkLayerConfig>();

        public string CacheKey { get; set; }
    }

    /// <summary>
    /// Configuration for a single watermark layer
    /// </summary>
    internal class WatermarkLayerConfig
    {
        /// <summary>
        /// Path to the watermark image
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Query string for preprocessing the watermark image
        /// </summary>
        public Instructions PreprocessingQuery { get; set; }
        
        /// <summary>
        /// Imageflow watermark options
        /// </summary>
        public WatermarkOptions Options { get; set; }
        
        /// <summary>
        /// Whether this is a text layer (for future use)
        /// </summary>
        public bool IsTextLayer { get; set; }
        
        /// <summary>
        /// Original layer data for diagnostics
        /// </summary>
        public LegacyLayerInfo LegacyInfo { get; set; }
        
        /// <summary>
        /// Generates a cache key for this watermark layer
        /// </summary>
        public string GetCacheKey()
        {
            var parts = new List<string>();
            
            // Path
            if (!string.IsNullOrEmpty(Path))
                parts.Add($"p:{Path}");
            
            // Preprocessing query
            if (PreprocessingQuery != null && PreprocessingQuery.Count > 0)
            {
                var queryString = string.Join("&", PreprocessingQuery
                    .AllKeys
                    .Select(k => $"{k}={PreprocessingQuery[k]}"));
                parts.Add($"q:{queryString}");
            }
            
            // Options
            if (Options != null)
            {
                if (Options.Opacity.HasValue)
                    parts.Add($"o:{Options.Opacity.Value:F2}");
                
                if (Options.FitMode.HasValue)
                    parts.Add($"fm:{Options.FitMode.Value}");
                
                if (Options.FitBox != null)
                {
                    if (Options.FitBox is WatermarkMargins margins)
                    {
                        parts.Add($"m:{margins.RelativeTo}:{margins.Left},{margins.Top},{margins.Right},{margins.Bottom}");
                    }
                    else if (Options.FitBox is WatermarkFitBox fitBox)
                    {
                        parts.Add($"fb:{fitBox.RelativeTo}:{fitBox.X1:F1},{fitBox.Y1:F1},{fitBox.X2:F1},{fitBox.Y2:F1}");
                    }
                }
                
                if (Options.Gravity != null)
                {
                    parts.Add($"g:{Options.Gravity.XPercent:F0},{Options.Gravity.YPercent:F0}");
                }
                
                if (Options.MinCanvasWidth.HasValue || Options.MinCanvasHeight.HasValue)
                    parts.Add($"min:{Options.MinCanvasWidth ?? 0}x{Options.MinCanvasHeight ?? 0}");
            }
            
            return string.Join("|", parts);
        }
    }

    /// <summary>
    /// Stores original legacy layer information for diagnostics
    /// </summary>
    internal class LegacyLayerInfo
    {
        public DistanceUnit Left { get; set; }
        public DistanceUnit Top { get; set; }
        public DistanceUnit Right { get; set; }
        public DistanceUnit Bottom { get; set; }
        public DistanceUnit Width { get; set; }
        public DistanceUnit Height { get; set; }
        public string RelativeTo { get; set; }
        public ContentAlignment Align { get; set; }
        public bool Fill { get; set; }
        public string DrawAs { get; set; }
        public string ImageQuery { get; set; }
    }

    /// <summary>
    /// Represents a distance value that can be either pixels or percentage
    /// </summary>
    internal class DistanceUnit
    {
        public enum Units { Pixels, Percentage }
        
        public double Value { get; set; }
        public Units Type { get; set; }
        
        public DistanceUnit(double value, Units type)
        {
            Value = value;
            Type = type;
        }
        
        /// <summary>
        /// Parses a string value into a DistanceUnit
        /// </summary>
        public static DistanceUnit TryParse(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            
            value = value.Trim();
            Units type = Units.Pixels;
            
            if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 2);
                type = Units.Pixels;
            }
            else if (value.EndsWith("%"))
            {
                value = value.Substring(0, value.Length - 1);
                type = Units.Percentage;
            }
            else if (value.EndsWith("percent", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 7);
                type = Units.Percentage;
            }
            else if (value.EndsWith("pct", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 3);
                type = Units.Percentage;
            }
            
            if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return new DistanceUnit(val, type);
            }
            
            return null;
        }
        
        public override string ToString()
        {
            return Type == Units.Pixels 
                ? $"{Value}px" 
                : $"{Value}%";
        }
    }

    /// <summary>
    /// Configuration issue for watermark diagnostics
    /// </summary>
    internal class WatermarkConfigIssue : IIssue
    {
        public string Source => "WatermarkPlugin.Imageflow";
        public IssueSeverity Severity { get; set; }
        public string Summary { get; set; }
        public string Details { get; set; }
        public string AffectedNode { get; set; }
        
        public int Hash()
        {
            return (Source + Severity + Summary + Details + AffectedNode).GetHashCode();
        }
    }

  
} 