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
using Endogine.Serialization;
using System.IO;

namespace Endogine.Codecs.Photoshop
{
    public class BinaryPSDReader : Endogine.Serialization.BinaryReverseReader
    {
        public BinaryPSDReader(Stream stream)
            : base(stream)
        { }

        public List<ushort> ReadPSDChannelValues(int numChannels)
        {
            List<ushort> result = new List<ushort>();
            for (int i = 0; i < numChannels; i++)
                result.Add(this.ReadUInt16());
            return result;
        }

        public System.Drawing.Color ReadPSDColor(int bits, bool alpha)
        {
            if (bits == 8)
            {
                int a = (int)base.ReadByte();
                if (!alpha)
                    a = 255;
                return System.Drawing.Color.FromArgb(a, this.ReadByte(), this.ReadByte(), this.ReadByte());
            }
            else
            {
                this.BaseStream.Position += 2; //Always?
                ushort a = ushort.MaxValue;
                if (alpha)
                    a = this.ReadUInt16();
                ushort r = this.ReadUInt16();
                ushort g = this.ReadUInt16();
                ushort b = this.ReadUInt16();
                return System.Drawing.Color.FromArgb((int)a >> 8, (int)r >> 8, (int)g >> 8, (int)b >> 8);
            }
        }

        /// <summary>
        /// Standard ReadPSDChars() keeps reading until it has numBytes chars that are != 0. This one doesn't care
        /// </summary>
        /// <param name="numBytes"></param>
        /// <returns></returns>
        public char[] ReadPSDChars(int numBytes)
        {
            char[] chars = new char[numBytes];
            for (int i = 0; i < numBytes; i++)
                chars[i] = (char)this.ReadByte();
            return chars;
        }

        public Endogine.ERectangle ReadPSDRectangle()
        {
            Endogine.ERectangle rct = new ERectangle();
            rct.Y = this.ReadInt32();
            rct.X = this.ReadInt32();
            rct.Height = this.ReadInt32() - rct.Y;
            rct.Width = this.ReadInt32() - rct.X;
            return rct;
        }
        public Endogine.ERectangle ReadPSDRectangleReversed()
        {
            Endogine.ERectangle rct = new ERectangle();
            rct.X = this.ReadInt32();
            rct.Y = this.ReadInt32();
            rct.Width = this.ReadInt32() - rct.X;
            rct.Height = this.ReadInt32() - rct.Y;
            return rct;
        }

        public double ReadPSDDouble()
        {
            //TODO: examine PSD format!
            double val = base.ReadDouble();
            unsafe
            {
                SwapBytes((byte*)&val, 8);
            }
            return val;
            //byte[] val = base.ReadBytes(8);
            //unsafe
            //{
            //    SwapBytes(&val, 8);
            //}
            //MemoryStream memStream = new MemoryStream(val);
            //BinaryReader r = new BinaryReader(memStream);
            //return r.ReadDouble();
        }

        public float ReadPSDFixedSingle()
        {
            short intVal = this.ReadInt16();
            return (float)intVal + (float)this.ReadUInt16() / ushort.MaxValue;
        }

        public float ReadPSDSingle()
        {
            //TODO: examine PSD format!
            float val = base.ReadSingle();
            unsafe
            {
                SwapBytes((byte*)&val, 4);
            }
            return val;
        }

        public string ReadPSDUnicodeString()
        {
            //TextReader input = new StreamReader(this.BaseStream, System.Text.Encoding.Unicode);
            //char[] buffer = new char[200];
            //int num = input.Read(buffer, (int)r.BaseStream.Position, (int)nUnicodeLength);

            string s = "";
            int nLength = (int)this.ReadUInt32();
            for (int i = 0; i < nLength * 2; i++)
            {
                char c = base.ReadChar();
                if (i % 2 == 1 && c != 0)
                    s += c;
            }
            //if ((nLength % 2) == 0)
            //    base.ReadByte();
            return s;
        }
    }
}
