/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections.Specialized;
using System.Globalization;
using ImageResizer.Resizing;
using System.Drawing.Drawing2D;
using System.Web;
using System.IO;

namespace ImageResizer.Util {
    public class Utils {

  
        public static Color parseColor(string value, Color defaultValue) {
            if (!string.IsNullOrEmpty(value)) {
                value = value.TrimStart('#');
                //try hex first
                int val;
                if (int.TryParse(value, System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out val)) {
                    int alpha = 255;
                    if (value.Length == 4 || value.Length == 8) {
                        int regLength = value.Length - (value.Length / 4);
                        alpha = int.Parse(value.Substring(regLength), System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture);
                        if (regLength == 3) alpha *= 16;
                        value = value.Substring(0, regLength);
                    }
                    return Color.FromArgb(alpha, System.Drawing.ColorTranslator.FromHtml("#" + value));
                } else {
                    try {
                        Color c = System.Drawing.ColorTranslator.FromHtml(value); //Throws an 'Exception' instance if invalid
                        return (c.IsEmpty) ? defaultValue : c;
                    } catch {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }

        public static string writeColor(Color value) {
            string text =  System.Drawing.ColorTranslator.ToHtml(value);
            if (text.StartsWith("#")) {
                text = text.TrimStart('#');
                if (value.A != 255) text += value.A.ToString("X2", null);
            }
            return text;
        }
        /// <summary>
        /// Parses lists in the form "3,4,5,2,5" and "(3,4,40,50)". If a number cannot be parsed (i.e, number 2 in "5,,2,3") defaultValue is used.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double[] parseList(string text, double defaultValue) {
            text = text.Trim(' ', '(', ')');
            string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.None);
            double[] vals = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++) {
                if (!double.TryParse(parts[i], out vals[i]))
                    vals[i] = defaultValue;
            }
            return vals;
        }
        public static int getInt(NameValueCollection q, string name, int defaultValue) {
            int temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!int.TryParse(q[name], out temp)) return defaultValue;
            return temp;
        }
        public static float getFloat(NameValueCollection q, string name, float defaultValue) {
            float temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!float.TryParse(q[name], out temp)) return defaultValue;
            return temp;
        }

        public static double getDouble(NameValueCollection q, string name, double defaultValue) {
            double temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                if (!double.TryParse(q[name], out temp)) return defaultValue;
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
        public static void copyStream(Stream source, Stream dest) {
            byte[] buffer = new byte[32768];//8Kb
            while (true) {
                int len = source.Read(buffer, 0, buffer.Length);
                if (len <= 0) break;
                dest.Write(buffer, 0, len);
            }
        }

        


        /// <summary>
        /// Returns RotateNoneFlipNone if not a recognize value.
        /// </summary>
        /// <param name="sFlip"></param>
        /// <returns></returns>
        public static RotateFlipType parseFlip(string sFlip) {

            if (!string.IsNullOrEmpty(sFlip)) {
                if ("none".Equals(sFlip, StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipNone;
                else if (sFlip.Equals("h", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipX;
                else if (sFlip.Equals("x", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipX;
                else if (sFlip.Equals("v", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipY;
                else if (sFlip.Equals("y", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipY;
                else if (sFlip.Equals("both", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipXY;
                else if (sFlip.Equals("xy", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipXY;
            }
            return RotateFlipType.RotateNoneFlipNone;
        }

        /// <summary>
        /// Throws an exception if the specified value is unsupported. Rotation values are not supported, and should be specified with the Rotate command.
        /// </summary>
        /// <returns></returns>
        public static string writeFlip(RotateFlipType flip) {
            if (flip == RotateFlipType.RotateNoneFlipNone) return "none";
            if (flip == RotateFlipType.RotateNoneFlipX) return "x";
            if (flip == RotateFlipType.RotateNoneFlipY) return "y";
            if (flip == RotateFlipType.RotateNoneFlipXY) return "xy";

            throw new ArgumentException("Valid flip values are RotateNoneFlipNone, RotateNoneFlipX, RotateNoneFlipY, and RotateNoneFlipXY. Rotation must be specified with Rotate instead. Received: " + flip.ToString());
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
            if (double.TryParse(value, out temp) && temp > 0) return new KeyValuePair<CropUnits, double>(CropUnits.Custom, temp);
            else return new KeyValuePair<CropUnits, double>(CropUnits.SourcePixels, default(double));
        }
        public static string writeCropUnits(KeyValuePair<CropUnits, double> value) {
            if (value.Key == CropUnits.Custom) return value.Value.ToString();
            else if (value.Key == CropUnits.SourcePixels) return "sourcepixels";
            else throw new NotImplementedException("Unrecognized CropUnits value: " + value.ToString());
        }

        public static ScaleMode parseScale(string value) {
            if (value != null) {
                if (value.Equals("both", StringComparison.OrdinalIgnoreCase))
                    return ScaleMode.Both;
                else if (value.Equals("upscaleonly", StringComparison.OrdinalIgnoreCase) || value.Equals("up", StringComparison.OrdinalIgnoreCase))
                    return ScaleMode.UpscaleOnly;
                else if (value.Equals("downscaleonly", StringComparison.OrdinalIgnoreCase) || value.Equals("down", StringComparison.OrdinalIgnoreCase))
                    return ScaleMode.DownscaleOnly;
                else if (value.Equals("upscalecanvas", StringComparison.OrdinalIgnoreCase) || value.Equals("canvas", StringComparison.OrdinalIgnoreCase))
                    return ScaleMode.UpscaleCanvas;
            }
            //default
            return ScaleMode.DownscaleOnly;
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
                    c += d.ToString() + ",";
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
            if (p.All != double.NaN) return p.All.ToString(); //Easy

            return "(" + p.Left + "," + p.Top + "," + p.Right + "," + p.Bottom + ")";

        }

        /// <summary>
        /// Draws a gradient around the specified polygon. Fades from 'inner' to 'outer' over a distance of 'width' pixels. 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="poly"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="width"></param>
        public static void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width) {

            PointF[,] corners = PolygonMath.RoundPoints(PolygonMath.GetCorners(poly, width));
            PointF[,] sides = PolygonMath.RoundPoints(PolygonMath.GetSides(poly, width));
            //Overlapping these causes darker areas... Dont use InflatePoly

            //Paint corners
            for (int i = 0; i <= corners.GetUpperBound(0); i++) {
                PointF[] pts = PolygonMath.GetSubArray(corners, i);
                Brush b = PolygonMath.GenerateRadialBrush(inner, outer, pts[0], width + 1);

                g.FillPolygon(b, pts);
            }
            //Paint sides
            for (int i = 0; i <= sides.GetUpperBound(0); i++) {
                PointF[] pts = PolygonMath.GetSubArray(sides, i);
                LinearGradientBrush b = new LinearGradientBrush(pts[3], pts[0], inner, outer);
                b.SetSigmaBellShape(1);
                b.WrapMode = WrapMode.TileFlipXY;
                g.FillPolygon(b, pts);
            }
        }



        
    }
}
