using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Specialized;
using System.Globalization;

namespace ImageResizer.Plugins.PsdComposer
{
    /// <summary>
    /// Layer names are case-insensitive. Asterisks can be used as wildcards to specify suffixes, prefixs, and search terms.
    /// </summary>
    public class PsdCommandBuilder 
    {
        public Dictionary<string, Color> layerColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, bool> layerVisibility = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, bool> layerRedraw = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> layerText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Set the renderer. graphicsmill and psdplugin are the currently supported values
        /// </summary>
        public string renderer = "psdplugin";
        public PsdCommandBuilder()
        {
        }
        public PsdCommandBuilder(NameValueCollection queryString)
        {
            this.layerColors = parseColorDict(queryString["layerColors"]);
            this.layerVisibility = parseBooleanDict(queryString["layerVisibility"]);
            this.layerRedraw = parseBooleanDict(queryString["layerRedraw"]);
            this.layerText = parseStringDict(queryString["layerText"]);
            this.renderer = queryString["renderer"];
        }

        public void SaveToQuerystring(NameValueCollection queryString)
        {
            queryString["layerColors"] = serializeColorDict(layerColors);
            queryString["layerVisibility"] = serializeBooleanDict(layerVisibility);
            queryString["layerRedraw"] = serializeBooleanDict(layerRedraw);
            queryString["layerText"] = serializeStringDict(layerText);
            queryString["renderer"] = renderer;
        }


        public Dictionary<string, Color> parseColorDict(string str)
        {
            if (string.IsNullOrEmpty(str)) return new Dictionary<string, System.Drawing.Color>();
            string[] parts = str.Trim('|').Split('|');
            
            if (parts.Length % 2 != 0) throw new ArgumentException("Invalid string! Must have an even number of parts.");
            Dictionary<string, Color> dict = new Dictionary<string, Color>();
            
            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                dict.Add(Base64UrlDecode(parts[i]), System.Drawing.Color.FromArgb(int.Parse(parts[i + 1], System.Globalization.NumberStyles.HexNumber,NumberFormatInfo.InvariantInfo)));
            }
            return dict;
        }
        public string serializeColorDict(Dictionary<string, Color> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Color> p in dict)
            {
                sb.Append(Base64UrlEncode(p.Key));
                sb.Append("|");
                sb.Append(p.Value.ToArgb().ToString("x", NumberFormatInfo.InvariantInfo));
                sb.Append("|");
            }
            return sb.ToString();
        }
        public Dictionary<string, string> parseStringDict(string str)
        {
            if (string.IsNullOrEmpty(str)) return new Dictionary<string, string>();
            if (str.EndsWith("|")) str = str.Substring(0, str.Length - 1);
            string[] parts = str.Split(new char[]{'|'},StringSplitOptions.None);

            if (parts.Length % 2 != 0) throw new ArgumentException("Invalid string! Must have an even number of parts.");
            Dictionary<string, string> dict = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                dict.Add(Base64UrlDecode(parts[i]), Base64UrlDecode(parts[i + 1]));
            }
            return dict;
        }
        public string serializeStringDict(Dictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> p in dict)
            {
                sb.Append(Base64UrlEncode(p.Key));
                sb.Append("|");
                sb.Append(Base64UrlEncode(p.Value != null ? p.Value : ""));
                sb.Append("|");
            }
            return sb.ToString();
        }


        public Dictionary<string, bool> parseBooleanDict(string str)
        {
            if (string.IsNullOrEmpty(str)) return new Dictionary<string, bool>();
            string[] parts = str.Trim('|').Split('|');
            if (parts.Length % 2 != 0) throw new ArgumentException("Invalid string! Must have an even number of parts.");
            Dictionary<string, bool> dict = new Dictionary<string, bool>();

            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                if (!(parts[i + 1].Equals("1") || parts[i + 1].Equals("0")))  throw new ArgumentException("Invalid bool value " + parts[i + 1] + ". Should be 0 or 1.");
                dict.Add(Base64UrlDecode(parts[i]), parts[i + 1].Equals("1"));
            }
            return dict;
        }
        
        public string serializeBooleanDict(Dictionary<string, bool> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, bool> p in dict)
            {
                sb.Append(Base64UrlEncode(p.Key));
                sb.Append("|");
                sb.Append(p.Value ? "1" : "0");
                sb.Append("|");
            }
            return sb.ToString();
        }




        public void Color(string layer, Color color, double opacity)
        {
            if (opacity > 1 || opacity < 0) throw new ArgumentOutOfRangeException("opacity","Cannot be less than 0 or greater than 1");
            byte alpha = (byte)(opacity * 255);

            Color(layer, System.Drawing.Color.FromArgb(alpha, color));
        }
        public void Color(string layer, Color color)
        {
            layerColors.Add(layer, color);
        }
        public void Show(string layer)
        {
            layerVisibility[layer] = true;
        }
        public void Hide(string layer)
        {
            layerVisibility[layer] = false;
        }
        public void Redraw(string layer) {
            layerRedraw[layer] = true;
        }

        public void SetText(string layer, string text) {
            this.layerText[layer] = text;
        }


        private string Base64UrlEncode(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return Convert.ToBase64String(new System.Text.UTF8Encoding().GetBytes(s)).Replace('+', '-').Replace('/', '_');
        }
        private string Base64UrlDecode(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return new System.Text.UTF8Encoding().GetString(Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/')));
        }

        public static IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "layerColors", "layerVisibility", "layerRedraw", "layerText", "renderer" };
        }
    }

    public class PsdCommandSearcher
    {
        PsdCommandBuilder b = null;
        string[] vKeys = null; //Wildcard keys
        string[] cKeys = null;//Wildcard keys
        string[] rKeys = null;//Wildcard keys
        string[] tKeys = null;//Wildcard keys
        public PsdCommandSearcher(PsdCommandBuilder b)
        {
            this.b = b;
            vKeys = b.layerVisibility.Keys.Where(key => key.Contains('*')).ToArray();
            rKeys = b.layerRedraw.Keys.Where(key => key.Contains('*')).ToArray();
            cKeys = b.layerColors.Keys.Where(key => key.Contains('*')).ToArray();
            tKeys = b.layerText.Keys.Where(key => key.Contains('*')).ToArray();
        }

        public Nullable<bool> getRedraw(string layer)
        {
            //Try case-insensitive exact match
            if (b.layerRedraw.ContainsKey(layer)) return b.layerRedraw[layer];
            //Try wildcard search
            string matchingKey = getFirstMatchingWildcard(layer, rKeys);

            if (matchingKey == null) return null;
            else return b.layerRedraw[matchingKey];
        }
        public string getReplacementText(string layer)
        {
            //Try case-insensitive exact match
            if (b.layerText.ContainsKey(layer)) return b.layerText[layer];
            //Try wildcard search
            string matchingKey = getFirstMatchingWildcard(layer, tKeys);

            if (matchingKey == null) return null;
            else return b.layerText[matchingKey];
        }
        public Nullable<bool> getVisibility(string layer)
        {
            //Try case-insensitive exact match
            if (b.layerVisibility.ContainsKey(layer)) return b.layerVisibility[layer];
            //Try wildcard search
            string matchingKey = getFirstMatchingWildcard(layer, vKeys);

            if (matchingKey == null) return null;
            else return b.layerVisibility[matchingKey];
        }
        public Nullable<Color> getColor(string layer)
        {
            //Try case-insensitive exact match
            if (b.layerColors.ContainsKey(layer)) return b.layerColors[layer];
            //Try wildcard search
            string matchingKey = getFirstMatchingWildcard(layer, cKeys);

            if (matchingKey == null) return null;
            else return b.layerColors[matchingKey];
        }

        public string getFirstMatchingWildcard(string layer, string[] wildcards)
        {
            foreach (string s in wildcards)
            {
                if (s.Equals("*")) return s; //Only used by unit tests

                string trimmed = s.Trim('*');
                //Contains query
                if (s.StartsWith("*") && s.EndsWith("*"))
                {
                    if (layer.IndexOf(trimmed,StringComparison.OrdinalIgnoreCase) > -1) return s;
                }else if (s.StartsWith("*")){
                    if (layer.EndsWith(trimmed, StringComparison.OrdinalIgnoreCase)) return s;
                }
                else if (s.EndsWith("*"))
                {
                    if (layer.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)) return s;
                }
            }
            return null;
        }
    }

}
