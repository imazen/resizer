using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using ImageResizer;
using ImageResizer.Util;
using Imageflow.Fluent;
using System.Linq;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Builds cache keys for watermarked images
    /// </summary>
    internal class WatermarkCacheKeyBuilder
    {

        /// <summary>
        ///  This is the only entry point. 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static string BuildCacheKey(WatermarkConfiguration configuration)
        {
            var sb = new StringBuilder();
            sb.Append(configuration.Name);
            sb.Append("|");
            AppendCacheKey(sb, configuration.Layers.Select(l => (l.Options, l.Path, l.PreprocessingQuery)));
            return sb.ToString();
        }

        /// <summary>
        /// Appends watermark cache key information to a StringBuilder
        /// </summary>
        public static void AppendCacheKey(StringBuilder sb, IEnumerable<(WatermarkOptions options, string path, ImageResizer.Instructions preprocessing)> watermarks)
        {
            if (watermarks == null) return;
            
            bool first = true;
            foreach (var (options, path, preprocessing) in watermarks)
            {
                if (!first) sb.Append(',');
                first = false;
                AppendWatermarkCacheKey(sb, options, path, preprocessing);
            }
        }
        
        /// <summary>
        /// Appends a cache key for a single watermark to the StringBuilder
        /// </summary>
        private static void AppendWatermarkCacheKey(StringBuilder sb, WatermarkOptions options, string path, NameValueCollection preprocessing)
        {
            bool needsPipe = false;

            void AppendSeparator()
            {
                if (needsPipe) sb.Append('|');
                needsPipe = true;
            }

            // Path
            if (!string.IsNullOrEmpty(path))
            {
                AppendSeparator();
                sb.Append("p:").Append(path);
            }

            // Preprocessing
            if (preprocessing != null && preprocessing.Count > 0)
            {
                AppendSeparator();
                sb.Append("q:");
                bool firstParam = true;
                foreach (string key in preprocessing.AllKeys)
                {
                    var value = preprocessing[key];
                    if (string.IsNullOrEmpty(value)) continue;

                    if (!firstParam) sb.Append('&');
                    firstParam = false;
                    sb.Append(key).Append('=').Append(value);
                }
            }

            // Options
            if (options != null)
            {
                if (options.Opacity.HasValue)
                {
                    AppendSeparator();
                    sb.Append("o:").AppendFormat(CultureInfo.InvariantCulture, "{0:F2}", options.Opacity.Value);
                }

                if (options.FitMode.HasValue)
                {
                    AppendSeparator();
                    sb.Append("fm:").Append(options.FitMode.Value);
                }

                if (options.FitBox != null)
                {
                    AppendSeparator();
                    if (options.FitBox is WatermarkMargins margins)
                    {
                        sb.Append("m:").Append(margins.RelativeTo).Append(':')
                          .Append(margins.Left).Append(',')
                          .Append(margins.Top).Append(',')
                          .Append(margins.Right).Append(',')
                          .Append(margins.Bottom);
                    }
                    else if (options.FitBox is WatermarkFitBox fitBox)
                    {
                        sb.Append("fb:").Append(fitBox.RelativeTo).Append(':')
                          .AppendFormat(CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1},{3:F1}",
                              fitBox.X1, fitBox.Y1, fitBox.X2, fitBox.Y2);
                    }
                }

                if (options.Gravity != null)
                {
                    AppendSeparator();
                    sb.Append("g:").AppendFormat(CultureInfo.InvariantCulture, "{0:F0},{1:F0}", options.Gravity.XPercent, options.Gravity.YPercent);
                }
                
                if (options.MinCanvasWidth.HasValue || options.MinCanvasHeight.HasValue)
                {
                    AppendSeparator();
                    sb.Append("min:").Append(options.MinCanvasWidth ?? 0).Append('x').Append(options.MinCanvasHeight ?? 0);
                }

                if (options.Hints != null)
                {
                    AppendSeparator();
                    AppendResampleHintsCacheKey(sb, options.Hints);
                }
            }
        }

        /// <summary>
        /// Appends ResampleHints to the cache key
        /// </summary>
        private static void AppendResampleHintsCacheKey(StringBuilder sb, ResampleHints hints)
        {
            sb.Append("h:");
            bool needsComma = false;

            void AppendSeparator()
            {
                if (needsComma) sb.Append(',');
                needsComma = true;
            }

            if (hints.DownFilter.HasValue)
            {
                AppendSeparator();
                sb.Append("df:").Append(hints.DownFilter.Value);
            }

            if (hints.UpFilter.HasValue)
            {
                AppendSeparator();
                sb.Append("uf:").Append(hints.UpFilter.Value);
            }

            if (hints.SharpenPercent.HasValue)
            {
                AppendSeparator();
                sb.Append("sp:").AppendFormat(CultureInfo.InvariantCulture, "{0:F1}", hints.SharpenPercent.Value);
            }

            if (hints.InterpolationColorspace.HasValue)
            {
                AppendSeparator();
                sb.Append("ic:").Append(hints.InterpolationColorspace.Value);
            }

            if (hints.ResampleWhen.HasValue)
            {
                AppendSeparator();
                sb.Append("rw:").Append(hints.ResampleWhen.Value);
            }

            if (hints.SharpenWhen.HasValue)
            {
                AppendSeparator();
                sb.Append("sw:").Append(hints.SharpenWhen.Value);
            }
        }
    }
} 