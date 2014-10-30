/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Collections.Specialized;
using System.Globalization;
using ImageResizer.Resizing;
using System.IO;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util {
    [Obsolete("All methods of this class have been deprecated. Use ParseUtils or ImageResizer.ExtensionMethods instead.  Will be removed in V3.5 or V4.")]
    public class Utils {
        
  
        public static Color parseColor(string value, Color defaultValue) {
            return ParseUtils.ParseColor(value, defaultValue);
        }

        public static string writeColor(Color value) {
            return ParseUtils.SerializeColor(value);
        }
        /// <summary>
        /// Parses lists in the form "3,4,5,2,5" and "(3,4,40,50)". If a number cannot be parsed (i.e, number 2 in "5,,2,3") defaultValue is used.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double[] parseList(string text, double defaultValue) {
            text = text.Trim(' ', '(', ')');
            var parts = text.Split(new[] { ',' }, StringSplitOptions.None);
            var vals = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++) {
                if (!double.TryParse(parts[i], floatingPointStyle, NumberFormatInfo.InvariantInfo, out vals[i]))
                    vals[i] = defaultValue;
            }
            return vals;
        }
        public const NumberStyles floatingPointStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | 
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

        public static int getInt(NameValueCollection q, string name, int defaultValue) {
            int temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!int.TryParse(q[name], NumberStyles.Integer,NumberFormatInfo.InvariantInfo, out temp)) return defaultValue;
            return temp;
        }

        public static float getFloat(NameValueCollection q, string name, float defaultValue) {
            float temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!float.TryParse(q[name], floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) return defaultValue;
            return temp; 
        }

        public static double getDouble(NameValueCollection q, string name, double defaultValue) {
            double temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!double.TryParse(q[name], floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) return defaultValue;
            return temp;
        }
        public static bool getBool(NameValueCollection q, string name, bool defaultValue) {
            bool temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name])){
                string s = q[name];
                if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
                else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return temp;
        }


        public static T parseEnum<T>(string value, T defaultValue) where T : struct, IConvertible {
            //if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");

            if (value == null) return defaultValue;
            else value = value.Trim();
            try {
                return (T)Enum.Parse(typeof(T), value, true);
            } catch (ArgumentException) {
                return defaultValue;
            }
        }



        /// <summary>
        /// Copies all remaining data from 'source' to 'dest'
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        [Obsolete("Use ImageResizer.ExtensionMethods instead.")]
        public static void copyStream(Stream source, Stream dest) {
            StreamExtensions.CopyToStream(source, dest, false, 0x1000); 
        }

        


        /// <summary>
        /// Returns RotateNoneFlipNone if not a recognized value.
        /// </summary>
        /// <param name="sFlip"></param>
        /// <returns></returns>
        public static RotateFlipType parseFlip(string sFlip) {
            return (RotateFlipType)ParseUtils.ParsePrimitive<FlipMode>(sFlip, FlipMode.None);
        }

        /// <summary>
        /// Returns 0 if not a recognized value. Rounds the value to 0, 90, 180, or 270
        /// </summary>
        /// <returns></returns>
        public static double parseRotate(string s) {

            if (!string.IsNullOrEmpty(s)) {
                double temp;
                if (!double.TryParse(s,floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) return 0;
                
                return normalizeTo90Intervals(temp);
            }
            return 0;
        }

        public static double normalizeTo90Intervals(double d){
            return PolygonMath.NormalizeTo90Intervals(d);
        }

        public static RotateFlipType combineFlipAndRotate(RotateFlipType flip, double angle) {
            return PolygonMath.CombineFlipAndRotate(flip, angle);
        }


        /// <summary>
        /// Throws an exception if the specified value is unsupported. Rotation values are not supported, and should be specified with the Rotate or srcRotate command.
        /// </summary>
        /// <returns></returns>
        public static string writeFlip(RotateFlipType flip) {
            if (flip == RotateFlipType.RotateNoneFlipNone) return "none";
            if (flip == RotateFlipType.RotateNoneFlipX) return "x";
            if (flip == RotateFlipType.RotateNoneFlipY) return "y";
            if (flip == RotateFlipType.RotateNoneFlipXY) return "xy";

            throw new ArgumentException("Valid flip values are RotateNoneFlipNone, RotateNoneFlipX, RotateNoneFlipY, and RotateNoneFlipXY. Rotation must be specified with Rotate or srcRotate instead. Received: " + flip.ToString());
        }

        public static StretchMode parseStretch(string value) {
            if ("fill".Equals(value, StringComparison.OrdinalIgnoreCase)) return StretchMode.Fill;
            return StretchMode.Proportionally;
        }
        public static string writeStretch(StretchMode value) {
            if (value == StretchMode.Proportionally) return "proportionally";
            else if (value == StretchMode.Fill) return "fill";
            throw new NotImplementedException("Unrecognized ScaleMode value: " + value.ToString());
        }
        public static KeyValuePair<CropUnits, double> parseCropUnits(string value) {
            if (string.IsNullOrEmpty(value)) return new KeyValuePair<CropUnits, double>(CropUnits.SourcePixels, default(double));

            double temp;
            if (double.TryParse(value,floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp) && temp > 0) return new KeyValuePair<CropUnits, double>(CropUnits.Custom, temp);
            else return new KeyValuePair<CropUnits, double>(CropUnits.SourcePixels, default(double));
        }
        public static string writeCropUnits(KeyValuePair<CropUnits, double> value) {
            if (value.Key == CropUnits.Custom) return value.Value.ToString();
            else if (value.Key == CropUnits.SourcePixels) return "sourcepixels";
            else throw new NotImplementedException("Unrecognized CropUnits value: " + value.ToString());
        }

        public static ScaleMode parseScale(string value) {
            return ParseUtils.ParsePrimitive<ScaleMode>(value, ScaleMode.DownscaleOnly);
        }
        public static string writeScale(ScaleMode value) {
            if (value == ScaleMode.Both) return "both";
            if (value == ScaleMode.DownscaleOnly) return "downscaleonly";
            if (value == ScaleMode.UpscaleCanvas) return "upscalecanvas";
            if (value == ScaleMode.UpscaleOnly) return "upscaleonly";
            throw new NotImplementedException("Unrecognized ScaleMode value: " + value.ToString());
        }

        public static KeyValuePair<CropMode, double[]> parseCrop(string value) {
            //Default to none if null
            if (string.IsNullOrEmpty(value)) return new KeyValuePair<CropMode, double[]>(CropMode.None, null);

            if ("auto".Equals(value, StringComparison.OrdinalIgnoreCase)) return new KeyValuePair<CropMode, double[]>(CropMode.Auto, null);

            double[] coords = parseList(value, double.NaN);
            if (coords.Length == 4) return new KeyValuePair<CropMode, double[]>(CropMode.Custom, coords);

            //Default to none if unrecognized
            return new KeyValuePair<CropMode, double[]>(CropMode.None, null);
        }

        public static string writeCrop(CropMode mode, double[] coords) {
            if (mode == CropMode.Auto) return "auto";
            if (mode == CropMode.None) return "none";
            if (mode == CropMode.Custom) {
                string c = "(";
                foreach (double d in coords)
                    c += d.ToString(NumberFormatInfo.InvariantInfo) + ",";
                return c.TrimEnd(',') + ")";
            }
            throw new NotImplementedException("Unrecognized CropMode value: " + mode.ToString());
        }

        /// <summary>
        /// Parses padding, allowing syntax (all) and (left, top, right, bottom). Parens are optional.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BoxPadding parsePadding(string value) {
            //Default to none if null
            if (string.IsNullOrEmpty(value)) return BoxPadding.Empty;

            double[] coords = parseList(value, 0);
            if (coords.Length == 1) return new BoxPadding(coords[0]);
            if (coords.Length == 4) return new BoxPadding(coords[0], coords[1], coords[2], coords[3]);

            return BoxPadding.Empty; //Unrecognized value;
        }
        public static PointF parsePointF(string value, PointF defaultValue) {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            double[] coords = parseList(value, 0);
            if (coords.Length == 2) return new PointF((float)coords[0], (float)coords[1]);
            return defaultValue; //Unrecognized value;
        }


        public static string writePadding(BoxPadding p) {
            if (!double.IsNaN(p.All)) return p.All.ToString(NumberFormatInfo.InvariantInfo); //Easy

            return "(" + p.Left.ToString(NumberFormatInfo.InvariantInfo) + "," + p.Top.ToString(NumberFormatInfo.InvariantInfo) + "," +
                p.Right.ToString(NumberFormatInfo.InvariantInfo) + "," + p.Bottom.ToString(NumberFormatInfo.InvariantInfo) + ")";

        }

        /// <summary>
        /// Draws a gradient around the specified polygon. Fades from 'inner' to 'outer' over a distance of 'width' pixels. 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="poly"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="width"></param>
        [Obsolete("This method will be removed in V3.3. Use DropShadow.DrawOuterGradient instead")]
        public static void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width) {
            ImageResizer.Plugins.Basic.DropShadow.DrawOuterGradient(g, poly, inner, outer, width);
        }



        
    }
}
