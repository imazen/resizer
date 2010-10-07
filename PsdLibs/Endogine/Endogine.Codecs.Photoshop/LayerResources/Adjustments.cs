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
using System.ComponentModel;

namespace Endogine.Codecs.Photoshop.LayerResources
{
    [Description("blnc")]
    public class ColorBalance : LayerResource
    {
        public class RangeSettings
        {
            [XmlAttributeAttribute()]
            public List<short> Values;

            public RangeSettings()
            { }
            public RangeSettings(BinaryPSDReader r)
            {
                this.Values = new List<short>();
                for (int i = 0; i < 3; i++)
                    this.Values.Add(r.ReadInt16());
            }
        }

        public List<RangeSettings> Records;

        public ColorBalance()
        {
        }

        public ColorBalance(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Records = new List<RangeSettings>();
            for (int i = 0; i < 3; i++)
            {
                this.Records.Add(new RangeSettings(r));
            }

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("brit")]
    public class BrightnessContrast : LayerResource
    {
        [XmlAttributeAttribute()]
        public short Brightness;
        [XmlAttributeAttribute()]
        public short Contrast;

        public BrightnessContrast()
        {
        }

        public BrightnessContrast(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Brightness = r.ReadInt16();
            this.Contrast = r.ReadInt16();

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("hue2")] //TODO: "hue " also
    public class HueSaturation : LayerResource
    {
        public class HSLModifier
        {
            [XmlAttributeAttribute()]
            public short Hue;
            [XmlAttributeAttribute()]
            public short Saturation;
            [XmlAttributeAttribute()]
            public short Lightness;
            [XmlAttributeAttribute()]
            public List<short> Ranges;

            public HSLModifier()
            { }
            public HSLModifier(BinaryPSDReader r)
            {
                this.Hue = r.ReadInt16();
                this.Saturation = r.ReadInt16();
                this.Lightness = r.ReadInt16();
            }

        }
        [XmlAttributeAttribute()]
        public bool ColorizeMode;
        public HSLModifier Colorize;
        //public Endogine.ColorEx.ColorHsb Colorize;
        public List<HSLModifier> Settings;

        public HueSaturation()
        {
        }

        public HueSaturation(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort version = r.ReadUInt16();
            this.ColorizeMode = r.ReadBoolean();
            r.BaseStream.Position += 1; //padding

            this.Settings = new List<HSLModifier>();
            if (version == 1)
            {
                this.Colorize = new HSLModifier();
                this.Colorize.Hue = (short)((int)r.ReadInt16() * 180 / 100);
                this.Colorize.Saturation = r.ReadInt16();
                this.Colorize.Lightness = r.ReadInt16();

                for (int i = 0; i < 7; i++)
                {
                    HSLModifier hsl = new HSLModifier();
                    this.Settings.Add(hsl);
                    hsl.Hue = r.ReadInt16();
                }
                for (int i = 0; i < 7; i++)
                    this.Settings[i].Saturation = r.ReadInt16();
                for (int i = 0; i < 7; i++)
                    this.Settings[i].Lightness = r.ReadInt16();

            }
            else if (version == 2)
            {
                this.Colorize = new HSLModifier(r);

                HSLModifier hsl = new HSLModifier(r); //master
                this.Settings.Add(hsl);

                for (int i = 0; i < 6; i++)
                {
                    List<short> ranges = new List<short>();
                    for (int j = 0; j < 4; j++)
                        ranges.Add(r.ReadInt16());
                    hsl = new HSLModifier(r);
                    this.Settings.Add(hsl);
                    hsl.Ranges = ranges;
                }
            }

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("GdFl")]
    public class GradientFill : LayerResource
    {
        public DynVal Descriptor;
        //public List<DynVal> Values;

        public GradientFill()
        {
        }

        public GradientFill(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();
            this.Descriptor = new DynVal(r, true);
            //uint version = r.ReadUInt32();
            //string unknown = Endogine.Serialization.ReadableBinary.CreateHexEditorString(r.ReadBytes(6));
            //this.Values = DynVal.ReadValues(r);

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("SoCo")]
    public class SolidColor : LayerResource
    {
        public DynVal Descriptor; // List<DynVal> Values;

        public SolidColor()
        {
        }

        public SolidColor(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Descriptor = new DynVal(r, true);
            //uint version = r.ReadUInt32();
            //string unknown = Endogine.Serialization.ReadableBinary.CreateHexEditorString(r.ReadBytes(6));
            //this.Values = DynVal.ReadValues(r);

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("PtFl")]
    public class PatternFill : LayerResource
    {
        public DynVal Descriptor; //public List<DynVal> Values;
        
        public PatternFill()
        {
        }
        public PatternFill(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Descriptor = new DynVal(r, true);

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("selc")]
    public class SelectiveColor : LayerResource
    {
        public class CorrectionRecord
        {
            [XmlAttributeAttribute()]
            public short Cyan;
            [XmlAttributeAttribute()]
            public short Magenta;
            [XmlAttributeAttribute()]
            public short Yellow;
            [XmlAttributeAttribute()]
            public short Black;

            public CorrectionRecord()
            { }
            public CorrectionRecord(BinaryPSDReader r)
            {
                this.Cyan = r.ReadInt16();
                this.Magenta = r.ReadInt16();
                this.Yellow = r.ReadInt16();
                this.Black = r.ReadInt16();
            }
        }

        public bool AbsoluteMode;
        public List<CorrectionRecord> Records;

        public SelectiveColor()
        {
        }
        public SelectiveColor(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort version = r.ReadUInt16();
            r.BaseStream.Position += 1;
            this.AbsoluteMode = r.ReadBoolean();

            this.Records = new List<CorrectionRecord>();
            for (int i = 0; i < 10; i++)
                this.Records.Add(new CorrectionRecord(r));

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("mixr")]
    public class ChannelMixer : LayerResource
    {
        public class MixerRecord
        {
            [XmlAttributeAttribute()]
            public List<short> Channels;
            [XmlAttributeAttribute()]
            public short Constant;
            [XmlAttributeAttribute()]
            public short Unknown;

            public MixerRecord()
            { }
            public MixerRecord(BinaryPSDReader r, int numChannels)
            {
                this.Channels = new List<short>();
                for (int i = 0; i < numChannels; i++)
                    this.Channels.Add(r.ReadInt16());

                this.Unknown = r.ReadInt16();
                this.Constant = r.ReadInt16();
            }
        }

        public List<MixerRecord> Records;
        [XmlAttributeAttribute()]
        public bool Monochrome;

        public ChannelMixer()
        {
        }
        public ChannelMixer(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort version = r.ReadUInt16();
            r.BaseStream.Position += 1;
            this.Monochrome = r.ReadBoolean();
            //TODO: probably dependent on document.Channels
            this.Records = new List<MixerRecord>();
            int numChannels = 3;
            if (this.Monochrome)
                this.Records.Add(new MixerRecord(r, numChannels));
            else
            {
                for (int i = 0; i < numChannels; i++)
                    this.Records.Add(new MixerRecord(r, numChannels));
            }

            //Hmm, doesn't make sense... Why keep Monochrome if it isn't used..?
            if (r.BytesToEnd > 0 && !this.Monochrome)
                this.Records.Add(new MixerRecord(r, numChannels));

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("grdm")]
    public class GradientMap : LayerResource
    {
        public class ColorStop
        {
            [XmlAttributeAttribute()]
            public uint Location;
            [XmlAttributeAttribute()]
            public uint Midpoint;
            [XmlAttributeAttribute()]
            public ColorModes ColorMode;
            [XmlAttributeAttribute()]
            public List<ushort> Channels;
            [XmlAttributeAttribute()]
            public ushort Unknown;
            
            public ColorStop()
            { }
            public ColorStop(BinaryPSDReader r)
            {
                this.Location = r.ReadUInt32();
                this.Midpoint = r.ReadUInt32();
                this.ColorMode = (ColorModes)r.ReadUInt16();

                this.Channels = r.ReadPSDChannelValues(4);

                this.Unknown = r.ReadUInt16();
            }
        }

        public class TransparencyStop
        {
            [XmlAttributeAttribute()]
            public uint Location;
            [XmlAttributeAttribute()]
            public uint Midpoint;
            [XmlAttributeAttribute()]
            public ushort Opacity;

            public TransparencyStop()
            { }
            public TransparencyStop(BinaryPSDReader r)
            {
                this.Location = r.ReadUInt32();
                this.Midpoint = r.ReadUInt32();
                this.Opacity = r.ReadUInt16();
            }

        }

        [XmlAttributeAttribute()]
        public bool Reverse;
        [XmlAttributeAttribute()]
        public bool Dither;
        [XmlAttributeAttribute()]
        public string Name;
        public List<ColorStop> ColorStops;
        public List<TransparencyStop> TransparencyStops;
        [XmlAttributeAttribute()]
        public short Interpolation;
        [XmlAttributeAttribute()]
        public ushort Mode;
        [XmlAttributeAttribute()]
        public uint RandomSeed;
        [XmlAttributeAttribute()]
        public bool ShowTransparency;
        [XmlAttributeAttribute()]
        public bool UseVectorColor;
        [XmlAttributeAttribute()]
        public uint Roughness;
        [XmlAttributeAttribute()]
        public ushort ColorModel;
        [XmlAttributeAttribute()]
        public List<ushort> MinChannelValues;
        [XmlAttributeAttribute()]
        public List<ushort> MaxChannelValues;


        public GradientMap()
        {
        }
        public GradientMap(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort version = r.ReadUInt16();
            this.Reverse = r.ReadBoolean();
            this.Dither = r.ReadBoolean();
            this.Name = r.ReadPSDUnicodeString();
            r.JumpToEvenNthByte(2);

            ushort cnt = r.ReadUInt16();
            this.ColorStops = new List<ColorStop>();
            for (int i = 0; i < cnt; i++)
                this.ColorStops.Add(new ColorStop(r));

            cnt = r.ReadUInt16();
            this.TransparencyStops = new List<TransparencyStop>();
            for (int i = 0; i < cnt; i++)
                this.TransparencyStops.Add(new TransparencyStop(r));

            ushort expansionCount = r.ReadUInt16();
            if (expansionCount > 0)
                this.Interpolation = r.ReadInt16();

            ushort length = r.ReadUInt16();
            this.Mode = r.ReadUInt16();
            this.RandomSeed = r.ReadUInt32();
            
            r.BaseStream.Position += 1;
            this.ShowTransparency = r.ReadBoolean();

            r.BaseStream.Position += 1;
            this.UseVectorColor = r.ReadBoolean();

            this.Roughness = r.ReadUInt32();

            this.ColorModel = r.ReadUInt16(); //ColorMode?!

            this.MinChannelValues = r.ReadPSDChannelValues(4);
            this.MaxChannelValues = r.ReadPSDChannelValues(4);

            this.Data = r.ReadBytes((int)r.BytesToEnd);
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("phfl")]
    public class PhotoFilter : LayerResource
    {
        [XmlAttributeAttribute()]
        public ushort Density;
        [XmlAttributeAttribute()]
        public bool PreserveLuminosity;

        public PhotoFilter()
        {
        }
        public PhotoFilter(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            //uint version = r.ReadUInt32();
            //TODO: no idea how to interpret colors... Not RGB or CMYK anyway!
            r.BaseStream.Position += 16;
            this.Density = r.ReadUInt16();
            this.PreserveLuminosity = r.ReadBoolean();
            //this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("nvrt")]
    public class Invert : LayerResource
    {
        public Invert()
        {
        }
        public Invert(BinaryPSDReader reader)
            : base(reader)
        {
        }
    }

    [Description("thrs")]
    public class Threshold : LayerResource
    {
        [XmlAttributeAttribute()]
        public ushort Value;
        public Threshold()
        {
        }
        public Threshold(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Value = r.ReadUInt16();
            //TODO: two bytes left---
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    [Description("post")]
    public class Posterize : LayerResource
    {
        [XmlAttributeAttribute()]
        public ushort Value;
        public Posterize()
        {
        }
        public Posterize(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            this.Value = r.ReadUInt16();
            //TODO: two bytes left---
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

}
