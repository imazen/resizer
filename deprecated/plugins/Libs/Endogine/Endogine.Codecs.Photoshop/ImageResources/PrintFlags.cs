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

namespace Endogine.Codecs.Photoshop.ImageResources
{
    public class PrintFlagsInfo : ImageResource
    {
        [XmlAttributeAttribute()]
        public byte CenterCrop;
        [XmlAttributeAttribute()]
        public uint BleedWidthValue;
        [XmlAttributeAttribute()]
        public ushort BleedWidthScale;

        public PrintFlagsInfo()
        { }

        public PrintFlagsInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            ushort version = reader.ReadUInt16();

            this.CenterCrop = reader.ReadByte();
            reader.ReadByte(); //padding?
            this.BleedWidthValue = reader.ReadUInt32();
            this.BleedWidthScale = reader.ReadUInt16();

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    public class PrintFlags : ImageResource
    {
        [XmlAttributeAttribute()]
        public bool Labels;
        [XmlAttributeAttribute()]
        public bool CropMarks;
        [XmlAttributeAttribute()]
        public bool ColorBars;
        [XmlAttributeAttribute()]
        public bool RegMarks;
        [XmlAttributeAttribute()]
        public bool Negative;
        [XmlAttributeAttribute()]
        public bool Flip;
        [XmlAttributeAttribute()]
        public bool Interpolate;
        [XmlAttributeAttribute()]
        public bool Caption;
        [XmlAttributeAttribute()]
        public bool Unknown;

        public PrintFlags()
        { }

        public PrintFlags(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            
            this.Labels = reader.ReadBoolean();
            this.CropMarks = reader.ReadBoolean();
            this.ColorBars = reader.ReadBoolean();
            this.RegMarks = reader.ReadBoolean();
            this.Negative = reader.ReadBoolean();
            this.Flip = reader.ReadBoolean();
            this.Interpolate = reader.ReadBoolean();
            this.Caption = reader.ReadBoolean();
            if (reader.BytesToEnd > 0)
                this.Unknown = reader.ReadBoolean();

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }
}
