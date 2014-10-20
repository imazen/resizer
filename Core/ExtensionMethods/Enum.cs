using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Collections;
using System.Reflection;
using System.Globalization;

namespace ImageResizer.ExtensionMethods {

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class EnumStringAttribute : Attribute {
        public string Name{get;set;}
        public bool Default { get; set; }

        public EnumStringAttribute(string name, bool defaultForSerialization)
            : base() {
                Name = name;
                Default = defaultForSerialization;
        }

        public EnumStringAttribute(string name) : this(name, false) { }
    }

    /// <summary>
    /// Extends enumerations by allowing them to define alternate strings with the [EnumString("Alternate Name",true)]  attribute, and support it through TryParse and ToPreferredString
    /// </summary>
    public static class EnumExtensions {



        private static Dictionary<Type, Dictionary<string, Enum>> values = null;
        private static Dictionary<Type, Dictionary<Enum, string>> preferredValues = null;

        private static void LoadValues(Type t) {
            //Copy dictionary so we can modify it safely.
            var v = values;
            var p = preferredValues;
            if (v == null) v = new Dictionary<Type, Dictionary<string, Enum>>();
            else v = new Dictionary<Type, Dictionary<string, Enum>>(v);
            if (p == null) p = new Dictionary<Type, Dictionary<Enum, string>>();
            else p = new Dictionary<Type, Dictionary<Enum, string>>(p);

            //Get the enumeration fields
            FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.GetField | BindingFlags.Public);

            //Create modifiable dictionaries
            var ev = new Dictionary<string, Enum>(fields.Length * 2, StringComparer.OrdinalIgnoreCase);
            var ep = new Dictionary<Enum, string>(fields.Length);

            //Loop through the enumerations
            foreach (FieldInfo field in fields) {
                string name = field.Name;
                Enum value = Enum.ToObject(t, field.GetRawConstantValue()) as Enum;
                //Add the default value
                ev[name] = value;

                string defaultName = name;
                //Add alternates
                object[] attrs = field.GetCustomAttributes(typeof(EnumStringAttribute), false);
                foreach(EnumStringAttribute a in attrs){
                    if (a.Default) defaultName = a.Name;
                    ev[a.Name] = value;
                }
                //Add the preferred name
                if (!ep.ContainsKey(value)) ep[value] = defaultName;

            }

            v[t] = ev;
            p[t] = ep;

            //Swap references
            values = v;
            preferredValues = p;

        }

        private static Dictionary<string, Enum> GetValues(Type t) {
            if (values == null) LoadValues(t);
            Dictionary<string, Enum> d = null;
            if (!values.TryGetValue(t, out d)) LoadValues(t);
            if (!values.TryGetValue(t, out d)) return null;
            return d;
        }

        private static Dictionary<Enum, string> GetPreferredStrings(Type t) {
            if (preferredValues == null) LoadValues(t);
            Dictionary<Enum, string> d;
            if (!preferredValues.TryGetValue(t, out d)) LoadValues(t);
            if (!preferredValues.TryGetValue(t, out d)) return null;
            return d;
        }

        /// <summary>
        /// Attempts case-insensitive parsing of the specified enum. Returns the specified default value if parsing fails.
        /// Supports [EnumString("Alternate Value")] attributes and parses flags. If any segment of a comma-delimited list isn't parsed as either a number or string, defaultValue will be returned.
        /// </summary>
        /// <param name="en"></param>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Parse<T>(this T en, string value, T defaultValue) where T : struct, IConvertible {
            T? val = EnumExtensions.Parse<T>(en, value);
            return val == null ? defaultValue : val.Value;
        }

        /// <summary>
        /// Attempts case-insensitive parsing of the specified enum. Returns null if parsing failed.
        /// Supports [EnumString("Alternate Value")] attributes and parses flags. If any segment of a comma-delimited list isn't parsed as either a number or string, null will be returned.
        /// </summary>
        /// <param name="en"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? Parse<T>(this T en, string value) where T:struct, IConvertible {
            return Parse<T>(value);
        }
        public static T? Parse<T>(string value) where T:struct, IConvertible {
            //Reject empty or whitespace values
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Trim();
            if (string.IsNullOrEmpty(value)) return null;

            //Get the type and load the dictionary
            Type t = typeof(T);
            Dictionary<string, Enum> d = GetValues(t);

            //Always parse flags, mimic Enum.Parse behavior. 
            long num = 0;
            bool parsedSomething = false;
            string[] parts = value.Split(',');
            for (int i = 0; i < parts.Length; i++) {
                string p = parts[i].Trim();
                if (p.Length == 0) continue;

                Enum part;
                long temp;
                if ((char.IsDigit(p[0]) || p[0] == '-' || p[0] == '+') && 
                    long.TryParse(p, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out temp)) {
                    num |= temp;
                    parsedSomething = true;
                }else if (d.TryGetValue(value, out part)){
                    num = num | Convert.ToInt64(part);
                    parsedSomething = true;
                } else return null; //If we fail to parse any non-empty bit, return null

            }
            //Only return a value if we parsed something 
            return parsedSomething ? (Enum.ToObject(t,num) as T?) : null;
        }

        /// <summary>
        /// Retuns the string representation for the given enumeration
        /// </summary>
        /// <param name="en"></param>
        /// <param name="lowerCase"></param>
        /// <returns></returns>
        public static string ToPreferredString(this Enum en, bool lowerCase) //ext method
        {
            Type t = en.GetType();
            bool isFlags = false; //Not supported yet t.IsDefined(typeof(FlagsAttribute), false);

            Dictionary<Enum,string> d = GetPreferredStrings(t);

            //Simple path
            if (!isFlags){
                string temp;
                if (!d.TryGetValue(en, out temp)) temp = en.ToString();
                return lowerCase ? temp.ToLowerInvariant() : temp;
            }
            return null;
            //TODO: loop through keys and use binary comparison to build a comma delimited list
        }
    }
}
