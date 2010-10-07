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
    public class Header
    {
        public ushort Version = 1;
        public short Channels = 4;
        public uint Rows;
        public uint Columns;
        public ushort BitsPerPixel = 8;
        public ColorModes ColorMode = ColorModes.RGB;

        public int NumColorChannels
        {
            get
            {
                switch (this.ColorMode)
                {
                    case ColorModes.Bitmap:
                    case ColorModes.Grayscale:
                    case ColorModes.Indexed:
                        return 1;
                    case ColorModes.Duotone:
                        return 2;
                    case ColorModes.Lab:
                    case ColorModes.RGB:
                        return 3;
                    case ColorModes.CMYK:
                        return 4;
                    case ColorModes.Multichannel:
                        return 5; //TODO: ??
                }
                return 0;
            }
        }

        public Header()
        {
        }

        public Header(BinaryPSDReader reader)
        {
            this.Version = reader.ReadUInt16();
            if (Version != 1)
                throw new Exception("Can not read .psd version " + Version);
            byte[] buf = new byte[256];
            reader.Read(buf, (int)reader.BaseStream.Position, 6); //6 bytes reserved
            this.Channels = reader.ReadInt16();
            this.Rows = reader.ReadUInt32();
            this.Columns = reader.ReadUInt32();
            this.BitsPerPixel = reader.ReadUInt16();
            this.ColorMode = (ColorModes)reader.ReadInt16();
        }

        public void Write(BinaryPSDWriter writer)
        {
            writer.Write(this.Version);
            for (int i = 0; i < 6; i++)
                writer.Write((byte)0);
            writer.Write(this.Channels);
            writer.Write(this.Rows);
            writer.Write(this.Columns);
            writer.Write(this.BitsPerPixel);
            writer.Write((short)this.ColorMode);
        }
    }
}
