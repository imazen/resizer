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

namespace Endogine.Codecs.Photoshop
{
    public class GlobalImage
    {
        //byte[] _tempGlobalImage;
        Document _document;
        [XmlIgnoreAttribute()]
        public Document Document
        {
            get { return _document; }
            set { _document = value; }
        }
        Layer _fakeLayer;
        /// <summary>
        /// For serialization only
        /// </summary>
        public Layer FakeLayer
        {
            get { return _fakeLayer; }
            set { _fakeLayer = value; }
        }

        public GlobalImage()
        {
        }

        public GlobalImage(Document document)
        {
            this._document = document;
        }

        public void Load(BinaryPSDReader reader)
        {
            this.CreateLayer();
            this._fakeLayer.ReadPixels(reader);
            //Test: System.Drawing.Bitmap bmp = this.GetChannelBitmap(0);
        }

        public void Save(BinaryPSDWriter writer)
        {
            //writer.StartLengthBlock(typeof(uint));
            this._fakeLayer.WritePixels(writer);
            //writer.EndLengthBlock();
        }

        private void CreateLayer()
        {
            this._fakeLayer = new Layer(this._document);
            this._fakeLayer.IsMerged = true;
            this._fakeLayer.Width = (int)this._document.Size.X;
            this._fakeLayer.Height = (int)this._document.Size.Y;
            this._fakeLayer.Channels = new Dictionary<int, Channel>();
            for (int i = 0; i < this._document.Channels; i++)
            {
                this._fakeLayer.Channels.Add(i, new Channel(this._fakeLayer));
            }
        }

        [XmlIgnoreAttribute()]
        public System.Drawing.Bitmap Bitmap
        {
            get { return this._fakeLayer.Bitmap; }
            set
            {
                this.CreateLayer();
                if (value.Width != this._document.Size.X || value.Height != this._document.Size.Y)
                    throw new Exception("Global image must be same size as document!");
                this._fakeLayer.Bitmap = value;
            }
        }

        public System.Drawing.Bitmap GetChannelBitmap(int index)
        {
            return this._fakeLayer.Channels[index].Bitmap;
        }
        public System.Drawing.Bitmap GetAlphaChannelBitmap(int index)
        {
            return this.GetChannelBitmap(index - this._document.Header.NumColorChannels);
        }
    }
}
