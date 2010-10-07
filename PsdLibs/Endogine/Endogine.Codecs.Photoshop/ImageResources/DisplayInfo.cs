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

namespace Endogine.Codecs.Photoshop.ImageResources
{
	/// <summary>
	/// Summary description for DisplayInfo.
	/// </summary>
	public class DisplayInfo : ImageResource
	{
		public ColorModes ColorSpace = ColorModes.RGB;
        [XmlIgnoreAttribute()]
        public short[] Color = new short[] { -1, 0, 0, 0 };
		public short Opacity = 100;			// 0..100
		public bool kind = false;				// selected = false, protected = true


        public DisplayInfo()
        { }

        public DisplayInfo(ImageResource imgRes)
            : base(imgRes)
		{
			BinaryPSDReader reader = imgRes.GetDataReader();

			this.ColorSpace = (ColorModes)reader.ReadInt16();
            for (int i = 0; i < 4; i++)
                this.Color[i] = reader.ReadInt16();

			this.Opacity = (short)Math.Max(0,Math.Min(100,(int)reader.ReadInt16()));
			this.kind = reader.ReadByte()==0?false:true;

			reader.Close();
		}

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write((short)this.ColorSpace);
            for (int i = 0; i < 4; i++)
                writer.Write(this.Color[i]);
            writer.Write(this.Opacity);
            writer.Write((byte)(this.kind ? 1 : 0));
        }
	}
}
