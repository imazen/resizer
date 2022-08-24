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
    public class GlobalAngle : ImageResource
    {
        [XmlAttributeAttribute()]
        public int Value = 30;

        public GlobalAngle()
        { }

        public GlobalAngle(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.Value = reader.ReadInt32();
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    public class GlobalAltitude : GlobalAngle
    {
        public GlobalAltitude()
        { }
        public GlobalAltitude(ImageResource imgRes)
            : base(imgRes)
        { }
    }


    public class CopyrightInfo : ImageResource
    {
        [XmlAttributeAttribute()]
        public bool Value;

        public CopyrightInfo()
        { }

        public CopyrightInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.Value = reader.ReadByte() == 0 ? false : true;
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((byte)(this.Value ? 1 : 0));
        }
    }


    public class ICCUntagged : CopyrightInfo
    {
        public ICCUntagged()
        { }
        public ICCUntagged(ImageResource imgRes)
            : base(imgRes)
        {
        }
    }




    public class URL : ImageResource
    {
        [XmlAttributeAttribute()]
        public string Value;

        public URL()
        { }

        public URL(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.Value = new string(reader.ReadChars((int)reader.BytesToEnd));
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }


    public class LayerStateInfo : ImageResource
    {
        [XmlAttributeAttribute()]
        public ushort TargetLayerIndex;

        public LayerStateInfo()
        { }

        public LayerStateInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.TargetLayerIndex = reader.ReadUInt16();
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    public class DocumentSpecificIds : ImageResource
    {
        [XmlAttributeAttribute()]
        public uint StartIndex;

        public DocumentSpecificIds()
        { }

        public DocumentSpecificIds(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            this.StartIndex = reader.ReadUInt32();
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

    public class URLList : ImageResource
    {
        public class URLEntry
        {
            [XmlAttributeAttribute()]
            public uint ID;
            [XmlAttributeAttribute()]
            public string URL;
            public URLEntry()
            { }
            public URLEntry(uint id, string url)
            {
                this.ID = id;
                this.URL = url;
            }
        }
        [XmlAttributeAttribute()]
        public List<string> URLsx;
        //TODO: this fails:
        [XmlIgnoreAttribute()]
        public List<URLEntry> URLs;

        public URLList()
        { }

        public URLList(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();
            int numUrls = reader.ReadInt32();
            this.URLs = new List<URLEntry>();
            for (int i = 0; i < numUrls; i++)
            {
                reader.ReadUInt32(); //padding??
                uint id = reader.ReadUInt32();
                string url = reader.ReadPSDUnicodeString();
                this.URLs.Add(new URLEntry(id, url));
                this.URLsx.Add(url);
            }
            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

}
