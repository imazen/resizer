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
    public class GridGuidesInfo : ImageResource
    {
        public class GridGuide
        {
            [XmlAttributeAttribute()]
            public uint Location;
            [XmlAttributeAttribute()]
            public bool IsHorizontal;

            [XmlIgnoreAttribute()]
            public float LocationInPixels
            {
                get { return (float)this.Location / 32; }
                set { this.Location = (uint)(value * 32); }
            }

            public GridGuide()
            { }

            public GridGuide(uint location, bool isHorizontal)
            {
                this.Location = location;
                this.IsHorizontal = isHorizontal;
            }
            public GridGuide(BinaryPSDReader reader)
            {
                this.Location = reader.ReadUInt32();
                this.IsHorizontal = reader.ReadBoolean();
            }
            public void Write(BinaryPSDWriter writer)
            {
                writer.Write(this.Location);
                writer.Write((byte)(this.IsHorizontal ? 1 : 0));
            }
        }

        public List<GridGuide> Guides = new List<GridGuide>();
        public ulong GridCycle = 576;

        public List<GridGuide> GetGuidesByAlignment(bool horizontal)
        {
            Dictionary<int, GridGuide> sorted = new Dictionary<int, GridGuide>();
            foreach (GridGuide gg in this.Guides)
            {
                if (gg.IsHorizontal = horizontal)
                    sorted.Add((int)gg.Location, gg);
            }
            List<GridGuide> result = new List<GridGuide>();
            foreach (KeyValuePair<int, GridGuide> kv in sorted)
                result.Add(kv.Value);
            return result;
        }

        public GridGuidesInfo()
        { }

        public GridGuidesInfo(ImageResource imgRes)
            : base(imgRes)
		{
			BinaryPSDReader reader = imgRes.GetDataReader();

            uint Version = reader.ReadUInt32();
            
            //Future implementation of document-specific grids. Initially, set the grid cycle to every quarter inch. At 72 dpi, that would be 18 * 32 = 576 (0x240)
            this.GridCycle = reader.ReadUInt64();

            uint guideCount = reader.ReadUInt32();

            for (int i = 0; i < guideCount; i++)
            {
                GridGuide guide = new GridGuide(reader);
                this.Guides.Add(guide);
            }

			reader.Close();
		}

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((uint)1);
            writer.Write(this.GridCycle);
            writer.Write((uint)this.Guides.Count);
            foreach (GridGuide guide in this.Guides)
                guide.Write(writer);
        }
    }
}
