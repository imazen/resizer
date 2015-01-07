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

        public static readonly char[] Comma = new char[] { ',' };
        public static readonly char[] Period = new char[] { '.' };
        public static readonly char[] EqualSign = new char[] { '=' };
        public static readonly char[] VBar = new char[] { '|' };
        public static readonly char[] Octothorpe = new char[] { '#' };
        public static readonly char[] Ampersand = new char[] { '&' };
        public static readonly char[] Asterisk = new char[] { '*' };
        public static readonly char[] DoubleQuote = new char[] { '"' };
        public static readonly char[] QueryOrFragment = new char[] { '?', '#' };
        public static readonly char[] QueryPartsWithSemi = new char[] { '?', '&', ';' };
        public static readonly char[] QueryParts = new char[] { '?', '&' };
        public static readonly char[] ForwardSlash = new char[] { '/' };
        public static readonly char[] BackSlash = new char[] { '\\' };
        public static readonly char[] ForwardSlashOrTilde = new char[] { '/', '~' };
        public static readonly char[] Slashes = new char[] { '/', '\\' };
        public static readonly char[] SpaceOrSlashes = new char[] { ' ', '/', '\\' };
        public static readonly char[] SpaceOrPeriod = new char[] { ' ', '.' };
        public static readonly char[] SpaceOrComma = new char[] { ' ', ',' };
        public static readonly char[] ListNoise = new char[] { ' ', '(', ')' };
        public static readonly char[] ListParts = new char[] { ' ', '(', ')', ',' };
        public static readonly char[] PathParts = new char[] { '.', '/', ' ', '\\', '?', '&', ':' };
        public static readonly char[] VirtualPathParts = new char[] { '/', '\\', '~' };
        public static readonly string[] EmptyStringArray = new string[0];

        public static Color ParseColor(string value, Color defaultValue) {
            Color? c = ParseColor(value);
            return c == null ? defaultValue : c.Value;
        }
        public static Color? ParseColor(string value) {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.TrimStart(Octothorpe);
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
                text = text.TrimStart(Octothorpe);
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
