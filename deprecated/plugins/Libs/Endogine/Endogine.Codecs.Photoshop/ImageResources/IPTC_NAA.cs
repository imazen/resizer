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
    public class IPTC_NAA : ImageResource
    {
        public List<IPTCEntry> Entries;

        public enum TypeId
        {
            DocTitle = 517,
            Keywords = 537,
            Instructions = 552,
            CreatedDate = 567,
            Author = 592,
            CreatedCity = 602,
            StateProvince = 607,
            Country = 613,
            Transmission = 615,
            Headline = 617,
            Credit = 622,
            Source = 627,
            CopyrightNotice = 628,
            Decription = 632,
            DescriptionWriter = 634
        }

        public class IPTCEntry
        {
            [XmlIgnoreAttribute()]
            public TypeId Id;
            [XmlAttributeAttribute()]
            public string TypeIdForXml
            { get { return this.Id.ToString(); } set { } }

            [XmlAttributeAttribute()]
            public string Name;

            public IPTCEntry()
            {
            }
        }

        public IPTC_NAA()
        { }

        public IPTC_NAA(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();

            reader.ReadByte();
            reader.ReadUInt16();
            reader.ReadUInt16();
            reader.ReadUInt16();

            this.Entries = new List<IPTCEntry>();
            while (reader.BytesToEnd > 0)
            {
                byte starter = reader.ReadByte();
                if (starter != 0x1c)
                    throw new Exception("IPTC error");

                IPTCEntry entry = new IPTCEntry();
                entry.Id = (TypeId)reader.ReadUInt16();
                reader.ReadByte();
                entry.Name = reader.ReadPascalStringUnpadded();
                this.Entries.Add(entry);
            }

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }

}
