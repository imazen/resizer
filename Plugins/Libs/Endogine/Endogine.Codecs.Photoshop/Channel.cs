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
    public class Channel
    {
        int _usage;
        /// <summary>
        /// 0 = red, 1 = green, etc...   1 = transparency mask  2 = user supplied layer mask
        /// </summary>
        public int Usage
        {
            get { return _usage; }
            set { _usage = value; } //TODO: should be set when creating the instance, and always correspond to the key in layer._channels
        }

        //long _length;
        
        protected Layer _layer;
        public Layer Layer
        {
            get { return _layer; }
        }

        protected byte[] _data;
        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        byte[] _dataForSave;


        public Channel(Layer layer)
        {
            this._layer = layer;
        }

        public Channel(BinaryPSDReader reader, Layer layer)
        {
            this._usage = reader.ReadInt16();

            long length = (long)reader.ReadUInt32();
            this._layer = layer;
        }

        public System.Drawing.Bitmap Bitmap
        {
            get
            {
                //TODO: can't set palette
                //System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(this.Layer.Width, this.Layer.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                System.Drawing.Bitmap bmpCp = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                System.Drawing.Imaging.ColorPalette pal = bmpCp.Palette;
                bmpCp.Dispose();
                for (int palIndex = 0; palIndex < 256; palIndex++)
                    pal.Entries[palIndex] = System.Drawing.Color.FromArgb(palIndex, palIndex, palIndex);
                bmp.Palette = pal;


                BitmapHelpers.Canvas canvas = BitmapHelpers.Canvas.Create(bmp);
                canvas.Locked = true;
                int i = 0;
                int width = canvas.Width;
                int height = canvas.Height;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        canvas.SetPixelByte(x, y, this._data[i++]);
                }
                
                return bmp;
            }
        }

        public float GetPixel(int x, int y)
        {
            if (this.Layer.BitsPerPixel == 16)
                return (float)(((int)this._data[x + y * this.Layer.Width * 2]) << 8 + this._data[x + y * this.Layer.Width * 2 + 1]) / 65535;
            else if (true || this.Layer.BitsPerPixel == 8)
                return (float)this._data[x + y * this.Layer.Width] / 255;
            throw new NotImplementedException();
        }
        public void SetPixel(int x, int y, float value)
        {
            if (this.Layer.BitsPerPixel == 16)
            {
                ushort val = (ushort)(value * 65535);
                this._data[x + y * this.Layer.Width * 2] = (byte)(val >> 8);
                this._data[x + y * this.Layer.Width * 2 + 1] = (byte)(val & 255);
            }
            else if (true || this.Layer.BitsPerPixel == 8)
                this._data[x + y * this.Layer.Width] = (byte)(value * 255);
        }

        public virtual void Write(BinaryPSDWriter writer)
        {
            writer.Write((ushort)this._usage);

            //we can't write pixel data immediately (comes later in the file), but we must know the size of the pixel data.
            //thus, precalculate the save data here and save it later.
            System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            BinaryPSDWriter memWriter = new BinaryPSDWriter(memStream);
            this.PreparePixelsForSave(memWriter);
            this._dataForSave = memStream.ToArray();

            writer.Write((uint)this._dataForSave.Length);
        }

        protected void PreparePixelsForSave(BinaryPSDWriter writer)
        {
            PixelData px = new PixelData(this.Layer.Width, this.Layer.Height, this.Layer.BitsPerPixel, 1, this._layer.IsMerged);
            px.Write(writer, this._data, PixelData.Compression.Rle);
        }

        public void WritePixels(BinaryPSDWriter writer)
        {
            if (this._dataForSave == null)
                this.PreparePixelsForSave(writer);
            else
            {
                writer.Write(this._dataForSave);
                this._dataForSave = null;
            }
        }
    }
}
