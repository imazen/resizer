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
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop.ImageResources
{
    /// <summary>
    /// Summary description for CopyRightInfo.
    /// </summary>
    public class Slices : ImageResource
    {
        public class Slice
        {
            [XmlAttributeAttribute()]
            public uint ID;
            [XmlAttributeAttribute()]
            public uint GroupID;
            [XmlAttributeAttribute()]
            public uint Origin;
            [XmlAttributeAttribute()]
            public string Name;
            [XmlAttributeAttribute()]
            public uint Type;
            public ERectangle Rectangle;
            [XmlAttributeAttribute()]
            public string URL;
            public string Target;
            public string Message;
            public string AltTag;
            [XmlAttributeAttribute()]
            public bool CellTextIsHtml;
            public string CellText;
            [XmlAttributeAttribute()]
            public uint HorizontalAlignment;
            [XmlAttributeAttribute()]
            public uint VerticalAlignment;
            public System.Drawing.Color Color;

            public Slice()
            { }
            public Slice(BinaryPSDReader reader)
            {
                this.ID = reader.ReadUInt32();
                this.GroupID = reader.ReadUInt32();
                this.Origin = reader.ReadUInt32();
                this.Name = reader.ReadPSDUnicodeString();
                this.Type = reader.ReadUInt32();
                this.Rectangle = reader.ReadPSDRectangleReversed(); //new Rectangle(reader).ToERectangle();
                this.URL = reader.ReadPSDUnicodeString();
                this.Target = reader.ReadPSDUnicodeString();
                this.Message = reader.ReadPSDUnicodeString();
                this.AltTag = reader.ReadPSDUnicodeString();
                this.CellTextIsHtml = reader.ReadBoolean();
                this.CellText = reader.ReadPSDUnicodeString();
                this.HorizontalAlignment = reader.ReadUInt32();
                this.VerticalAlignment = reader.ReadUInt32();
                this.Color = reader.ReadPSDColor(8, true);

                //TODO: same info seems to follow in another format!
            }
        }

        [XmlAttributeAttribute()]
        public uint Version;
        public ERectangle Rectangle;
        [XmlAttributeAttribute()]
        public string SlicesName;
        public List<Slice> SliceList;

        public List<DynVal> Values;

        public Slices()
        { }

        public Slices(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.Version = reader.ReadUInt32();
            this.Rectangle = reader.ReadPSDRectangle(); // new Rectangle(reader).ToERectangle();
            this.SlicesName = reader.ReadPSDUnicodeString();

            int cnt = (int)reader.ReadUInt32();
            this.SliceList = new List<Slice>();
            for (int i = 0; i < cnt; i++)
                this.SliceList.Add(new Slice(reader));

            int unknown1 = (int)reader.ReadUInt32();
            int unknown2 = (int)reader.ReadUInt32();
            ushort unknown3 = reader.ReadUInt16();
            string unknown4 = DynVal.ReadSpecialString(reader);
            int unknown5 = (int)reader.ReadUInt32();

            this.Values = new List<DynVal>();
            while (reader.BytesToEnd > 0)
            {
                DynVal val = DynVal.ReadValue(reader, false);
                this.Values.Add(val);
            }
            //this.Values = DynVal.ReadValues(reader);
            //this.Data = reader.ReadBytes((int)reader.BytesToEnd);
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            //TODO: !
        }
    }
}
