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
using System.Drawing;
using System.IO;
//using System.Collections;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop
{
	public enum ColorModes
	{
		Bitmap=0, Grayscale=1, Indexed=2, RGB=3, CMYK=4, Multichannel=7, Duotone=8, Lab=9
	};

    //http://www.pcpix.com/Photoshop/char.htm
    //http://www.soft-gems.net:8080/browse/~raw,r=99/Library/GraphicEx/Source/GraphicEx.pas

	/// <summary>
	/// Summary description for Photoshop.
	/// </summary>
    [XmlRoot("PsdDocument")]
	public class Document
	{
        //private Dictionary<int, Layer> _layers;

        ////[XmlArray("Layers"), XmlArrayItem("Layer", typeof(Layer))]
        //[XmlIgnoreAttribute()]
        //public Dictionary<int, Layer> Layers
        //{
        //    get { return _layers; }
        //}
        private List<Layer> _layers = new List<Layer>();
        public List<Layer> Layers
        {
            get { return _layers; }
            set { _layers = value; }
        }
        //public List<Layer> LayersForXml
        //{
        //    get
        //    {
        //        List<Layer> layers = new List<Layer>();
        //        foreach (Layer lr in this._layers.Values)
        //            layers.Add(lr);
        //        return layers;
        //    }
        //    set { }
        //}

        Header _header;
        public Header Header
        {
            get { return _header; }
            set { _header = value; }
        }

        List<LayerResource> _globalLayerResources = new List<LayerResource>();
        public List<LayerResource> GlobalLayerResources
        {
            get { return this._globalLayerResources; }
            set { }
        }
        
        List<ImageResource> _imageResources = new List<ImageResource>();
        //[XmlIgnoreAttribute()]
        //public Dictionary<ImageResource.ResourceIDs, ImageResource> _imageResources = new Dictionary<ImageResource.ResourceIDs, ImageResource>();
        public List<ImageResource> Resources
        {
            get
            {
                return this._imageResources;
                //List<ImageResource> res = new List<ImageResource>();
                //foreach (ImageResource ir in this._imageResources.Values)
                //    res.Add(ir);
                //return res;
            }
            set { }
        }
        //TODO: Resources should be an Endogine PropList
        public ImageResource AddResource(ImageResource.ResourceIDs resourceId)
        {
            //TODO: check if we already have one of same type! Except for f*cking paths (stupid Adobe), gotta get special treatment...
            Type type = ImageResource.ResourceTypes[resourceId];
            ImageResource imgRes = ImageResource.CreateResource(type);
            this._imageResources.Add(imgRes);
            return imgRes;
        }
        public ImageResource GetResource(Type type)
        {
            //Of course, this doesn't work for bloody paths...
            foreach (ImageResource res in this._imageResources)
            {
                if (res.GetType() == type)
                    return res;
            }
            return null;
        }


        [XmlArray("ColorTable"), XmlArrayItem("Color", typeof(Color))]
        public List<Color> ColorTable;
        //[XmlIgnoreAttribute()]
		
        [XmlIgnoreAttribute()]
        public ushort Version
        {
            get { return this._header.Version; }
            set { this._header.Version = value; }
        }
        [XmlIgnoreAttribute()]
        public short Channels
        {
            get { return this._header.Channels; }
            set { this._header.Channels = value; }
        }
        [XmlIgnoreAttribute()]
        public EPoint Size
        {
            get { return new EPoint((int)this._header.Columns, (int)this._header.Rows); }
            set { this._header.Columns = (uint)value.X; this._header.Rows = (uint)value.Y; }
        }
        [XmlIgnoreAttribute()]
        public ushort BitsPerPixel
        {
            get {return this._header.BitsPerPixel; }
            set { this._header.BitsPerPixel = value; }
        }
        [XmlIgnoreAttribute()]
        public ColorModes ColorMode
        {
            get { return this._header.ColorMode; }
            set { this._header.ColorMode = value; }
        }

        GlobalImage _globalImage;
        [XmlIgnoreAttribute()]
        public GlobalImage GlobalImage
        {
            get { return _globalImage; }
            set { _globalImage = value; }
        }

        byte[] _tempGlobalLayerMask;
        public string TempGlobalLayerMask
        {
            get
            {
                if (this._tempGlobalLayerMask == null)
                    return null;
                return Endogine.Serialization.ReadableBinary.CreateHexEditorString(this._tempGlobalLayerMask);
            }
            set { }
        }

        public string Unknown;


        public Document()
        {
            this._header = new Header();
            this.Version = 1;
            this._layers = new List<Layer>(); // new Dictionary<int, Layer>();
            this._globalImage = new GlobalImage(this);

            this.Init();

            this.AddResource(ImageResource.ResourceIDs.ResolutionInfo);
            //this._imageResources.Add(ResourceIDs.DisplayInfo, new ImageResources.DisplayInfo());
            //this._imageResources.Add(ImageResource.ResourceIDs.ResolutionInfo, new ImageResources.ResolutionInfo());
        }

        private void Init()
        {
            ImageResource.Prepare();
        }

        public void SaveXml(string filename, bool saveChannels)
        {
            TextWriter w = new StreamWriter(filename);
            XmlSerializer xs = new XmlSerializer(this.GetType());
            xs.Serialize(w, this);
            //XmlSerializer xs = new XmlSerializer(typeof(LayerResources.ObjectBasedEffects));
            //xs.Serialize(w, new Descriptor());
            w.Close();

            if (saveChannels)
            {
                bool useRgbPng = (this._header.ColorMode == ColorModes.RGB && this._header.BitsPerPixel == 8);
                //useRgbPng = false;

                FileInfo fi = new FileInfo(filename);
                string path = fi.Directory.FullName + "\\" + fi.Name + "_Bitmaps\\";
                Directory.CreateDirectory(path);
                foreach (Layer layer in this._layers)
                {
                    if (useRgbPng)
                    {
                        Bitmap bmp = layer.Bitmap;
                        if (bmp == null)
                            continue;
                        BitmapHelpers.BitmapHelper.Save(bmp, path + layer.Name + ".png");
                    }
                    else
                    {
                        foreach (KeyValuePair<int, Channel> kv in layer.Channels)
                        {
                            Bitmap bmp = kv.Value.Bitmap;
                            BitmapHelpers.BitmapHelper.Save(bmp, path + layer.Name + "_Channel_" + kv.Key + ".png");
                        }
                    }
                }
            }
        }

        public void Save(string filename)
        {
            if (File.Exists(filename))
                File.Delete(filename);

			FileStream stream = new FileStream(filename,
				FileMode.OpenOrCreate, FileAccess.Write);

            BinaryPSDWriter writer = new BinaryPSDWriter(stream);

            writer.Write("8BPS");
            this._header.Write(writer);

            writer.Write((uint)0); //No palette
            //TODO: palette

            //if (this._imageResources == null)
            //    this._imageResources = new Dictionary<ImageResource.ResourceIDs, ImageResource>();
            writer.StartLengthBlock(typeof(uint));
            foreach (ImageResource imgRes in this._imageResources)
                imgRes.Write(writer);
            writer.EndLengthBlock();


            //   Layers
            writer.StartLengthBlock(typeof(uint), 4);

            // LayerInfo:
            writer.StartLengthBlock(typeof(uint));
            writer.Write((ushort)this.Layers.Count);
            foreach (Layer layer in this.Layers)
                layer.Write(writer);
            //Layer pixel data
            foreach (Layer layer in this.Layers)
                layer.WritePixels(writer);

            writer.EndLengthBlock();

            writer.EndLengthBlock();

            writer.PadToNextMultiple(4);

            //Global Mask
            writer.StartLengthBlock(typeof(uint));
            if (this._tempGlobalLayerMask != null)
                writer.Write(this._tempGlobalLayerMask);
            writer.EndLengthBlock();

            //Global image (merged)
            //TODO: !
            if (this._globalImage != null)
                this._globalImage.Save(writer);
        }

		public Document(string a_sFilename)
		{
            this.Init();

			FileStream stream = new FileStream(a_sFilename,
				FileMode.Open, FileAccess.Read);
			//stream.
			BinaryPSDReader reader = new BinaryPSDReader(stream);

			string signature = new string(reader.ReadPSDChars(4));
			if (signature != "8BPS")
				return;

            this._header = new Header(reader);
            //this.Version = reader.ReadUInt16();
            //if (Version != 1)
            //    throw new Exception("Can not read .psd version " + Version);
            //byte[] buf = new byte[256];
            //reader.Read(buf, (int)reader.BaseStream.Position, 6); //6 bytes reserved
            //this.Channels = reader.ReadInt16();
            //this.Rows = reader.ReadUInt32();
            //this.Columns = reader.ReadUInt32();
            //this.BitsPerPixel = (int)reader.ReadUInt16();
            //this.ColorMode = (ColorModes)reader.ReadInt16();
            
            #region Palette
            uint nPaletteLength = reader.ReadUInt32();
			if (nPaletteLength > 0)
			{
                this.ColorTable = new List<Color>();
                for (int i = 0; i < nPaletteLength; i+=3)
                {
                    this.ColorTable.Add(Color.FromArgb((int)reader.ReadByte(), (int)reader.ReadByte(), (int)reader.ReadByte()));
                }
                //this.ColorTable.Add(Color.FromArgb(255, 10, 20));
				
				if (this.ColorMode == ColorModes.Duotone)
				{
				}
				else
				{
				}
            }
            #endregion


            uint nResLength = reader.ReadUInt32(); //? Number of bytes, or number of entries??
			if (nResLength > 0)
			{
				//read settings
                this._imageResources = ImageResource.ReadImageResources(reader);
			}

            
			//reader.JumpToEvenNthByte(4);
			uint nTotalLayersBytes = reader.ReadUInt32();
			long nAfterLayersDefinitions = reader.BaseStream.Position + nTotalLayersBytes;

            if (nTotalLayersBytes == 8)
            {
                stream.Position += nTotalLayersBytes;
                //this.Unknown = Endogine.Serialization.ReadableBinary.CreateHexEditorString(reader.ReadBytes((int)reader.BytesToEnd));
            }
            else
            {
                uint nSize = reader.ReadUInt32(); //What's the difference between nTotalLayersBytes and nSize really?
                long nLayersEndPos = reader.BaseStream.Position + nSize;

                short nNumLayers = reader.ReadInt16();
                bool bSkipFirstAlpha = false;

                if (nNumLayers < 0)
                {
                    bSkipFirstAlpha = true;
                    nNumLayers = (short)-nNumLayers;
                }


                List<Layer> loadOrderLayers = new List<Layer>();
                this._layers = new List<Layer>(); // new Dictionary<int, Layer>();
                for (int nLayerNum = 0; nLayerNum < nNumLayers; nLayerNum++)
                {
                    Layer layerInfo = new Layer(reader, this);
                    layerInfo.DebugLayerLoadOrdinal = nLayerNum;
                    //if (layerInfo.LayerID < 0)
                    //    layerInfo.LayerID = nLayerNum;
                    //if (this._layers.ContainsKey(layerInfo.LayerID))
                    //    throw(new Exception("Duplicate layer IDs! " + layerInfo.LayerID.ToString()));
                    //else
                    //    this._layers.Add(layerInfo.LayerID, layerInfo);
                    this._layers.Add(layerInfo);
                    loadOrderLayers.Add(layerInfo);
                }

                for (int layerNum = 0; layerNum < nNumLayers; layerNum++)
                {
                    Layer layer = (Layer)loadOrderLayers[layerNum];
                    layer.ReadPixels(reader);
                }

                reader.JumpToEvenNthByte(4);
                if (reader.BaseStream.Position != nLayersEndPos)
                    reader.BaseStream.Position = nLayersEndPos; // nAfterLayersDefinitions;

                //Global layer mask
                uint maskLength = reader.ReadUInt32();
                this._tempGlobalLayerMask = null;
                if (maskLength > 0)
                {
                    this._tempGlobalLayerMask = reader.ReadBytes((int)maskLength);

                    //TODO: the docs are obviously wrong here...
                    //ushort overlayColorSpace = reader.ReadUInt16(); //undefined in docs
                    //for (int i = 0; i < 4; i++) 
                    //    reader.ReadUInt16(); //TODO: UInt16 if 16-bit color? Color components - says *both* 4*2 bytes, and 8 bytes in the docs?
                    //reader.ReadUInt16(); //opacity (0-100)
                    //reader.ReadByte(); //Kind: 0=Color selectedi.e. inverted; 1=Color protected;128=use value stored per layer.
                    //reader.ReadByte(); //padding
                }

                //hmm... another section of "global" layer resources..?
                while (true)
                {
                    long cpPos = reader.BaseStream.Position;
                    string sHeader = new string(reader.ReadPSDChars(4));
                    reader.BaseStream.Position = cpPos; //TODO: -= 4 should work, but sometimes ReadPSDChars advances 5?!?!
                    if (sHeader != "8BIM")
                    {
                        break;
                    }
                    LayerResource res = LayerResource.ReadLayerResource(reader, null);
                    this._globalLayerResources.Add(res);
                }
            }

            bool readGlobalImage = true;
            if (readGlobalImage)
            {
                this._globalImage = new GlobalImage(this);
                this._globalImage.Load(reader);
                //the big merged bitmap (which is how the photoshop document looked when it was saved)
                //Bitmap bmp = this._globalImage.Bitmap;
            }

			reader.Close();
			stream.Close();
		}


        public void CreateSprites()
        {
            foreach (Layer layer in this._layers)
            {
                layer.CreateSprite();
            }
        }

        public Layer CreateLayer(string name)
        {
            Layer layer = new Layer(this);
            layer.Name = name;
            //layer.LayerID = this.Layers.Count;
            this._layers.Add(layer);
            return layer;
        }
	}
}
