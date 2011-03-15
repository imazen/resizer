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
	/// Summary description for Thumbnail.
	/// </summary>
	public class Thumbnail : ImageResource
	{
        [XmlAttributeAttribute()]
		public int Format;
        [XmlAttributeAttribute()]
		public int Width;
        [XmlAttributeAttribute()]
        public int Height;
        [XmlAttributeAttribute()]
        public int WidthBytes;
        [XmlAttributeAttribute()]
        public int Size;
        [XmlAttributeAttribute()]
        public int CompressedSize;
        [XmlAttributeAttribute()]
        public short BitPerPixel;
        [XmlAttributeAttribute()]
        public short Planes;

        System.Drawing.Bitmap Bitmap;

        public override ResourceIDs[] AcceptedResourceIDs
        {
            get { return new ResourceIDs[] { ResourceIDs.Thumbnail1, ResourceIDs.Thumbnail2 }; }
        }

        public Thumbnail()
        { }

        public Thumbnail(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();

            //m_bThumbnailFilled = true;

            this.Format = reader.ReadInt32();
            this.Width = reader.ReadInt32();
            this.Height = reader.ReadInt32();
            this.WidthBytes = reader.ReadInt32(); //padded row bytes (
            this.Size = reader.ReadInt32(); //Total size widthbytes * height * planes
            this.CompressedSize = reader.ReadInt32(); //used for consistancy check
            this.BitPerPixel = reader.ReadInt16();
            this.Planes = reader.ReadInt16();

            int numBytes = (int)reader.BytesToEnd;
            byte[] buffer = reader.ReadBytes(numBytes);

            if (this.ID == 1033)
            {
                // BGR
                for (int n = 0; n < numBytes - 2; n += 3)
                {
                    byte tmp = buffer[n + 2];
                    buffer[n + 2] = buffer[n];
                    buffer[n] = tmp;
                }
            }
            System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
            this.Bitmap = new System.Drawing.Bitmap(stream);

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(Format);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(WidthBytes);
            writer.Write(Size);
            writer.Write(CompressedSize);
            writer.Write(BitPerPixel);
            writer.Write(Planes);

            //int nTotalData = this.nSize - 28; // header
            //TODO: writer.Write(this._readPixels);
        }
	}
}
