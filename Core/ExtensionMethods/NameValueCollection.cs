using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Globalization;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.ExtensionMethods {
    public static class NameValueCollectionExtensions {

        private static NumberStyles floatingPointStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | 
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

        public static string GetAsString(this NameValueCollection t, string name, string defaultValue) 
        {
            var val = t.Get(name);
            return string.IsNullOrEmpty(val) ? defaultValue : val;
        }

        /// <summary>
        /// Provides culture-invariant parsing of int, double, float, bool, and enum values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T? Get<T>(this NameValueCollection t, string name) where T : struct, IConvertible {
            return Get<T>(t, name, null);
        }
        /// <summary>
        /// Provides culture-invariant parsing of int, double, float, bool, and enum values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? Get<T>(this NameValueCollection q, string name, T? defaultValue) where T:struct, IConvertible{
            return ParsePrimitive<T>(q[name],defaultValue);
        }
        /// <summary>
        /// Provides culture-invariant parsing of int, double, float, bool, and enum values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this NameValueCollection q, string name, T defaultValue) where T : struct, IConvertible {
            return ParsePrimitive<T>(q[name], defaultValue).Value;
        }


        /// <summary>
        /// Provides culture-invariant parsing of int, double, float, bool, and enum values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? ParsePrimitive<T>(string value, T? defaultValue) where T : struct,IConvertible {
            if (value == null) return defaultValue;
            value = value.Trim(); //Trim whitespace
            if (value.Length == 0) return defaultValue;

            Type t = typeof(T);

            if (t == typeof(byte)) {
                byte temp = 0;
                if (byte.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out temp)) return temp as T?;
            } else if (t == typeof(int)) {
                int temp = 0;
                if (int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out temp)) return temp as T?;
            } else if (t == typeof(double)) {
                double temp = 0;
                if (double.TryParse(value, floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) return temp as T?;
            } else if (t == typeof(float)) {
                float temp = 0;
                if (float.TryParse(value, floatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) return temp as T?;
            } else if (t == typeof(bool)) {
                string s = value;
                if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true as T?;
                else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false as T?;
            } else if (t.IsEnum) {
                T? val = EnumExtensions.Parse<T>(value); //Support EnumString values
                if (val.HasValue) return val;
            } else {
                T? val =  value as T?; //Just try casting
                if (val.HasValue) return val;
            }

            return defaultValue;
        }


        public static string SerializePrimitive<T>(T? val) where T : struct, IConvertible {
            if (val == null) return null;
            T value = val.Value;
            Type t = typeof(T);
            if (t.IsEnum) {
                return EnumExtensions.ToPreferredString(value as Enum, true);
            } else {
                return Convert.ToString(value, NumberFormatInfo.InvariantInfo).ToLowerInvariant();
            }

        }

        /// <summary>
        /// Serializes the given value by calling .ToString(). If the value is null, the key is removed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static NameValueCollection SetAsString<T>(this NameValueCollection q, string name, T val) where T : class {
            if (val == null) q.Remove(name);
            else q[name] = val.ToString();
            return q;
        }

        /// <summary>
        /// Provides culture-invariant serialization of value types, in lower case for querystring readability. Setting a key to null removes it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static NameValueCollection Set<T>(this NameValueCollection q, string name, T? val) where T : struct, IConvertible {
            if (val == null) q.Remove(name);
            else q[name] = SerializePrimitive<T>(val);
            return q;
        }

        public static T[] GetList<T>(this NameValueCollection q, string name, T? fallbackValue, params int[] allowedSizes) where T : struct, IConvertible {
            return ParseList<T>(q[name], fallbackValue, allowedSizes);
        }

        /// <summary>
        /// Parses a comma-delimited list of primitive values. If there are unparsable items in the list, they will be replaced with 'fallbackValue'. If fallbackValue is null, the function will return null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="fallbackValue"></param>
        /// <param name="allowedSizes"></param>
        /// <returns></returns>
        public static T[] ParseList<T>(string text, T? fallbackValue, params int[] allowedSizes) where T : struct, IConvertible {
            if (text == null) return null;
            text = text.Trim(' ', '(', ')', ','); //Trim parenthesis, commas, and spaces
            if (text.Length == 0) return null;

            string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.None);

            //Verify the array is of an accepted size if any are specified
            bool foundCount = allowedSizes.Length == 0;
            foreach (int c in allowedSizes) if (c == parts.Length) foundCount = true;
            if (!foundCount) return null;

            //Parse the array
            var vals = new T[parts.Length];
            for (var i = 0; i < parts.Length; i++) {
                var v = ParsePrimitive<T>(parts[i], fallbackValue);
                if (v == null) return null;
                vals[i] = v.Value;
            }
            return vals;
        }


        private static string JoinPrimitives<T>(T[] array, char delimiter) where T : struct, IConvertible {
            var sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++) {
                sb.Append(SerializePrimitive<T>(array[i]));
                if (i < array.Length - 1) sb.Append(delimiter);
            }
            return sb.ToString();
        }

        public static NameValueCollection SetList<T>(this NameValueCollection q, string name, T[] values, bool throwExceptions, params int[] allowedSizes) where T : struct, IConvertible {
            if (values == null) { q.Remove(name); return q; }
            //Verify the array is of an accepted size
            bool foundCount = allowedSizes.Length == 0;
            foreach (int c in allowedSizes) if (c == values.Length) foundCount = true;
            if (!foundCount) {
                if (throwExceptions) throw new ArgumentOutOfRangeException("values", "The specified array is not a valid length. Valid lengths are " + JoinPrimitives<int>(allowedSizes, ','));
                else return q;
            }
            q[name] = JoinPrimitives<T>(values, ',');
            return q;
        }



        /// <summary>
        /// Returns true if any of the specified keys contain a value
        /// </summary>
        /// <param name="q"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static bool IsOneSpecified(this NameValueCollection q, params string[] keys) {
            foreach (String s in keys) if (!string.IsNullOrEmpty(q[s])) return true;
            return false;
        }



        /// <summary>
        /// Normalizes a command that has two possible names. 
        /// If either of the commands has a null or empty value, those keys are removed. 
        /// If both the the primary and secondary are present, the secondary is removed. 
        /// Otherwise, the secondary is renamed to the primary name.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public static NameValueCollection Normalize(this NameValueCollection q, string primary, string secondary) {
            //Get rid of null and empty values.
            if (string.IsNullOrEmpty(q[primary])) q.Remove(primary);
            if (string.IsNullOrEmpty(q[secondary])) q.Remove(secondary);
            //Our job is done if no secondary value exists.
            if (q[secondary] == null) return q;
            else {
                //Otherwise, we have to resolve it
                //No primary value? copy the secondary one. Otherwise leave it be
                if (q[primary] == null) q[primary] = q[secondary];
                //In either case, we now have a duplicate to remove
                q.Remove(secondary);
            }
            return q;
        }

        /// <summary>
        /// Creates and returns a new NameValueCollection instance that contains only the specified keys from the current collection.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="keysToKeep"></param>
        /// <returns></returns>
        public static NameValueCollection Keep(this NameValueCollection q, params string[] keysToKeep) {
            NameValueCollection c = new NameValueCollection();
            foreach (string s in keysToKeep)
                if (q[s] != null) c[s] = q[s];

            return c;
        }

        /// <summary>
        /// Creates and returns a new NameValueCollection instance that contains all of the
        /// keys/values from 'q', and any keys/values from 'defaults' that 'q' does not already
        /// contain.
        /// </summary>
        /// <param name="q">The settings specific to a particular query</param>
        /// <param name="defaults">Default settings to use when not overridden by 'q'.</param>
        /// <returns></returns>
        public static NameValueCollection MergeDefaults(this NameValueCollection q, NameValueCollection defaults)
        {
            // Start with the defaults, and then blindly copy the keys/values
            // from 'q'.  Any keys in both 'defaults' and 'q' will end up having
            // the value from 'q', since that one is set last.
            NameValueCollection c = new NameValueCollection(defaults);
            foreach (string s in q.AllKeys) {
                c[s] = q[s];
            }

            return c;
        }
    }
}
