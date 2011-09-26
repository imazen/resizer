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
using System.ComponentModel;
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop
{
    [XmlInclude(typeof(EffectBase))]
    [XmlInclude(typeof(LayerResources.BlendClipping))]
    [XmlInclude(typeof(LayerResources.BlendElements))]
    [XmlInclude(typeof(LayerResources.BrightnessContrast))]
    [XmlInclude(typeof(LayerResources.ChannelMixer))]
    [XmlInclude(typeof(LayerResources.ColorBalance))]
    [XmlInclude(typeof(LayerResources.Curves))]
    [XmlInclude(typeof(LayerResources.Effects))]
    [XmlInclude(typeof(LayerResources.GradientFill))]
    [XmlInclude(typeof(LayerResources.GradientMap))]
    [XmlInclude(typeof(LayerResources.HueSaturation))]
    [XmlInclude(typeof(LayerResources.Invert))]
    [XmlInclude(typeof(LayerResources.Knockout))]
    [XmlInclude(typeof(LayerResources.LayerId))]
    [XmlInclude(typeof(LayerResources.LayerNameSource))]
    [XmlInclude(typeof(LayerResources.Levels))]
    [XmlInclude(typeof(LayerResources.ObjectBasedEffects))]
    [XmlInclude(typeof(LayerResources.PatternFill))]
    [XmlInclude(typeof(LayerResources.Patterns))]
    [XmlInclude(typeof(LayerResources.PhotoFilter))]
    [XmlInclude(typeof(LayerResources.Posterize))]
    [XmlInclude(typeof(LayerResources.Protected))]
    [XmlInclude(typeof(LayerResources.ReferencePoint))]
    [XmlInclude(typeof(LayerResources.SectionDivider))]
    [XmlInclude(typeof(LayerResources.SelectiveColor))]
    [XmlInclude(typeof(LayerResources.SheetColor))]
    [XmlInclude(typeof(LayerResources.SolidColor))]
    [XmlInclude(typeof(LayerResources.Threshold))]
    [XmlInclude(typeof(LayerResources.Txt2))]
    [XmlInclude(typeof(LayerResources.TypeTool))]
    [XmlInclude(typeof(LayerResources.TypeToolObject))]
    [XmlInclude(typeof(LayerResources.UnicodeName))]
    public class LayerResource
    {
        public static Dictionary<string, Type> ResourceTypes;

        string _tag;
        [XmlAttributeAttribute()] //XmlIgnoreAttribute XmlAttributeAttribute
        public virtual string Tag
        {
            get { if (this._tag == null) this._tag = this.ExtractTag(); return this._tag; }
            set { this._tag = value; }
        }
        [XmlIgnoreAttribute()]
        public string Category
        {
            get { return this.ExtractCategory(); }
        }

        [XmlIgnoreAttribute()]
        public byte[] Data;
        public string DataForXml
        {
            get
            {
                if (this.Data == null)
                    return null;
                return Endogine.Serialization.ReadableBinary.CreateHexEditorString(this.Data);
            }
            set { }
        }


        public LayerResource()
        {
        }

        //public LayerResource(LayerResource res)
        //{
        //    Prepare();
        //    this.Tag = res.Tag;
        //}

        public LayerResource(BinaryPSDReader reader)
        {
            //this.Tag = new string(reader.ReadPSDChars(4));
            uint settingLength = reader.ReadUInt32();
            this.Data = reader.ReadBytes((int)settingLength);
            if (!(this is EffectBase))
                reader.JumpToEvenNthByte(2);
        }

        public BinaryPSDReader GetDataReader()
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream(this.Data);
            return new BinaryPSDReader(stream);
        }

        public void Write(BinaryPSDWriter writer)
        {
            writer.Write("8BIM");
            writer.Write(this.Tag);
            writer.StartLengthBlock(typeof(uint));
            this.SubWrite(writer);
            writer.EndLengthBlock();
            if (!(this is EffectBase))
                writer.PadToNextMultiple(2);
        }

        protected virtual void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Data);
        }

        protected string ExtractTag()
        {
            AttributeCollection attribs = TypeDescriptor.GetAttributes(this.GetType());
            DescriptionAttribute attr = (DescriptionAttribute)attribs[typeof(DescriptionAttribute)];
            return attr.Description;
        }
        protected string ExtractCategory()
        {
            AttributeCollection attribs = TypeDescriptor.GetAttributes(this.GetType());
            CategoryAttribute attr = (CategoryAttribute)attribs[typeof(CategoryAttribute)];
            return attr.Category;
        }




        public static void Prepare()
        {
            if (ResourceTypes != null)
                return;

            //Get ImageResource types
            //TODO: is there a way to not get *all* types, but only those in a sub-namespace?

            ResourceTypes = new Dictionary<string, Type>();
            Type[] types = typeof(LayerResource).Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.FullName.Contains("LayerResources."))
                {
                    AttributeCollection attribs = TypeDescriptor.GetAttributes(type);
                    DescriptionAttribute descAtt = (DescriptionAttribute)attribs[typeof(DescriptionAttribute)];
                    //CategoryAttribute catAtt = (CategoryAttribute)attribs[typeof(CategoryAttribute)];
                    if (descAtt != null && !string.IsNullOrEmpty(descAtt.Description))
                    {
                        string[] split = descAtt.Description.Split(',');
                        foreach (string s in split)
                        {
                            if (s.Length == 0)
                                continue;
                            ResourceTypes.Add(s, type); //descAtt.Description
                        }
                    }
                }
            }
        }

        public static Dictionary<string, LayerResource> ReadLayerResources(BinaryPSDReader reader, Type inheritsType)
        {
            Prepare();

            Dictionary<string, LayerResource> result = new Dictionary<string, LayerResource>();
            while (true)
            {
                LayerResource res = ReadLayerResource(reader, inheritsType);
                if (res == null)
                    break;
                result.Add(res.Tag, res);
            }
            return result;
        }

        public static LayerResource ReadLayerResource(BinaryPSDReader reader, Type inheritsType)
        {
            long posBefore = reader.BaseStream.Position;
            string sHeader = new string(reader.ReadPSDChars(4));
            if (sHeader != "8BIM")
            {
                reader.BaseStream.Position = posBefore; //back it up
                return null;
            }

            string tag = new string(reader.ReadPSDChars(4));
            Type type = null;
            bool usingDefault = false;
            if (!ResourceTypes.ContainsKey(tag))
            {
                if (inheritsType == null)
                    inheritsType = typeof(LayerResource);
                //throw new Exception("LayerResource tag unknown: " + tag);
                type = inheritsType;
                usingDefault = true;
                //LayerResource temp = new LayerResource(reader);
                //temp.Tag = tag;
                //return temp;
            }
            else
                type = ResourceTypes[tag];

            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(BinaryPSDReader) });
            LayerResource res = (LayerResource)ci.Invoke(new object[] { reader });

            res.Tag = tag;

            return res;
        }

        public static LayerResource Create(Type type)
        {
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { });
            LayerResource res = (LayerResource)ci.Invoke(new object[] { });
            return res;
        }

        public static List<LayerResource> GetFlatResources(Dictionary<string, LayerResource> resources)
        {
            if (resources.Count == 0)
                return null;
            
            List<LayerResource> res = new List<LayerResource>();
            foreach (LayerResource r in resources.Values)
                res.Add(r);
            return res;
        }
    }
}
