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


//Only used by Photoshop 5.0 files


using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using PhotoshopFile;

namespace PhotoshopFile.Text
{
    [Description("tySh")]
    public class TypeTool : PhotoshopFile.Layer.AdjustmentLayerInfo
    {
        public class FontInfo
        {
            public ushort Mark;
            public uint FontType;
            public string FontName;
            public string FontFamilyName;
            public string FontStyleName;
            public ushort Script;
            public List<uint> DesignVectors;

            public FontInfo()
            {}
            public FontInfo(BinaryReverseReader r)
            {
                this.Mark = r.ReadUInt16();
                this.FontType = r.ReadUInt32();
                this.FontName = r.ReadPascalString();
                this.FontFamilyName = r.ReadPascalString();
                this.FontStyleName = r.ReadPascalString();
                this.Script = r.ReadUInt16();

                ushort NumDesignAxesVectors = r.ReadUInt16();
                this.DesignVectors = new List<uint>();
                for (int vectorNum = 0; vectorNum < NumDesignAxesVectors; vectorNum++)
                    this.DesignVectors.Add(r.ReadUInt32());
            }
        }
        public Matrix2D Transform;
        public List<FontInfo> FontInfos;

        public TypeTool(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;

            BinaryReverseReader reader = this.DataReader;
            ushort Version = reader.ReadUInt16(); //1= Photoshop 5.0


            //2D transform matrix (6 doubles)
            Transform = new Matrix2D(reader);



            //Font info:
            ushort FontVersion = reader.ReadUInt16(); //6 = Photoshop 5.0
            ushort FaceCount = reader.ReadUInt16();
            this.FontInfos = new List<FontInfo>();
            for (int i = 0; i < FaceCount; i++)
                this.FontInfos.Add(new FontInfo(reader));

            //TODO: make classes of styles as well...
            ushort StyleCount = reader.ReadUInt16();
            for (int i = 0; i < StyleCount; i++)
            {
                ushort Mark = reader.ReadUInt16();
                ushort FaceMark = reader.ReadUInt16();
                uint Size = reader.ReadUInt32();
                uint Tracking = reader.ReadUInt32();
                uint Kerning = reader.ReadUInt32();
                uint Leading = reader.ReadUInt32();
                uint BaseShift = reader.ReadUInt32();
                
                byte AutoKern = reader.ReadByte();
                byte Extra = 0;
                if (Version <= 5)
                    Extra = reader.ReadByte();
                byte Rotate = reader.ReadByte();
            }

            //Text information
            ushort Type = reader.ReadUInt16();
            uint ScalingFactor = reader.ReadUInt32();
            uint CharacterCount = reader.ReadUInt32();

            uint HorizontalPlacement = reader.ReadUInt32();
            uint VerticalPlacement = reader.ReadUInt32();

            uint SelectStart = reader.ReadUInt32();
            uint SelectEnd = reader.ReadUInt32();

            ushort LineCount = reader.ReadUInt16();
            for (int i = 0; i < LineCount; i++)
            {
                uint CharacterCountLine = reader.ReadUInt32();
                ushort Orientation = reader.ReadUInt16();
                ushort Alignment = reader.ReadUInt16();

                ushort DoubleByteChar = reader.ReadUInt16();
                ushort Style = reader.ReadUInt16();
            }

            ushort ColorSpace = reader.ReadUInt16();
            for (int i = 0; i < 4; i++)
                reader.ReadUInt16(); //Color compensation
            byte AntiAlias = reader.ReadByte();
        }

    
    }
}
