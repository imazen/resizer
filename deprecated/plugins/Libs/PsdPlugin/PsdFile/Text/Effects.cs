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
using System.ComponentModel;
using System.Xml.Serialization;
using System.Drawing;

namespace PhotoshopFile.Text
{
    [Description("lrFX")]
    public class Effects : PhotoshopFile.Layer.AdjustmentLayerInfo
    {
       public  Dictionary<string, PhotoshopFile.Layer.AdjustmentLayerInfo> _resources = new Dictionary<string, PhotoshopFile.Layer.AdjustmentLayerInfo>();
    
        public Effects()
        {
        }
        public Effects(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;

            BinaryReverseReader r = this.DataReader;

            r.BaseStream.Position += 2; //unused
            ushort nNumEffects = r.ReadUInt16();
            this._resources = new Dictionary<string, PhotoshopFile.Layer.AdjustmentLayerInfo>();
            for (int nEffectNum = 0; nEffectNum < nNumEffects; nEffectNum++)
            {
                PhotoshopFile.Layer.AdjustmentLayerInfo res = new PhotoshopFile.Layer.AdjustmentLayerInfo(r, info.Layer);
                //if (res.Tag != "cmnS")
                //  continue;
                this._resources.Add(res.Key, res);
                //    case "sofi": //unknown
            }

            this.Data = null;
        }

    }



    [Description("oglw,iglw")]
    public class Glow : EffectBase
    {
        [XmlAttributeAttribute()]
        public uint Intensity;
        [XmlAttributeAttribute()]
        public bool UseGlobalAngle;
        [XmlAttributeAttribute()]
        public bool Inner { get { return m_key.StartsWith("i"); } }

        public byte Unknown;
        public Color UnknownColor;


        public Glow()
        { }

        public Glow(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;

            BinaryReverseReader r = this.DataReader;

            //string blendModeSignature = null;
            uint version = r.ReadUInt32(); //two version specifications?!?
            switch (version)
            {
                case 0:
                    this.Blur = r.ReadUInt32();
                    this.Data = null;
                    break;
                case 2:
                    this.Blur = (uint)r.ReadUInt16();
                    this.Intensity = r.ReadUInt32();
                    ushort something = r.ReadUInt16();
                    this.Color = r.ReadPSDColor(16, false); //Inner color (no alpha)

                    this.BlendModeKey = this.ReadBlendKey(r);
                    this.Enabled = r.ReadBoolean();
                    this.Opacity = r.ReadByte();
                    //TODO!
                    if (this.Inner)
                        this.Unknown = r.ReadByte();
                    this.UnknownColor = r.ReadPSDColor(16, false); //unknown color(no alpha)
                    this.Data = r.ReadBytes((int)r.BytesToEnd);
                    break;
            }
        }
    }

    [Description("bevl"), Category("Effect")]
    public class Bevel : EffectBase
    {
        [XmlAttributeAttribute()]
        public uint Angle;
        [XmlAttributeAttribute()]
        public uint Strength;

        [XmlAttributeAttribute()]
        public string ShadowBlendModeKey;

        public Color ShadowColor;

        [XmlAttributeAttribute()]
        public byte BevelStyle;
        [XmlAttributeAttribute()]
        public byte ShadowOpacity;

        [XmlAttributeAttribute()]
        public bool UseGlobalAngle;
        [XmlAttributeAttribute()]
        public bool Inverted;

        public byte Unknown1;
        public byte Unknown2;
        public ushort Unknown3;
        public ushort Unknown4;

        public Bevel()
        {
        }

        public Bevel(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;

            BinaryReverseReader r = this.DataReader;

            //string blendModeSignature = null;

            uint version = r.ReadUInt32();

            switch (version)
            {
                case 0:
                    this.Blur = r.ReadUInt32();
                    this.Data = null;
                    break;
                case 2:
                    this.Angle = (uint)r.ReadUInt16();
                    this.Strength = (uint)r.ReadUInt16();
                    this.Blur = (uint)r.ReadUInt16();

                    this.Unknown1 = r.ReadByte();
                    this.Unknown2 = r.ReadByte();
                    this.Unknown3 = r.ReadUInt16();
                    this.Unknown4 = r.ReadUInt16();

                    this.BlendModeKey = this.ReadBlendKey(r);
                    this.ShadowBlendModeKey = this.ReadBlendKey(r);

                    this.Color = r.ReadPSDColor(16, true);
                    this.ShadowColor = r.ReadPSDColor(16, true);

                    this.BevelStyle = r.ReadByte();
                    this.Opacity = r.ReadByte();
                    this.ShadowOpacity = r.ReadByte();

                    this.Enabled = r.ReadBoolean();
                    this.UseGlobalAngle = r.ReadBoolean();
                    this.Inverted = r.ReadBoolean();

                    System.Drawing.Color someColor = r.ReadPSDColor(16, true);
                    System.Drawing.Color someColor2 = r.ReadPSDColor(16, true);
                    break;
            }
            this.Data = null;
        }
   
    }


    [Description("dsdw,isdw")]
    public class Shadow : EffectBase
    {
        [XmlAttributeAttribute()]
        public uint Intensity;
        [XmlAttributeAttribute()]
        public uint Angle;
        [XmlAttributeAttribute()]
        public uint Distance;
        [XmlAttributeAttribute()]
        public bool UseGlobalAngle;
        [XmlAttributeAttribute()]
        public bool Inner { get { return m_key.StartsWith("i"); } }

        public Shadow()
        { }

        public Shadow(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;

            BinaryReverseReader r = this.DataReader;

            //string blendModeSignature = null;

            int version = r.ReadInt32();
            switch (version)
            {
                case 0:
                    this.Blur = r.ReadUInt32();
                    this.Intensity = r.ReadUInt32();

                    this.Angle = r.ReadUInt32();
                    this.Distance = r.ReadUInt32();

                    this.Color = r.ReadPSDColor(16, true);

                    this.BlendModeKey = this.ReadBlendKey(r);
                    //this.BlendModeSignature = r.ReadUInt32();
                    //this.BlendModeKey = r.ReadUInt32();
                    this.Enabled = r.ReadBoolean();
                    this.UseGlobalAngle = r.ReadBoolean();
                    this.Opacity = r.ReadByte();
                    break;

                case 2:
                    this.Blur = (uint)r.ReadUInt16();
                    this.Intensity = r.ReadUInt32();

                    this.Angle = r.ReadUInt32();
                    this.Distance = r.ReadUInt32();

                    ushort something = r.ReadUInt16();//TODO:?

                    this.Color = r.ReadPSDColor(16, true);

                    this.BlendModeKey = this.ReadBlendKey(r);
                    this.Enabled = r.ReadBoolean();
                    this.UseGlobalAngle = r.ReadBoolean();
                    this.Opacity = r.ReadByte();
                    //TODO: 10 unknown bytes!
                    break;
            }

            this.Data = null;
        }
    }


}
