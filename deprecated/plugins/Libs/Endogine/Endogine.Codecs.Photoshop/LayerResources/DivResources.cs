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
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Endogine.Codecs.Photoshop.LayerResources
{
    [Description("smrs")]
    public class SomeResource : LayerResource
    {
        public SomeResource()
        {
        }
    }

    [Description("luni")]
    public class UnicodeName : LayerResource
    {
        [XmlAttributeAttribute()]
        public string Name;
        public UnicodeName()
        {
        }

        public UnicodeName(string name)
        {
            Name = name;
        }
        public UnicodeName(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Name = r.ReadPSDUnicodeString();
            //uint nUnicodeLength = r.ReadUInt32();
            //this.Name = new string(r.ReadPSDChars((int)nUnicodeLength * 2));
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.StartLengthBlock(typeof(uint));
            writer.Write(this.Name);
            writer.EndLengthBlock();
        }
    }

    [Description("lyid")]
    public class LayerId : LayerResource
    {
        [XmlAttributeAttribute()]
        public uint Id;
        public LayerId()
        {
        }
        public LayerId(uint id)
        {
            Id = id;
        }
        public LayerId(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Id = r.ReadUInt32();
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Id);
        }
    }

    [Description("fxrp")]
    public class ReferencePoint : LayerResource
    {
        public EPointF Point;
        public ReferencePoint()
        {
        }

        public ReferencePoint(EPointF point)
        {
            Point = point;
        }
        public ReferencePoint(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Point = new EPointF();
            this.Point.X = (float)r.ReadPSDDouble();
            this.Point.Y = (float)r.ReadPSDDouble();
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.WritePSDDouble((double)this.Point.X);
            writer.WritePSDDouble((double)this.Point.Y);
        }
    }

    [Description("clbl")]
    public class BlendClipping : LayerResource
    {
        [XmlAttributeAttribute()]
        public bool Value;
        public BlendClipping()
        {
        }
        public BlendClipping(bool clipping)
        {
            Value = clipping;
        }
        public BlendClipping(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Value = r.ReadBoolean();
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((byte)(this.Value ? 1 : 0));
        }
    }

    [Description("infx")]
    public class BlendElements : LayerResource
    {
        [XmlAttributeAttribute()]
        public bool Value;
        public BlendElements()
        {
        }
        public BlendElements(bool blend)
        {
            Value = blend;
        }
        public BlendElements(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Value = r.ReadBoolean();
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((byte)(this.Value ? 1 : 0));
        }
    }

    [Description("knko")]
    public class Knockout : LayerResource
    {
        [XmlAttributeAttribute()]
        public bool Value;
        public Knockout()
        {
        }
        public Knockout(bool knockout)
        {
            Value = knockout;
        }
        public Knockout(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Value = r.ReadBoolean();
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((byte)(this.Value ? 1 : 0));
        }
    }

    [Description("lspf")]
    public class Protected : LayerResource
    {
        [XmlAttributeAttribute()]
        public bool Settings;
        byte[] Unknown;
        //bits 0-2 = Transparency, composite and position
        public Protected()
        {
        }
        public Protected(bool settings)
        {
            Settings = settings;
        }
        public Protected(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            Unknown = r.ReadBytes((int)r.BytesToEnd);
            //this.Settings = r.ReadBoolean(); //4); //nLength?
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            //writer.Write((byte)(this.Settings ? 1 : 0));
            writer.Write(Unknown);
        }
    }


    [Description("lclr")]
    public class SheetColor : LayerResource
    {
        [XmlIgnoreAttribute()]
        public System.Drawing.Color Color;
        [XmlAttributeAttribute("Color")]
        public string ColorForXml
        {
            get { return this.Color.ToString(); }
            set { }
        }

        //bits 0-2 = Transparency, composite and position
        public SheetColor()
        {
        }
        public SheetColor(System.Drawing.Color color)
        {
            Color = color;
        }
        public SheetColor(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Color = System.Drawing.Color.FromArgb(
                r.ReadByte(),
                r.ReadByte(),
                r.ReadByte(),
                r.ReadByte());
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Color.A);
            writer.Write(this.Color.R);
            writer.Write(this.Color.G);
            writer.Write(this.Color.B);
        }
    }

    [Description("lnsr")]
    public class LayerNameSource : LayerResource
    {
        [XmlAttributeAttribute()]
        public string Name;
        //bits 0-2 = Transparency, composite and position
        public LayerNameSource()
        {
        }
        public LayerNameSource(string name)
        {
            Name = name;
        }
        public LayerNameSource(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            Name = new string(r.ReadPSDChars((int)r.BytesToEnd));
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Name);
        }
    }

    [Description("lsct")]
    public class SectionDivider : LayerResource
    {
        [XmlAttributeAttribute()]
        public uint Type;
        [XmlAttributeAttribute()]
        public string BlendKey;
        //bits 0-2 = Transparency, composite and position
        public SectionDivider()
        {
            //TODO: !!
        }
        public SectionDivider(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Type = r.ReadUInt32(); //0 = any other type of layer, 1 = open folder, 2 = closed folder, 3 = bounding section divider, hidden in the UI
            if (r.BytesToEnd > 0)
            {
                string header = new string(r.ReadPSDChars(4));
                this.BlendKey = new string(r.ReadPSDChars(4));
                if (this.BlendKey != "pass")
                {
                }
            }
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Type);
            if (this.BlendKey != null)
            {
                writer.Write("8BIM");
                writer.Write(this.BlendKey);
            }
        }
    }

    [Description("lfx2")]
    public class ObjectBasedEffects : LayerResource
    {
        public DynVal Descriptor;
        public List<DynVal> Values;

        public ObjectBasedEffects()
        {
        }
        public ObjectBasedEffects(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            r.ReadUInt32(); //Object version, Always 0

            this.Descriptor = new DynVal(r, true);
            //r.ReadUInt32(); //Descriptor version, 16 = PS6.0
            //string unknown = Endogine.Serialization.ReadableBinary.CreateHexEditorString(r.ReadBytes(6));
            //this.Values = DynVal.ReadValues(r);
            //Action Descriptor(s)
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            //writer.Write((uint)0);
            //writer.Write((uint)16);
            writer.Write(this.Data);
        }
    }


    [Description("Patt")]
    public class Patterns : LayerResource
    {
        public class Pattern
        {
            [XmlAttributeAttribute()]
            public ColorModes ColorMode;
            [XmlAttributeAttribute()]
            private EPoint Loc; //TODO: crashes on Xml serialization
            [XmlAttributeAttribute()]
            public string Name;
            [XmlAttributeAttribute()]
            public string Id;
            public string PaletteForXml;
            public string ImageData;
            public Pattern()
            { }

            public Pattern(BinaryPSDReader r)
            {
                long startPos = r.BaseStream.Position;

                uint length = r.ReadUInt32();
                uint version = r.ReadUInt32();
                this.ColorMode = (ColorModes)r.ReadUInt32();
                this.Loc = new EPoint(r.ReadUInt16(), r.ReadUInt16()); //TODO: signed??
                this.Name = r.ReadPSDUnicodeString();
                this.Id = r.ReadPascalString(); //?
                if (this.ColorMode == ColorModes.Indexed)
                {
                    this.PaletteForXml = "";
                    for (int i = 0; i < 256; i++)
                    {
                        string s = "";
                        for (int j = 0; j < 3; j++)
                            s += r.ReadByte().ToString("X");
                        this.PaletteForXml += s;
                    }
                }
                byte[] imageData = r.ReadBytes((int)(length - (int)r.BaseStream.Position - startPos));
                //TODO: what is the format?
                //System.IO.MemoryStream stream = new System.IO.MemoryStream(imageData);
                //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(stream);
                //this.ImageData = Endogine.Serialization.ReadableBinary.CreateHexEditorString(imageData);

                //TODO: length isn't correct! By 6 bytes always??
                if (r.BytesToEnd < 20)
                    r.BaseStream.Position = r.BaseStream.Length;
            }
        }

        public List<Pattern> Records;

        public Patterns()
        {
        }
        public Patterns(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Records = new List<Pattern>();
            while (r.BytesToEnd > 0)
            {
                this.Records.Add(new Pattern(r));
            }
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Data);
        }
    }

    [Description("Txt2")]
    public class Txt2 : LayerResource
    {
        [XmlAttributeAttribute()]
        public string Settings
        { get { return "EXTREMELY bloated data - 280 kB or more - to inspect, change to 'readData = true' in Txt2 class"; } set { } }
        public DynVal Values;

        public Txt2()
        {
            //TODO: unknown specification!! Seems to be a DynVal, Tdta type of resource... Bloated like hell.
        }
        public Txt2(BinaryPSDReader reader)
            : base(reader)
        {
            bool readData = false; //Set to true to see it in all its... glory.
            if (readData)
            {
                BinaryPSDReader r = this.GetDataReader();
                r.BaseStream.Position += 2;
                this.Values = new DynVal();
                this.Values.Children = new List<DynVal>();
                while (true)
                {
                    DynVal child = new DynVal();
                    this.Values.Children.Add(child);
                    try
                    {
                        if (child.ReadTdtaItem(r) == false)
                            break;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            this.Data = null;
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Data);
        }
    }

    //case "Anno":
    //    //Annotations

    //case "grdm":
    //    //Gradient settings

}
