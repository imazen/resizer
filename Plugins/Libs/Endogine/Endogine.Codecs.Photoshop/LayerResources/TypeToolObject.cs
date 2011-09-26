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
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Endogine.Codecs.Photoshop.LayerResources
{
    [Description("TySh")]
    public class TypeToolObject : LayerResource
    {
        public class Matrix2D
        {
            public double M11;
            public double M12;
            public double M13;
            public double M21;
            public double M22;
            public double M23;
            public Matrix2D()
            { }
            public Matrix2D(BinaryPSDReader r)
            {
                this.M11 = r.ReadPSDDouble();
                this.M12 = r.ReadPSDDouble();
                this.M13 = r.ReadPSDDouble();
                this.M21 = r.ReadPSDDouble();
                this.M22 = r.ReadPSDDouble();
                this.M23 = r.ReadPSDDouble();
            }
        }

        //[XmlIgnoreAttribute()]
        public Matrix2D Transform;
        [XmlIgnoreAttribute()]
        public Descriptor TextDescriptor;
        ////[XmlIgnoreAttribute()]
        public DynVal TxtDescriptor;
        [XmlIgnoreAttribute()]
        public DynVal WarpDescriptor;
        [XmlIgnoreAttribute()]
        public ERectangleF WarpRect;

        public TypeToolObject()
        {
        }

        public TypeToolObject(BinaryPSDReader reader)
            : base(reader)
        {
            BinaryPSDReader r = this.GetDataReader();

            ushort Version = r.ReadUInt16(); //1= Photoshop 5.0

            this.Transform = new Matrix2D(r);

            ushort TextDescriptorVersion = r.ReadUInt16(); //=50. For Photoshop 6.0.
            if (true)
                this.TxtDescriptor = new DynVal(r, true);
            else
            {
                uint XTextDescriptorVersion = r.ReadUInt32(); //=16. For Photoshop 6.0.
                this.TextDescriptor = new Descriptor(r);
            }
            this.Data = r.ReadBytes((int)r.BytesToEnd);

            ////ushort WarpDescriptorVersion = r.ReadUInt16(); //=50. For Photoshop 6.0.
            ////uint XWarpDescriptorVersion = r.ReadUInt32(); //=16. For Photoshop 6.0.
            ////Descriptor warpDescriptor = new Descriptor(r);
            //this.WarpDescriptor = new DynVal(r, true);

            //this.WarpRect = ERectangleF.FromLTRB((float)r.ReadPSDDouble(), (float)r.ReadPSDDouble(), (float)r.ReadPSDDouble(), (float)r.ReadPSDDouble());
            ////this.WarpRect.Left = r.ReadPSDDouble();
            ////double warpRectTop = r.ReadPSDDouble();
            ////double warpRectRight = r.ReadPSDDouble();
            ////double warpRectBottom = r.ReadPSDDouble();

            //this.Data = null;
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            //TODO:
        }


        public class Descriptor
        {
            public class Item
            {
                [XmlAttributeAttribute()]
                public string ID;
                [XmlAttributeAttribute()]
                public string Value;
                public string Data;

                public Item()
                { }
                public bool Read(BinaryPSDReader r)
                {
                    this.ID = "";
                    while (true)
                    {
                        char c = r.ReadChar();
                        if (c == 0x0a)
                            break;
                        this.ID += c;
                    }
                    byte[] buffer = new byte[255];
                    int bufPos = 0;
                    int nearEndCnt = 0;
                    while (true)
                    {
                        byte b = r.ReadByte();
                        buffer[bufPos++] = b;
                        if (b == 0x2f)
                            break;
                        if (b <= 0x00)
                        {
                            nearEndCnt++;
                            if (nearEndCnt == 12)
                                break;
                        }
                        else
                            nearEndCnt = 0;
                    }

                    if (this.ID.Contains(" "))
                    {
                        int index = this.ID.IndexOf(" ");
                        this.Value = this.ID.Substring(index + 1);
                        this.ID = this.ID.Remove(index);
                    }

                    int endPos = bufPos - nearEndCnt - 1;
                    //See if it's only 0x09's:
                    for (int i = 0; i < endPos; i++)
                    {
                        if (buffer[i] != 0x09)
                        {
                            this.Data = Endogine.Serialization.ReadableBinary.CreateHexEditorString(buffer, 0, endPos);
                            break;
                        }
                    }

                    return (nearEndCnt == 0);
                }
            }

            public List<Item> Items;

            public Descriptor()
            {
            }
            public Descriptor(BinaryPSDReader r)
            {
                uint version = r.ReadUInt32(); //?
                r.BaseStream.Position += 6; //?
                string type = new string(r.ReadPSDChars(4));
                uint unknown1 = r.ReadUInt32(); //?
                uint unknown2 = r.ReadUInt32(); //?
                string resType1 = new string(r.ReadPSDChars(4));
                string resType2 = new string(r.ReadPSDChars(4));

                string uniName = r.ReadPSDUnicodeString();
                while (r.ReadByte() != 0x2f)
                    ;
                this.Items = new List<Item>();
                while (true)
                {
                    Item item = new Item();
                    this.Items.Add(item);
                    if (item.Read(r) == false)
                        break;
                }
            }
        }
    }
}
