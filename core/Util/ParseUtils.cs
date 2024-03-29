// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Drawing;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util
{
    /// <summary>
    ///     Provides invariant parsing &amp; serialization of primitive types, like enums, integers, floats, and booleans.
    /// </summary>
    public class ParseUtils
    {
        /// <summary>
        ///     Defines a parsing style that permits leading/trailing whitespace, a leading negative/positive sign, decimal points,
        ///     exponential notation, and a thousands separator
        /// </summary>
        public const NumberStyles FloatingPointStyle =
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands |
            NumberStyles.AllowExponent;


        public static Color ParseColor(string value, Color defaultValue)
        {
            var c = ParseColor(value);
            return c == null ? defaultValue : c.Value;
        }

        public static Color? ParseColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.TrimStart('#');
            //try hex first
            int val;
            if (int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                var alpha = 255;
                if (value.Length == 4 || value.Length == 8)
                {
                    var regLength = value.Length - value.Length / 4;
                    alpha = int.Parse(value.Substring(regLength), NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture);
                    if (regLength == 3) alpha *= 16;
                    value = value.Substring(0, regLength);
                }

                return Color.FromArgb(alpha, ColorTranslator.FromHtml("#" + value));
            }
            else
            {
                try
                {
                    var c = ColorTranslator.FromHtml(value); //Throws an 'Exception' instance if invalid
                    return c.IsEmpty ? null : (Nullable<Color>)c;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static string SerializeColor(Color value)
        {
            var text = ColorTranslator.ToHtml(value);
            if (text.StartsWith("#"))
            {
                text = text.TrimStart('#');
                if (value.A != 255) text += value.A.ToString("X2", NumberFormatInfo.InvariantInfo);
            }

            return text;
        }


        public static T ParsePrimitive<T>(string value, T defaultValue) where T : struct, IConvertible
        {
            return NameValueCollectionExtensions.ParsePrimitive<T>(value, defaultValue).Value;
        }

        public static T? ParsePrimitive<T>(string value) where T : struct, IConvertible
        {
            return NameValueCollectionExtensions.ParsePrimitive<T>(value, null);
        }

        public static string SerializePrimitive<T>(T? val) where T : struct, IConvertible
        {
            return NameValueCollectionExtensions.SerializePrimitive<T>(val);
        }

        public static T[] ParseList<T>(string text, T? fallbackValue, params int[] allowedSizes)
            where T : struct, IConvertible
        {
            return NameValueCollectionExtensions.ParseList<T>(text, fallbackValue, allowedSizes);
        }
    }
}