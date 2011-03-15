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
using Endogine.BitmapHelpers;
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop
{
	public class Layer
	{
        Document _document;
        public Document Document
        {
            get { return _document; }
        }

        ERectangle _rect;
        public ERectangle Rectangle
        {
            get { return _rect; }
        }
        [XmlIgnoreAttribute()]
        public int Width
        {
            get { return this._rect.Width; }
            set { if (this._rect == null) this._rect = new ERectangle(); this._rect.Width = value; }
        }
        [XmlIgnoreAttribute()]
        public int Height
        {
            get { return this._rect.Height; }
            set { if (this._rect == null) this._rect = new ERectangle(); this._rect.Height = value; }
        }

        /// <summary>
        /// Returns the Document's BitsPerPixel
        /// </summary>
        [XmlIgnoreAttribute()]
        public int BitsPerPixel
        {
            get { return this.Document.BitsPerPixel; }
        }

		//public Page page;
		//public ushort NumChannels;

        Dictionary<int, Channel> _channels;
        [XmlIgnoreAttribute()]
        public Dictionary<int, Channel> Channels
        {
            get { return _channels; }
            set { _channels = value; }
        }

        Mask _mask;

        bool _isMerged = false;
        [XmlIgnoreAttribute()]
        public bool IsMerged
        {
            get { return _isMerged; }
            set { _isMerged = value; }
        }

        Dictionary<string, LayerResource> _resources = new Dictionary<string, LayerResource>();
        public List<LayerResource> ResourcesForXml
        {
            get { return LayerResource.GetFlatResources(this._resources); }
            set { }
        }

        [XmlIgnoreAttribute()]
        public int DebugLayerLoadOrdinal;

        [XmlAttributeAttribute()]
        public string Name;

        [XmlAttributeAttribute()]
        public byte Opacity = 255; //0 (transparent) - 255 (opaque)

        [XmlAttributeAttribute()]
        public byte Clipping; // 0 = base, 1 = non-base

        /// <summary>
        /// Name that makes sense
        /// </summary>
        [XmlAttributeAttribute()]
        public string BlendKey = BlendKeys.Normal.ToString();
        public enum BlendKeys
        {
            Normal, Darken, Lighten, Hue, Saturation, Color, Luminosity, Multiply, Screen, Dissolve, Overlay, HardLight, SoftLight, Difference, Exclusion, ColorDodge, ColorBurn
        }
        private enum _blendKeysPsd
        {
            norm, dark, lite, hue, sat, colr, lum, mul, scrn, diss, over, hLit, sLit, diff, smud, div, idiv
        }

        
        public byte Flags = 0;
        private enum FlagValues
        {
            TransparencyProtected = 1,
            Invisible = 2,
            Obsolete = 4,
            Bit4Used = 8, //1 for Photoshop 5.0 and later, tells if bit 4 has useful information
            PixelDataIrrelevant = 16 //pixel data irrelevant to appearance of document
        }
        [XmlAttributeAttribute()]
        public bool Visible
        {
            get { return (this.Flags & (int)FlagValues.Invisible) == 0; }
            set { this.Flags = Endogine.Serialization.BinaryReaderEx.SetBit(this.Flags, (int)FlagValues.Invisible, !value); }
        }


        public LayerResource GetResource(Type type)
        {
            foreach (LayerResource res in this._resources.Values)
            {
                if (res.GetType() == type)
                    return res;
            }
            return null;
        }
        public LayerResource GetOrCreateLayerResource(Type type)
        {
            LayerResource res = this.GetResource(type);
            if (res == null)
            {
                res = LayerResource.Create(type);
                this._resources.Add(res.Tag, res);
            }
            return res;
        }

        [XmlIgnoreAttribute()]
        public int LayerID
        {
            get { LayerResource res = this.GetResource(typeof(LayerResources.LayerId)); if (res == null) return -999; return (int)((LayerResources.LayerId)res).Id; }
            set { LayerResources.LayerId res = (LayerResources.LayerId)this.GetOrCreateLayerResource(typeof(LayerResources.LayerId)); res.Id = (uint)value; }
        }
        [XmlIgnoreAttribute()]
        public EPointF ReferencePoint
        {
            get { return ((LayerResources.ReferencePoint)this.GetResource(typeof(LayerResources.ReferencePoint))).Point; }
        }
        [XmlIgnoreAttribute()]
        public bool BlendClipping
        {
            get { return ((LayerResources.BlendClipping)this.GetResource(typeof(LayerResources.BlendClipping))).Value; }
        }
        [XmlIgnoreAttribute()]
        public bool Blend
        {
            get { return ((LayerResources.BlendElements)this.GetResource(typeof(LayerResources.BlendElements))).Value; }
        }
        [XmlIgnoreAttribute()]
        public bool Knockout
        {
            get { return ((LayerResources.Knockout)this.GetResource(typeof(LayerResources.Knockout))).Value; }
        }
        [XmlIgnoreAttribute()]
        public System.Drawing.Color SheetColor
        {
            get { return ((LayerResources.SheetColor)this.GetResource(typeof(LayerResources.SheetColor))).Color; }
        }
        //public uint NameSourceSetting;
        //public string UnicodeName;

        List<System.Drawing.Color> _blendRanges = new List<System.Drawing.Color>();

        public Layer()
        {
        }

        public Layer(Document document)
        {
            this._document = document;
        }

        //public void AddChannel(Channel ch)
        //{
        //    this._channels.Add(ch.Usage, ch);
        //}

		public Layer(BinaryPSDReader reader, Document document)
		{
            this._document = document;

            this._rect = reader.ReadPSDRectangle();

			ushort numChannels = reader.ReadUInt16();
            this._channels = new Dictionary<int, Channel>();
            for (int channelNum = 0; channelNum < numChannels; channelNum++)
			{
                Channel ch = new Channel(reader, this);
                if (this._channels.ContainsKey(ch.Usage))
                    continue; //TODO: !!
                this._channels.Add(ch.Usage, ch);
			}

			string sHeader = new string(reader.ReadPSDChars(4));
			if (sHeader != "8BIM")
				throw(new Exception("Layer Channelheader error!"));
            
            //'levl'=Levels 'curv'=Curves 'brit'=Brightness/contrast 'blnc'=Color balance 'hue '=Old Hue/saturation, Photoshop 4.0 'hue2'=New Hue/saturation, Photoshop 5.0 'selc'=Selective color 'thrs'=Threshold 'nvrt'=Invert 'post'=Posterize
			this.BlendKey = new string(reader.ReadPSDChars(4));
            int nBlend = -1;
            try
            {
                nBlend = (int)Enum.Parse(typeof(_blendKeysPsd), this.BlendKey);
            }
            catch
            {
                throw new Exception("Unknown blend key: " + this.BlendKey);
            }
            if (nBlend >= 0)
            {
                BlendKeys key = (BlendKeys)nBlend;
                this.BlendKey = Enum.GetName(typeof(BlendKeys), key);
            }

            this.Opacity = reader.ReadByte();
			this.Clipping = reader.ReadByte();
			this.Flags = reader.ReadByte();
            
	        reader.ReadByte(); //padding


			uint extraDataSize = reader.ReadUInt32();
			long nChannelEndPos = reader.BaseStream.Position + (long)extraDataSize;
			if (extraDataSize > 0)
			{
				uint nLength;

                this._mask = new Mask(reader, this);
                if (this._mask.Rectangle == null)
                    this._mask = null;

				//blending ranges
                this._blendRanges = new List<System.Drawing.Color>();
				nLength = reader.ReadUInt32();
                //First come Composite gray blend source / destination; Contains 2 black values followed by 2 white values. Present but irrelevant for Lab & Grayscale.
                //Then 4+4 for each channel (source + destination colors)
				for (uint i = 0; i < nLength/8; i++)
				{
                    this._blendRanges.Add(System.Drawing.Color.FromArgb((int)reader.ReadUInt32()));
                    this._blendRanges.Add(System.Drawing.Color.FromArgb((int)reader.ReadUInt32()));
                }

				//Name
                //nLength = (uint)reader.ReadByte();
                //reader.BaseStream.Position -= 1; //TODO: wtf did I do here?
				this.Name = reader.ReadPascalString();

				//TODO: sometimes there's a 2-byte padding here, but it's not 4-aligned... What is it?
                long posBefore = reader.BaseStream.Position;
				sHeader = new string(reader.ReadPSDChars(4));
				if (sHeader != "8BIM")
				{
					reader.BaseStream.Position-=2;
					sHeader = new string(reader.ReadPSDChars(4));
				}
                if (sHeader != "8BIM")
                    reader.BaseStream.Position = posBefore;
                else
                {
                    reader.BaseStream.Position -= 4;
                    this._resources = LayerResource.ReadLayerResources(reader, null);
                }
                if (reader.BaseStream.Position != nChannelEndPos)
                    reader.BaseStream.Position = nChannelEndPos;
			}
		}

        public void ReadPixels(BinaryPSDReader reader)
        {
            int numChannelsToRead = this._channels.Count;
            if (this._channels.ContainsKey(-2))
                numChannelsToRead--;

            PixelData px = new PixelData(this.Width, this.Height, this.BitsPerPixel, numChannelsToRead, this._isMerged);
            px.Read(reader);

            int i = 0;
            foreach (Channel ch in this._channels.Values)
            {
                if (ch.Usage != -2)
                    ch.Data = px.GetChannelData(i++);
                    //ch.ReadPixels(reader);
            }

            if (this._mask != null)
            {
                px = new PixelData(this.Width, this.Height, this.BitsPerPixel, 1, false);
                px.Read(reader);
                this._mask.Data = px.GetChannelData(0);
                //this._mask.ReadPixels(reader);
            }
        }



        public Sprite CreateSprite()
        {
            //TODO: check if we've already created a member!
            System.Drawing.Bitmap bmp = this.Bitmap;
            if (bmp == null)
                return null;
            Sprite sp = new Sprite();
            sp.Name = this.Name;
            sp.LocZ = this.LayerID;
            sp.Blend = this.Opacity;
            //TODO:
            //sp.Loc = layerInfo.ReferencePoint;
            //sp.Ink = layerInfo.BlendKey;
            sp.Color = this.SheetColor;
            MemberSpriteBitmap mb = new MemberSpriteBitmap(bmp);
            sp.Member = mb;
            return sp;
        }

        [XmlIgnoreAttribute()]
        public System.Drawing.Bitmap Bitmap
        {
            //TODO: add property to get Mask bitmap! Or Mask multiplied with alpha?
            get
            {
                if (this.Width == 0 || this.Height == 0)
                    return null;

                if (this._channels.Count == 0)
                    return null;

                foreach (Channel ch in this._channels.Values)
                {
                    if (ch.Data == null)
                        return null;
                }

                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(this.Width, this.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Endogine.BitmapHelpers.Canvas canvas = Endogine.BitmapHelpers.Canvas.Create(bmp);
                canvas.Locked = true;

                //this.Document.ColorMode == ColorModes.
                //this.GetType().Assembly.GetType("Endogine.ColorEx.");
                Endogine.ColorEx.ColorBase clr = new Endogine.ColorEx.ColorRgbFloat();

                List<Channel> channelsToUse = new List<Channel>();
                foreach (KeyValuePair<int, Channel> kv in this._channels)
                {
                    if (kv.Key >= 0)
                        channelsToUse.Add(kv.Value);
                }
                if (this._channels.ContainsKey(-1))
                    channelsToUse.Add(this._channels[-1]); //Alpha must always come last!

                float[] channelValues = new float[channelsToUse.Count];
                for (int y = this.Height-1; y >= 0; y--)
                {
                    for (int x = this.Width - 1; x >= 0; x--)
                    {
                        for (int channelNum = 0; channelNum < channelsToUse.Count; channelNum++)
                            channelValues[channelNum] = channelsToUse[channelNum].GetPixel(x, y);

                        clr.Array = channelValues;
                        canvas.SetPixel(x, y, clr.ColorRGBA);
                    }
                }

                canvas.Locked = false;
                return bmp;
            }
            set
            {
                this._rect = new ERectangle(0, 0, value.Width, value.Height);
                this._channels = new Dictionary<int, Channel>();
                Canvas canvas = Canvas.Create(value);

                //TODO: alpha channel has id 0, when no alpha, must set first id = 1!
                int startUsageId = 0;
                if (canvas.HasAlpha)
                    startUsageId = -1;
                if (this._document.Channels != canvas.NumChannels)
                {
                }
                for (int i = 0; i < canvas.NumChannels; i++)
                {
                    Channel ch = new Channel(this);
                    ch.Usage = i + startUsageId;
                    ch.Data = new byte[value.Width * value.Height * canvas.BitsPerPixel / 8];
                    this._channels.Add(ch.Usage, ch);
                }
                canvas.Locked = true;
                Endogine.ColorEx.ColorRgbFloat clrFloat = new Endogine.ColorEx.ColorRgbFloat();
                for (int y = this.Height - 1; y >= 0; y--)
                {
                    for (int x = this.Width - 1; x >= 0; x--)
                    {
                        System.Drawing.Color clr = canvas.GetPixel(x, y);
                        clrFloat.ColorRGBA = clr;
                        float[] array = clrFloat.Array;
                        for (int channelNum = 0; channelNum < this._channels.Count; channelNum++)
                            this._channels[3 - channelNum + startUsageId].SetPixel(x, y, array[channelNum]);
                    }
                }
                canvas.Locked = false;
            }
        }


        public void Write(BinaryPSDWriter writer)
        {
            writer.WritePSDRectangle(this._rect); // new Rectangle(this._rect).Write(writer);

            writer.Write((ushort)this._channels.Count);
            foreach (Channel channel in this._channels.Values)
            {
                channel.Write(writer);
            }

            writer.Write("8BIM");

            BlendKeys bk = (BlendKeys)Enum.Parse(typeof(BlendKeys), this.BlendKey);
            _blendKeysPsd bkPsd = (_blendKeysPsd)(int)bk;
            writer.Write(bkPsd.ToString());

            writer.Write(this.Opacity);
            writer.Write(this.Clipping);
            writer.Write(this.Flags);
            writer.Write((byte)0); //padding
            writer.StartLengthBlock(typeof(uint));

            if (this._mask != null || (this._blendRanges != null && this._blendRanges.Count > 0) || !string.IsNullOrEmpty(this.Name))
            {
                if (this._mask != null)
                    this._mask.Write(writer);
                else
                    writer.Write((uint)0); //TODO: should be a static of Mask..?

                //blending ranges
                writer.Write((uint)this._blendRanges.Count * 4);
                foreach (System.Drawing.Color clr in this._blendRanges)
                    writer.Write((uint)clr.ToArgb());


                //why is there padding here? Seems unnecessary.
                long namePosition = writer.BaseStream.Position;

                writer.WritePascalString(this.Name);

                int paddingBytes = (int)((writer.BaseStream.Position - namePosition) % 4);
                for (int i = 0; i < paddingBytes; i++)
                    writer.Write((byte)0);


                foreach (LayerResource res in this._resources.Values)
                {
                    res.Write(writer);
                }
            }

            writer.EndLengthBlock();
        }

        public void WritePixels(BinaryPSDWriter writer)
        {
            if (this._isMerged)
            {
                PixelData px = new PixelData(this.Width, this.Height, this.BitsPerPixel, this._channels.Count, this._isMerged);
                List<byte[]> chs = new List<byte[]>();
                foreach (Channel ch in this._channels.Values)
                {
                    if (ch.Usage != -2)
                        chs.Add(ch.Data);
                }
                px.Write(writer, chs, PixelData.Compression.Rle);
                //System.IO.MemoryStream stream = new System.IO.MemoryStream();
                //System.IO.BinaryWriter wr = new System.IO.BinaryWriter(stream);
            }
            else
            {
                foreach (Channel ch in this._channels.Values)
                {
                    if (ch.Usage != -2)
                        ch.WritePixels(writer);
                }
            }

            if (this._mask != null)
                this._mask.WritePixels(writer);
        }
	}
}
