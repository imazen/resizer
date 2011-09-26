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
    public class PathInfo : ImageResource
    {
        public enum RecordType
        {
            ClosedPathLength = 0, ClosedPathBezierKnotLinked, ClosedPathBezierKnotUnlinked,
            OpenPathLength, OpenPathBezierKnotLinked, OpenPathBezierKnotUnlinked,
            PathFill, Clipboard, InitialFill
        }

        public class BezierKnot
        {
            public EPointF Control1;
            public EPointF Anchor;
            public EPointF Control2;
            //public bool Open;
            [XmlAttributeAttribute()]
            public bool Linked;

            public BezierKnot()
            { }
        }

        public class Clipboard
        {
            public ERectangleF Rectangle;
            public float Scale;

            public Clipboard()
            { }
        }

        public class NewPath
        {
            [XmlAttributeAttribute()]
            public bool Open;
            public NewPath()
            { }
        }

        [XmlAttributeAttribute()]
        public int PathNum;
        public List<object> Commands;

        public PathInfo()
        { }

        public PathInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();

            this.PathNum = this.ID - 2000;
            this.ID = 2000;

            ushort numKnots = 0;
            int cnt = 0;
            this.Commands = new List<object>();
            while (reader.BytesToEnd > 0)
            {
                RecordType rtype = (RecordType)(int)reader.ReadUInt16();
                //Should always start with PathFill (0)
                if (cnt == 0 && rtype != RecordType.PathFill)
                    throw new Exception("PathInfo start error!");

                switch (rtype)
                {
                    case RecordType.InitialFill:
                        reader.BaseStream.Position += 1;
                        bool allPixelStart = reader.ReadBoolean();
                        reader.BaseStream.Position += 22;
                        break;

                    case RecordType.PathFill:
                        if (cnt != 0)
                            throw new Exception("Path fill?!?");
                        reader.BaseStream.Position += 24;
                        break;

                    case RecordType.Clipboard:
                        ERectangleF rct = new ERectangleF();
                        rct.Top = reader.ReadPSDSingle();
                        rct.Left = reader.ReadPSDSingle();
                        rct.Bottom = reader.ReadPSDSingle();
                        rct.Right = reader.ReadPSDSingle();
                        Clipboard clp = new Clipboard();
                        clp.Rectangle = rct;
                        clp.Scale = reader.ReadPSDSingle();
                        reader.BaseStream.Position += 4;
                        this.Commands.Add(clp);
                        break;

                    case RecordType.ClosedPathLength:
                    case RecordType.OpenPathLength:
                        numKnots = reader.ReadUInt16();
                        reader.BaseStream.Position += 22;
                        NewPath np = new NewPath();
                        np.Open = (rtype == RecordType.OpenPathLength);
                        this.Commands.Add(np);
                        break;

                    case RecordType.ClosedPathBezierKnotLinked:
                    case RecordType.ClosedPathBezierKnotUnlinked:
                    case RecordType.OpenPathBezierKnotLinked:
                    case RecordType.OpenPathBezierKnotUnlinked:
                        BezierKnot bz = new BezierKnot();
                        
                        EPointF[] pts = new EPointF[3];
                        for (int i = 0; i < 3; i++)
                        {
                            float y = reader.ReadPSDFixedSingle(); //y comes first...
                            pts[i] = new EPointF(reader.ReadPSDFixedSingle(), y) / 256;
                        }
                        bz.Control1 = pts[0];
                        bz.Anchor = pts[1];
                        bz.Control2 = pts[2];
                        bz.Linked = (rtype == RecordType.ClosedPathBezierKnotLinked || rtype == RecordType.OpenPathBezierKnotLinked);
                        //bz.Open = (rtype == RecordType.OpenPathBezierKnotLinked || rtype == RecordType.OpenPathBezierKnotUnlinked);

                        this.Commands.Add(bz);
                        numKnots--;
                        break;
                }
                cnt++;
            }

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }
}
