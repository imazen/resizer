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

namespace Endogine.Codecs.Photoshop.LayerResources
{
    [Description("levl")]
    public class Levels : LayerResource
    {
        public class LevelRecord
        {
            [XmlAttributeAttribute()]
            public ushort InputFloor;
            [XmlAttributeAttribute()]
            public ushort InputCeiling;
            [XmlAttributeAttribute()]
            public ushort OutputFloor;
            [XmlAttributeAttribute()]
            public ushort OutputCeiling;
            [XmlAttributeAttribute()]
            public ushort Gamma;

            public LevelRecord()
            { }
            public LevelRecord(BinaryPSDReader r)
            {
                this.InputFloor = r.ReadUInt16();
                this.InputCeiling = r.ReadUInt16();
                this.OutputFloor = r.ReadUInt16();
                this.OutputCeiling = r.ReadUInt16();
                this.Gamma = r.ReadUInt16();
            }
        }

        public List<LevelRecord> Records;

        public Levels()
        {
        }

        public Levels(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort version = r.ReadUInt16();
            this.Records = new List<LevelRecord>();
            int endPos = (int)Math.Min(r.BytesToEnd, 292);
            while (r.BaseStream.Position < endPos)
                this.Records.Add(new LevelRecord(r));
            
            if (r.BytesToEnd > 0)
            {
                string head = new string(r.ReadPSDChars(4));
                ushort unknown1 = r.ReadUInt16();
                ushort unknown2 = r.ReadUInt16();
                while (r.BytesToEnd > 0)
                    this.Records.Add(new LevelRecord(r));
            }
            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }
}
