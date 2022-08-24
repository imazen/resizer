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

namespace Endogine.Codecs.Photoshop
{
    /// <summary>
    /// Used for storing arbitrary structs and values; hierarchical
    /// </summary>
    public class DynVal
    {
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

        [XmlAttributeAttribute()]
        public string ObjectType;

        public DynVal()
        { }
        public DynVal(BinaryPSDReader r, bool asDescriptor)
        {
            if (asDescriptor)
            {
                uint version = r.ReadUInt32();
                string unknown = Endogine.Serialization.ReadableBinary.CreateHexEditorString(r.ReadBytes(6));
            }
            this.ObjectType = GetMeaningOfFourCC(ReadSpecialString(r));
            this.Children = DynVal.ReadValues(r);
        }


        public static string ReadSpecialString(BinaryPSDReader r)
        {
            uint length = r.ReadUInt32();
            if (length == 0)
                length = 4;
            return new string(r.ReadPSDChars((int)length));
        }
        public static List<DynVal> ReadValues(BinaryPSDReader r)
        {
            int numValues = (int)r.ReadUInt32();
            List<DynVal> values = new List<DynVal>();
            for (int i = 0; i < numValues; i++)
            {
                DynVal vt = ReadValue(r, false);
                if (vt != null)
                    values.Add(vt);
            }
            return values;
        }
        public static DynVal ReadValue(BinaryPSDReader r, bool ignoreName)
        {
            DynVal vt = new DynVal();
            if (!ignoreName)
                vt.Name = GetMeaningOfFourCC(ReadSpecialString(r)); // r.ReadPascalString();
            //else
            //TODO: should be assigned a sequential number?
            string type = new string(r.ReadPSDChars(4));

            switch (type)
            {
                case "tdta":
                    vt.Value = Endogine.Serialization.ReadableBinary.CreateHexEditorString(r.ReadBytes(9));
                    vt.Children = new List<DynVal>();
                    while (true)
                    {
                        DynVal child = new DynVal();
                        vt.Children.Add(child);
                        if (child.ReadTdtaItem(r) == false)
                            break;
                    }

                    //r.BaseStream.Position += 9;
                    break;
                case "Objc": //Decriptor
                case "GlbO": //GlobalObject (same)
                    string uniName = r.ReadPSDUnicodeString();
                    //uint numSub = r.ReadUInt32();
                    //if (numSub > 1)
                    //{
                    //    //A unicode text here!? What does this have to do with numSub??
                    //    r.BaseStream.Position -= 4;
                    //    r.ReadPSDUnicodeString();
                    //    r.BaseStream.Position -= 2; //Ehh... What?!
                    //}
                    ////TODO: ah: 1 = 1 short = unknown...
                    //ushort unknown = r.ReadUInt16();
                    //vt.Children = ReadValues(r);
                    vt = new DynVal(r, false);
                    if (uniName.Length > 0)
                        vt.UnicodeName = uniName;
                    break;
                case "VlLs": //List
                    vt.Children = new List<DynVal>();
                    int numValues = (int)r.ReadUInt32();
                    for (int i = 0; i < numValues; i++)
                    {
                        DynVal ob = ReadValue(r, true);
                        if (ob != null)
                            vt.Children.Add(ob);
                    }
                    break;
                case "doub":
                    vt.Value = r.ReadPSDDouble();
                    break;
                case "UntF": //Unif float
                    //TODO: need a specific type for this, with a double and a type (percent/pixel)?
                    string tst = GetMeaningOfFourCC(new string(r.ReadPSDChars(4))); //#Prc #Pxl #Ang = percent / pixels / angle?
                    double d = r.ReadPSDDouble();
                    tst += ": " + d;
                    vt.Value = tst;
                    break;
                case "enum":
                    string namesp = ReadSpecialString(r);
                    string item = ReadSpecialString(r);
                    //vt.Value = namesp + "." + item; //TODO: cast to real enum
                    vt.Value = GetMeaningOfFourCC(namesp) + "." + GetMeaningOfFourCC(item);
                    break;
                case "long":
                    vt.Value = r.ReadInt32(); //64?
                    break;
                case "bool":
                    vt.Value = r.ReadBoolean();
                    break;
                //case "obj ": //reference
                //    break;
                case "TEXT":
                    vt.Value = r.ReadPSDUnicodeString();
                    break;
                //case "Enmr": //Enumerated
                //    break;
                //case "Clss": //Class
                //    break;
                //case "GlbC": //GlobalClass
                //    break;
                //case "alis": //Alias
                //    break;
                default:
                    throw new Exception("Unknown type: " + type);
            }
            if (vt.Value == null && vt.Children == null)
                return null;
            return vt;
        }

        public bool ReadTdtaItem(BinaryPSDReader r)
        {
            this.Name = "";
            while (r.BytesToEnd > 0)
            {
                char c = r.ReadChar();
                if (c == 0x0a)
                    break;
                this.Name += c;
            }

            byte[] buffer = new byte[255];
            int bufPos = 0;
            int nearEndCnt = 0;

            //Read until a slash or several 0x00 comes along
            while (r.BytesToEnd > 0)
            {
                byte b = r.ReadByte();
                buffer[bufPos++] = b;
                if (b == 0x2f) // slash "/" seems to be the field delimiter
                    break;
                if (b <= 0x00)
                {
                    nearEndCnt++;
                    if (nearEndCnt == 12)
                        break;
                }
                else
                    nearEndCnt = 0;
            }

            if (this.Name.Contains(" "))
            {
                int index = this.Name.IndexOf(" ");
                string val = this.Name.Substring(index + 1);
                //Sometimes it's a unicode string (how do I know..? seems to always have parenthesis) Ugly shortcut:
                if (val[0] == '(' && val[val.Length - 1] == ')')
                {
                    bool unicode = true;
                    for (int i = 0; i < val.Length; i ++)
                        if (val[i] != 0 && val[i] <= 31)
                        {
                            unicode = false;
                            break;
                        }

                    if (unicode)
                    {
                        string uniVal = "";
                        for (int i = 2; i < val.Length; i += 2)
                            uniVal += val[i];
                        val = uniVal;
                    }
                    else
                    {
                        byte[] tmp = new byte[val.Length];
                        int i = 0;
                        foreach (char ch in val)
                            tmp[i++] = (byte)ch;
                        val = Endogine.Serialization.ReadableBinary.CreateHexEditorString(tmp).Replace("\r\n", " ");
                    }
                }
                this.Value = val;
                
                this.Name = this.Name.Remove(index);
            }

            int endPos = bufPos - nearEndCnt - 1;
            //See if it's only 0x09's:
            for (int i = 0; i < endPos; i++)
            {
                if (buffer[i] != 0x09)
                {
                    //TODO: this.Data = Endogine.Serialization.ReadableBinary.CreateHexEditorString(buffer, 0, endPos);
                    break;
                }
            }

            return (nearEndCnt == 0 && r.BytesToEnd > 0); //OK to continue reading values?
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
            System.IO.StreamReader sr = new System.IO.StreamReader("FourCC.txt");
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
