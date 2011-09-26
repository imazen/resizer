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
    [Description("curv")]
    public class Curves : LayerResource
    {
        public class Curve
        {
            public class CurvePoint
            {
                [XmlAttributeAttribute()]
                public ushort Output;
                [XmlAttributeAttribute()]
                public ushort Input;

                public CurvePoint()
                {
                }
                public CurvePoint(BinaryPSDReader r)
                {
                    this.Output = r.ReadUInt16();
                    this.Input = r.ReadUInt16();
                }
            }

            [XmlAttributeAttribute()]
            public int Channel;
            public List<CurvePoint> Points;

            public Curve()
            { }
            public Curve(BinaryPSDReader r)
            {
                ushort numPoints = r.ReadUInt16();
                this.Points = new List<CurvePoint>();
                for (int i = 0; i < numPoints; i++)
                {
                    this.Points.Add(new CurvePoint(r));
                }
            }
        }

        public List<Curve> Records;

        public Curves()
        {
        }

        public Curves(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            byte unknown = r.ReadByte();
            ushort version = r.ReadUInt16();
            uint definedCurves = r.ReadUInt32();

            this.Records = new List<Curve>();
            int channelNum = -1;
            while (definedCurves > 0)
            {
                if ((definedCurves & 1) > 0)
                {
                    Curve curve = new Curve(r);
                    curve.Channel = channelNum;
                    this.Records.Add(curve);
                }
                channelNum++;
                definedCurves >>= 1;
            }

            if (r.BytesToEnd > 0)
            {
                //Same again? Clear Records and begin anew? Brrr... Adobe, argh!
                string head = new string(r.ReadPSDChars(4));
                version = r.ReadUInt16(); //version??
                uint numCurves = r.ReadUInt32();

                for (int i = 0; i < numCurves; i++)
                {
                    ushort channelId = r.ReadUInt16();
                    Curve curve = new Curve(r);
                    curve.Channel = (int)channelId - 1;
                }
            }

            this.Data = null;
        }
        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }
}
