using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util {
    /// <summary>
    /// Provides invariant parsing &amp; serialization of primitive types, like Enums, integers, floats, and booleans.
    /// </summary>
    public class ParseUtils {

        /// <summary>
        /// Defines a parsing style that permits leading/trailing whitespace, a leading negitve/postiive sign, decimal points, exponential notation, and a thousands separator
        /// </summary>
        public const NumberStyles FloatingPointStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;


        public static Color ParseColor(string value, Color defaultValue) {
            Color? c = ParseColor(value);
            return c == null ? defaultValue : c.Value;
        }
        public static Color? ParseColor(string value) {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.TrimStart('#');
            //try hex first
            int val;
            if (int.TryParse(value, System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val)) {
                int alpha = 255;
                if (value.Length == 4 || value.Length == 8) {
                    int regLength = value.Length - (value.Length / 4);
                    alpha = int.Parse(value.Substring(regLength), System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    if (regLength == 3) alpha *= 16;
                    value = value.Substring(0, regLength);
                }
                return Color.FromArgb(alpha, System.Drawing.ColorTranslator.FromHtml("#" + value));
            } else {
                try {
                    Color c = System.Drawing.ColorTranslator.FromHtml(value); //Throws an 'Exception' instance if invalid
                    return (c.IsEmpty) ? null : (Nullable<Color>)c;
                } catch {
                    return null;
                }
            }

        }

        public static string SerializeColor(Color value) {
            string text = System.Drawing.ColorTranslator.ToHtml(value);
            if (text.StartsWith("#")) {
                text = text.TrimStart('#');
                if (value.A != 255) text += value.A.ToString("X2", NumberFormatInfo.InvariantInfo);
            }
            return text;
        }


        public static T ParsePrimitive<T>(string value, T defaultValue) where T : struct,IConvertible {
            return NameValueCollectionExtensions.ParsePrimitive<T>(value, defaultValue).Value;
        }
        public static T? ParsePrimitive<T>(string value) where T : struct,IConvertible {
            return NameValueCollectionExtensions.ParsePrimitive<T>(value, null);
        }

        public static string SerializePrimitive<T>(T? val) where T : struct, IConvertible {
            return NameValueCollectionExtensions.SerializePrimitive<T>(val);
        }

        public static T[] ParseList<T>(string text, T? fallbackValue, params int[] allowedSizes) where T : struct, IConvertible {
            return NameValueCollectionExtensions.ParseList<T>(text, fallbackValue, allowedSizes);
        }


    }
}
