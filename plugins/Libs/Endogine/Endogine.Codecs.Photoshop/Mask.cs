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

namespace Endogine.Codecs.Photoshop
{
    public class Mask : Channel
    {
        public ERectangle Rectangle;
        ERectangle _otherRectangle;
        byte _color;
        byte _flags = 0;
        byte _flags2 = 0;
        byte _maskBg;

        private enum FlagValues
        {
            PositionRelativeToLayer = 1,
            Disabled = 2,
            InvertWhenBlending = 4
        }

        public bool PositionRelativeToLayer
        {
            get { return (this._flags & (int)FlagValues.PositionRelativeToLayer) > 0; }
            set { this._flags = Endogine.Serialization.BinaryReaderEx.SetBit(this._flags, (int)FlagValues.PositionRelativeToLayer, value); }
        }
        public bool Disabled
        {
            get { return (this._flags & (int)FlagValues.Disabled) > 0; }
            set { this._flags = Endogine.Serialization.BinaryReaderEx.SetBit(this._flags, (int)FlagValues.Disabled, value); }
        }
        public bool InvertWhenBlending
        {
            get { return (this._flags & (int)FlagValues.InvertWhenBlending) > 0; }
            set { this._flags = Endogine.Serialization.BinaryReaderEx.SetBit(this._flags, (int)FlagValues.InvertWhenBlending, value); }
        }


        public Mask(BinaryPSDReader reader, Layer layer)
            : base(layer)
        {
            this._layer = layer;

            int nLength = (int)reader.ReadUInt32();
            if (nLength == 0)
                return;

            long nStart = reader.BaseStream.Position;

            this.Rectangle = reader.ReadPSDRectangle(); //new Rectangle(reader).ToERectangle();

            this._color = reader.ReadByte();
            
            this._flags = reader.ReadByte();
            
            if (nLength == 20)
                reader.ReadUInt16(); //padding
            else if (nLength == 36)
            {
                this._flags2 = reader.ReadByte();//same flags as above according to docs!?!?
                this._maskBg = reader.ReadByte(); //Real user mask background. Only 0 or 255 - ie bool?!?
                this._otherRectangle = reader.ReadPSDRectangle(); //new Rectangle(reader).ToERectangle(); //same as above rectangle according to docs?!?!
            }

            reader.BaseStream.Position = nStart + nLength;
        }

        public override void Write(BinaryPSDWriter writer)
        {
            writer.StartLengthBlock(typeof(uint));
            if (this.Rectangle != null)
            {
                writer.WritePSDRectangle(this.Rectangle); //new Rectangle(this.Rectangle).Write(writer);
                writer.Write(this._color);
                writer.Write(this._flags);

                if (this._otherRectangle != null)
                {
                    writer.Write(this._flags2);
                    writer.Write(this._maskBg);
                    writer.WritePSDRectangle(this._otherRectangle); //new Rectangle(this._otherRectangle).Write(writer);
                }
                else
                    writer.Write((short)0); //padding
            }
            writer.EndLengthBlock();
        }

        public void WritePixels(BinaryPSDWriter writer)
        {
            this.PreparePixelsForSave(writer);
        }
    }
}
