/* Copyright (c) 2014 Imazen See license.txt for your rights */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;

namespace PhotoshopFile.Text
{
    public class TdTaParser
    {
        private static readonly Regex arrayNotation = new Regex("^\\[(?<index>\\d+)\\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex dotNotation = new Regex("^\\.?(?<key>[^\\.\\[\\]\\@\\$]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);


        public static Dictionary<string, object> getDict(object tree, string selector) { return (Dictionary<string, object>)query(tree, selector); }
        public static List<object> getList(object tree, string selector) { return (List< object>)query(tree, selector); }
        public static string getString(object tree, string selector) { return (string)query(tree, selector); }
        public static bool getBool(object tree, string selector) { return (bool)query(tree, selector); }

        public static Color getColor(object tree, string selector)
        {
            /*{
                    Type:1,
                    Values:[1,0,0.20001,0.79999],
            }*/
            //Get the color object
            Dictionary<string, object> d = getDict(tree, selector);
            //Type should always be one, we don't know how to parse anything else.
            Debug.Assert((int)d["Type"] == 1); 
            //Get the array of values
            List<object> values = d["Values"] as List<object>;
            return Color.FromArgb((int)((double)values[0] * 255),(int)((double)values[1] * 255),(int)((double)values[2] * 255),(int)( (double)values[3] * 255));
        }
        /// <summary>
        /// Navigates a tree, returning the selected object. supports dot and array notation. A trailing $ means to convert the byte array to a string.
        /// Objects can be primitive types, byte[] arrays, Dict(string,object), or List(objct)
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static object query(object tree, string selector)
        {
            //Null or empty gets the current obj.
            if (string.IsNullOrEmpty(selector)) return tree;

            Match m = null;
            //Check for dot notation
            m = dotNotation.Match(selector);
            if (m != null && m.Success)
            {
                string key = m.Groups["key"].Value;
                return query(((Dictionary<string,object>)tree)[key], selector.Substring(m.Length));
            }

            //Check for array notation
            m = arrayNotation.Match(selector);
            if (m != null && m.Success)
            {
                int index = int.Parse(m.Groups["index"].Value,NumberStyles.Integer,NumberFormatInfo.InvariantInfo);
                return query(((List<object>)tree)[index], selector.Substring(m.Length));
            }

            //Check for string notation
            if (selector.Equals("$"))
            {
                byte[] b = (byte[])tree;
                //FEFF - big endian
                if (b[0] == 254 && b[1] == 255) return System.Text.UTF8Encoding.BigEndianUnicode.GetString(b,2,b.Length -2);
                //FFEF - little endian
                if (b[0] == 255 && b[1] == 254) return System.Text.UTF8Encoding.Unicode.GetString(b, 2, b.Length - 2);
                throw new Exception("Failed to find Byte Order mark in string! :" + System.Text.UTF8Encoding.BigEndianUnicode.GetString(b));
            }
            throw new Exception("What does this selector mean? " + selector);
        }
        /// <summary>
        /// Merges two tdta dictionaries. Overlays items from second onto first. Recursive for child dicts. Arrays are simply replaced. 
        /// Resulting item contains only references to shared immutable objects.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Dictionary<string, object> MergeObjects(object first, object second)
        {
            Dictionary<string, object> n = (Dictionary<string, object>)copy(first);
            Dictionary<string, object> overlay = (Dictionary<string, object>)(second);
            foreach (KeyValuePair<string, object> p in overlay)
            {
                if (p.Value is Dictionary<string, object> && n.ContainsKey(p.Key))
                    //Recursive hild merge.
                    n[p.Key] = MergeObjects(n[p.Key], overlay[p.Key]);
                else
                    //Overwrite with deep copy.
                    n[p.Key] = copy(overlay[p.Key]); 
            }
            return n;
        }
        /// <summary>
        /// Deeop copies trees. Supports byte[], int, string, double, bool, dict(str,obj),list(obj)
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object copy(object o)
        {
            if (o is double || o is string || o is int || o is bool) return o; // No cloning needed for immutable types.
            if (o is byte[]) return ((byte[])o).ToArray();
            if (o is List<object>)
            {
                List<object> old = (List<object>)o;
                List<object> n = new List<object>(old.Count);
                foreach (object c in old)
                    n.Add(copy(c));
                return n;
            }
            if (o is Dictionary<string, object>)
            {
                Dictionary<string, object> old = (Dictionary<string, object>)o;
                Dictionary<string, object> n = new Dictionary<string, object>(old.Count);
                foreach (KeyValuePair<string,object> p in old)
                    n.Add(p.Key,copy(p.Value));
                return n;
            }
            throw new Exception("Unknown object type " + o);
        }


        BinaryReverseReader r;
        public TdTaParser(BinaryReverseReader r)
        {
            this.r = r;
        }
        /// <summary>
        /// Parses one tree from the stream. If there are multiple root nodes, you may have to call this multiple times, but you will get an exception if the stream is empty.
        /// </summary>
        /// <returns></returns>
        public object ParseOneTree()
        {
            return parse();
        }

        protected Token ReadToken()
        {
            return Token.nextToken(r);
        }
        protected Token ReadTokenIgnoreWhitespace()
        {
            Token t = ReadToken();
            //Fast forward through whitespace tokens
            while (t.type == Token.TokenType.Whitespace) { t = ReadToken(); }
            return t;
        }
        /// <summary>
        /// Can start parsing anywhere, as long as it isn't before a map key.
        /// </summary>
        /// <returns></returns>
        protected object parse()
        {
            //Ignore whitespace
            Token t = ReadTokenIgnoreWhitespace();
            //Return the primitive value types
            if (t.IsValueType) return t.value;
            if (t.type == Token.TokenType.StartList) return parseList();
            if (t.type == Token.TokenType.StartDict) return parseDict();
            if (t.type == Token.TokenType.EndList || t.type == Token.TokenType.EndDict) return t;
            throw new TdTaParseException("Unexpected token " + t);
        }
        /// <summary>
        /// Parses a list of objects from the stream. Assumes opening token was alread read.
        /// </summary>
        /// <returns></returns>
        protected List<object> parseList()
        {
            List<object> list = new List<object>();

            while (true)
            {
                object o = parse(); //Parse an object
                if (o is Token)
                {
                    //Must be end of list token, any other is an error
                    if (((Token)o).type != Token.TokenType.EndList)
                        throw new TdTaParseException("Unexpected token " + (Token)o + " in list");
                    //end of list
                    return list;
                }
                else
                {
                    list.Add(o);
                }
            }
        }
        /// <summary>
        /// Parses a dictionary (map,object) from the stream. Assumes opening token was alread read.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string,object> parseDict()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            while (true)
            {
                //parse key
                //Ignore whitespace
                Token key = ReadTokenIgnoreWhitespace();

                //Check for end of dict
                if (key.type == Token.TokenType.EndDict) return dict;
                
                //Check for invalid token
                if (key.type != Token.TokenType.MapKey)
                    throw new TdTaParseException("Unexpected token " + key + " in dictionary, expected a dictionary key like /key");

                //Parse the value (as an object) (can be another dict, will handle itself)
                object o = parse(); 

                //Shouldn't be a token
                if (o is Token) throw new TdTaParseException("Unexpected token " + (Token)o + " in dict! Expected value.");
                
                //Add pair
                dict.Add((string)key.value, o);
            }
        }
    }
}
