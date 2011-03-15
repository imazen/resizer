/*
* Copyright (c) 2006, Jonas Beckeman
* All rights reserved.
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of Jonas Beckeman nor the names of its contributors
*       may be used to endorse or promote products derived from this software
*       without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY JONAS BECKEMAN AND CONTRIBUTORS ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL JONAS BECKEMAN AND CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
* HEADER_END*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using PhotoshopFile.Text;

namespace PhotoshopFile
{

    /// <summary>
    /// Used for storing arbitrary structs and values; hierarchical
    /// </summary>
    public class DynVal
    {
        /// <summary>
        /// The OSType value for the current object (always 4 chars)
        /// </summary>
        [XmlAttributeAttribute()]
        public OSType Type = OSType.None;

        /// <summary>
        /// OSTypes (object types) in the PSD format
        /// </summary>
        public enum OSType
        {
            Reference, Descriptor, List, Double, UnitFloat, String, Enumerated, Integer, Boolean, Class, Alias, tdta, PropertyRef, ClassRef, EnumeratedRef, OffestRef,IdentifierRef,IndexRef,NameRef, None
        }
        public static OSType parseTypeString(string fourCharType)
        {
            switch (fourCharType)
            {
                case "obj ": return OSType.Reference;
                case "Objc": return OSType.Descriptor;
                case "VlLs": return OSType.List;
                case "doub": return OSType.Double;
                case "UntF": return OSType.UnitFloat;
                case "TEXT": return OSType.String;
                case "enum": return OSType.Enumerated;
                case "long": return OSType.Integer;
                case "bool": return OSType.Boolean;
                case "GlbO": return OSType.Descriptor;
                case "type": return OSType.Class;
                case "GlbC": return OSType.Class;
                case "alis": return OSType.Alias;
                case "tdta": return OSType.tdta;
            }
             throw new Exception("Unrecognized type " + fourCharType);
        }



        /// <summary>
        /// The classID value for the current object (usually 4 chars)
        /// </summary>
        [XmlAttributeAttribute()]
        public string classID;
       
        [XmlAttributeAttribute()]
        public string Name;
        [XmlAttributeAttribute()]
        public string UnicodeName;
        [XmlIgnoreAttribute()]
        public object Value;
        [XmlAttributeAttribute()]
        public string ValueForXml
        {
            get { if (this.Value == null) return null; return this.Value.ToString(); }
            set { }
        }
        public List<DynVal> Children; //TODO: 

        public String getString()
        {
            return getString("\n");
        }
        public String getString(string lineprefix)
        {
            string s = lineprefix +  "(" + this.Type.ToString()  + ") "+ this.Name + ":" + ((this.Value == null) ? "null" : this.Value.ToString());
            if (Children != null)
            {
                foreach (DynVal d in Children)
                {
                    s += d.getString(lineprefix + "\t");
                }
            }
            if (Value is Dictionary<string,object>){
                s += getString(lineprefix + "\t", Value);
            }
            return s;
        }

        public String getString(string lineprefix, object o)
        {
            if (o is Dictionary<string,object>){
                string s = "\n" + lineprefix + "{";
                foreach (KeyValuePair<string, object> p in (Dictionary<string,object>)o)
                {
                    s += "\n" + lineprefix + "\t" + p.Key + ":" + getString(lineprefix + "\t", p.Value) + ",";
                }
                s += "\n" + lineprefix + "}";
                return s;
            }
            if (o is List<object>){
                string s = "[";
                foreach (object a in (List<object>)o)
                {
                    s += getString(lineprefix, a);
                    s += ",";
                }
                s = s.TrimEnd(',') + "]";
                return s;
            }
            if (o is byte[]){
                return "(" + ReadableBinary.CreateHexEditorString((byte[])o) + ")";
            }
            return o.ToString();
        }

        [XmlAttributeAttribute()]
        public string ObjectType;

        public DynVal()
        { }

    

        public static DynVal ReadDescriptor(BinaryReverseReader r)
        {
            DynVal v = new DynVal();
            v.UnicodeName = r.ReadPSDUnicodeString();
            v.Type = OSType.Descriptor;
            v.classID = ReadSpecialString(r);
            v.Name = GetMeaningOfFourCC(v.classID);
            v.Children = DynVal.ReadValues(r, false);
            return v;
        }

        /// <summary>
        /// A peculiar type of ascii string frequently used throughout the Descriptor data structure.
        /// First 4 bytes are length (in bytes), followed by string. If length is 0, length is assumed to be 4. No idea why they did this... RLE compression?
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static string ReadSpecialString(BinaryReverseReader r)
        {
            uint length = r.ReadUInt32();
            if (length == 0)
                length = 4;
            return new string(r.ReadPSDChars((int)length));
        }
        /// <summary>
        /// Parses either a list format or a dictionary format. Specify skipKeys=true to parse the list format
        /// </summary>
        /// <param name="r"></param>
        /// <param name="skipKeys"></param>
        /// <returns></returns>
        public static List<DynVal> ReadValues(BinaryReverseReader r, bool skipKeys)
        {
            int numValues = (int)r.ReadUInt32();
            List<DynVal> values = new List<DynVal>(numValues);
            for (int i = 0; i < numValues; i++)
                values.Add(ReadValue(r, skipKeys));
            
            return values;
        }
        public static DynVal ReadValue(BinaryReverseReader r, bool skipKey)
        {
            DynVal vt = new DynVal();
            if (!skipKey)
                vt.Name = GetMeaningOfFourCC(ReadSpecialString(r));
            
            //TODO: should be assigned a sequential number?
            vt.Type = parseTypeString(new string(r.ReadPSDChars(4)));
            switch (vt.Type)
            {
                case  OSType.tdta:
                    
                    uint unknown = r.ReadUInt32();
                    TdTaParser p = new TdTaParser(r);
                    object o = p.ParseOneTree();
                    vt.Value = o;

                    break;
                case  OSType.Descriptor:
                    vt = DynVal.ReadDescriptor(r);
                    break;
                case OSType.List: 
                    vt.Children = ReadValues(r,true);
                    break;
                case OSType.Double:
                    vt.Value = r.ReadDouble();
                    break;
                case OSType.UnitFloat: //Unif float
                    //TODO: need a specific type for this, with a double and a type (percent/pixel)?
                    string tst = GetMeaningOfFourCC(new string(r.ReadPSDChars(4))); //#Prc #Pxl #Ang = percent / pixels / angle?
                    double d = r.ReadDouble();
                    tst += ": " + d;
                    vt.Value = tst;
                    break;
                case OSType.Enumerated:
                    string namesp = ReadSpecialString(r);
                    string item = ReadSpecialString(r);
                    //vt.Value = namesp + "." + item; //TODO: cast to real enum
                    vt.Value = GetMeaningOfFourCC(namesp) + "." + GetMeaningOfFourCC(item);
                    break;
                case OSType.Integer:
                    vt.Value = r.ReadInt32(); //4 byte integer
                    break;
                case OSType.Boolean:
                    vt.Value = r.ReadBoolean();
                    break;
                case  OSType.String:
                    vt.Value = r.ReadPSDUnicodeString();
                    break;

                default:
                    throw new Exception("Unhandled type: " + vt.Type);
            }
            return vt;
        }


  


   
        public static Dictionary<string, string> FourCCs;
        public static string GetMeaningOfFourCC(string fourCC)
        {
            if (FourCCs == null)
                LoadFourCC();
            if (fourCC.Length != 4)
                return fourCC;

            if (FourCCs.ContainsKey(fourCC))
                return FourCCs[fourCC];
            return fourCC;
        }
        public static void LoadFourCC()
        {
            string[] prefixes = new string[]{ "Key", "Enum", "Event", "Class", "Type" };
            System.Reflection.Assembly asm = Assembly.GetExecutingAssembly();
            System.IO.StreamReader sr = new System.IO.StreamReader(asm.GetManifestResourceStream("PhotoshopFile.FourCC.txt"));
            string contents = sr.ReadToEnd();
            FourCCs = new Dictionary<string, string>();
            string[] lines = contents.Split("\r\n".ToCharArray());
            foreach (string line in lines)
            {
                string[] items = line.Split('\t');
                if (items.Length <= 1)
                    continue;
                if (items[1].Length == 0)
                    continue;
                string name = items[0].Substring(2);
                foreach (string prefix in prefixes)
                {
                    if (name.StartsWith(prefix))
                    {
                        name = name.Substring(prefix.Length);
                        break;
                    }
                }
                FourCCs.Add(items[1].PadRight(4, ' '), name);
            }
        }
    }
}
