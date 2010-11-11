/////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006, Frank Blumenberg
// 
// See License.txt for complete licensing and attribution information.
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// 
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
//
// This code contains code from the Endogine sprite engine by Jonas Beckeman.
// http://www.endogine.com/CS/
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using PhotoshopFile.Text;

namespace PhotoshopFile
{
  public class Layer
  {
    ///////////////////////////////////////////////////////////////////////////

    public class Channel
    {
      private Layer m_layer;
      /// <summary>
      /// The layer to which this channel belongs
      /// </summary>
      public Layer Layer
      {
        get { return m_layer; }
      }


      private short m_id;
      /// <summary>
      /// 0 = red, 1 = green, etc.
      /// -1 = transparency mask
      /// -2 = user supplied layer mask
      /// </summary>
      public short ID
      {
        get { return m_id; }
        set { m_id = value; }
      }

      /// <summary>
      /// The length of the compressed channel data.
      /// </summary>
      public int Length;

      private byte[] m_data;
      /// <summary>
      /// The compressed raw channel data
      /// </summary>
      public byte[] Data
      {
        get { return m_data; }
        set { m_data = value; }
      }

      /// <summary>
      /// The raw image data from the channel.
      /// </summary>
      public byte[] m_imageData;

      public byte[] ImageData
      {
        get { return m_imageData; }
        set { m_imageData = value; }
      }

      uint[] m_rowLengthList;
      int m_bytesPerRow;

      private ImageCompression m_imageCompression;
      public ImageCompression ImageCompression
      {
        get { return m_imageCompression; }
        set { m_imageCompression = value; }
      }

      //////////////////////////////////////////////////////////////////

      internal Channel(short id, Layer layer)
      {
        m_id = id;
        m_layer = layer;
        m_layer.Channels.Add(this);
        m_layer.SortedChannels.Add(this.ID, this);
      }

      internal Channel(BinaryReverseReader reader, Layer layer)
      {
        Debug.WriteLine("Channel started at " + reader.BaseStream.Position.ToString());
        
        m_id = reader.ReadInt16();
        Length = reader.ReadInt32();

        m_layer = layer;
      }

      internal void Save(BinaryReverseWriter writer)
      {
        Debug.WriteLine("Channel Save started at " + writer.BaseStream.Position.ToString());

        writer.Write(m_id);
        writer.Write(Data.Length+2); // 2 bytes for the image compression tag
      }

      //////////////////////////////////////////////////////////////////

      internal void LoadPixelData(BinaryReverseReader reader)
      {
        Debug.WriteLine("Channel.LoadPixelData started at " + reader.BaseStream.Position.ToString());

        m_data = reader.ReadBytes((int)Length);

        using (BinaryReverseReader readerImg = DataReader)
        {
          m_imageCompression = (ImageCompression)readerImg.ReadInt16();

          m_bytesPerRow = 0;

          switch (m_layer.PsdFile.Depth)
          {
            case 1:
              m_bytesPerRow = ImageDecoder.BytesFromBits(m_layer.m_rect.Width);
              break;
            case 8:
              m_bytesPerRow = m_layer.m_rect.Width;
              break;
            case 16:
              m_bytesPerRow = m_layer.m_rect.Width * 2;
              break;
          }

          m_imageData = new byte[m_layer.m_rect.Height * m_bytesPerRow];

          switch (m_imageCompression)
          {
            case ImageCompression.Raw:
              readerImg.Read(m_imageData, 0, m_imageData.Length);
              break;
            case ImageCompression.Rle:
              {
                m_rowLengthList = new uint[m_layer.m_rect.Height];
                uint totalRleLength = 0;
                for (int i = 0; i < m_rowLengthList.Length; i++)
                {
                  m_rowLengthList[i] = readerImg.ReadUInt16();
                  totalRleLength += m_rowLengthList[i];
                }
                m_data = new byte[totalRleLength];

                uint idxData = 0;
                for (int i = 0; i < m_layer.m_rect.Height; i++)
                {
                  readerImg.Read(m_data, (int)idxData, (int)m_rowLengthList[i]);
                  idxData += m_rowLengthList[i];

                  // The PSD specification states that rows are padded to even sizes.
                  // However, PSD files generated by Photoshop CS4 do not actually
                  // follow this stipulation.
                }
              }
              break;
            default:
              break;
          }
        }
      }

      public void DecompressImageData()
      {
        MemoryStream stream = new MemoryStream(m_data);
        for (int i = 0; i < m_layer.m_rect.Height; i++)
        {
          int rowIndex = i * m_layer.m_rect.Width;
          RleHelper.DecodedRow(stream, m_imageData, rowIndex, m_bytesPerRow);
        }
      }

      public void CompressImageData()
      {
        if (m_imageCompression == ImageCompression.Rle)
        {
          MemoryStream dataStream = new MemoryStream();
          BinaryReverseWriter writer = new BinaryReverseWriter(dataStream);

          // we will write the correct lengths later, so remember 
          // the position
          long lengthPosition = writer.BaseStream.Position;

          int[] rleRowLengths = new int[m_layer.m_rect.Height];
          for (int i = 0; i < rleRowLengths.Length; i++)
          {
            writer.Write((short)0x1234);
          }

          //---------------------------------------------------------------

          int bytesPerRow = 0;

          switch (m_layer.PsdFile.Depth)
          {
            case 1:
              bytesPerRow = ImageDecoder.BytesFromBits(m_layer.m_rect.Width);
              break;
            case 8:
              bytesPerRow = m_layer.m_rect.Width;
              break;
            case 16:
              bytesPerRow = m_layer.m_rect.Width * 2;
              break;
          }

          //---------------------------------------------------------------

          for (int row = 0; row < m_layer.m_rect.Height; row++)
          {
            int rowIndex = row * m_layer.m_rect.Width;
            rleRowLengths[row] = RleHelper.EncodedRow(writer.BaseStream, m_imageData, rowIndex, bytesPerRow);
          }

          //---------------------------------------------------------------

          long endPosition = writer.BaseStream.Position;

          writer.BaseStream.Position = lengthPosition;

          for (int i = 0; i < rleRowLengths.Length; i++)
          {
            writer.Write((short)rleRowLengths[i]);
          }

          writer.BaseStream.Position = endPosition;

          dataStream.Close();

          m_data = dataStream.ToArray();

          dataStream.Dispose();

        }
        else
        {
          m_data = (byte[])m_imageData.Clone();
        }
      }

      internal void SavePixelData(BinaryReverseWriter writer)
      {
        Debug.WriteLine("Channel SavePixelData started at " + writer.BaseStream.Position.ToString());

        writer.Write((short)m_imageCompression);
        writer.Write(m_data);
      }

      //////////////////////////////////////////////////////////////////

      public BinaryReverseReader DataReader
      {
        get
        {
          if (m_data == null)
            return null;

          return new BinaryReverseReader(new System.IO.MemoryStream(this.m_data));
        }
      }
    }

    ///////////////////////////////////////////////////////////////////////////

    public class Mask
    {
      private Layer m_layer;
      /// <summary>
      /// The layer to which this mask belongs
      /// </summary>
      public Layer Layer
      {
        get { return m_layer; }
      }

      private Rectangle m_rect = Rectangle.Empty;
      /// <summary>
      /// The rectangle enclosing the mask.
      /// </summary>
      public Rectangle Rect
      {
        get { return m_rect; }
        set { m_rect = value; }
      }

      private byte m_defaultColor;
      public byte DefaultColor
      {
        get { return m_defaultColor; }
        set { m_defaultColor = value; }
      }


      private static  int m_positionIsRelativeBit = BitVector32.CreateMask();
      private static int m_disabledBit = BitVector32.CreateMask(m_positionIsRelativeBit);
      private static int m_invertOnBlendBit = BitVector32.CreateMask(m_disabledBit);

      private BitVector32 m_flags = new BitVector32();
      /// <summary>
      /// If true, the position of the mask is relative to the layer.
      /// </summary>
      public bool PositionIsRelative
      {
        get
        {
          return m_flags[m_positionIsRelativeBit];
        }
        set
        {
          m_flags[m_positionIsRelativeBit] = value;
        }
      }

      public bool Disabled
      {
        get { return m_flags[m_disabledBit]; }
        set { m_flags[m_disabledBit] = value; }
      }

      /// <summary>
      /// if true, invert the mask when blending.
      /// </summary>
      public bool InvertOnBlendBit
      {
        get { return m_flags[m_invertOnBlendBit]; }
        set { m_flags[m_invertOnBlendBit] = value; }
      }

      ///////////////////////////////////////////////////////////////////////////

      internal Mask(Layer layer)
      {
        m_layer = layer;
        m_layer.MaskData = this;
      }

      ///////////////////////////////////////////////////////////////////////////

      internal Mask(BinaryReverseReader reader, Layer layer)
      {
        Debug.WriteLine("Mask started at " + reader.BaseStream.Position.ToString());

        m_layer = layer;

        uint maskLength = reader.ReadUInt32();

        if (maskLength <= 0)
          return;

        long startPosition = reader.BaseStream.Position;

        //-----------------------------------------------------------------------

        m_rect = new Rectangle();
        m_rect.Y = reader.ReadInt32();
        m_rect.X = reader.ReadInt32();
        m_rect.Height = reader.ReadInt32() - m_rect.Y;
        m_rect.Width = reader.ReadInt32() - m_rect.X;

        m_defaultColor = reader.ReadByte();

        //-----------------------------------------------------------------------

        byte flags = reader.ReadByte();
        m_flags = new BitVector32(flags);

        //-----------------------------------------------------------------------

        if (maskLength == 36)
        {
          BitVector32 realFlags = new BitVector32(reader.ReadByte());

          byte realUserMaskBackground = reader.ReadByte();

          Rectangle rect = new Rectangle();
          rect.Y = reader.ReadInt32();
          rect.X = reader.ReadInt32();
          rect.Height = reader.ReadInt32() - m_rect.Y;
          rect.Width = reader.ReadInt32() - m_rect.X;
        }


        // there is other stuff following, but we will ignore this.
        reader.BaseStream.Position = startPosition + maskLength;
      }

      ///////////////////////////////////////////////////////////////////////////

      public void Save(BinaryReverseWriter writer)
      {
        Debug.WriteLine("Mask Save started at " + writer.BaseStream.Position.ToString());

        if (m_rect.IsEmpty)
        {
          writer.Write((uint)0);
          return;
        }

        using (new LengthWriter(writer))
        {
          writer.Write(m_rect.Top);
          writer.Write(m_rect.Left);
          writer.Write(m_rect.Bottom);
          writer.Write(m_rect.Right);

          writer.Write(m_defaultColor);

          writer.Write((byte)m_flags.Data);

          // padding 2 bytes so that size is 20
          writer.Write((int)0);
        }
      }

      //////////////////////////////////////////////////////////////////

      /// <summary>
      /// The raw image data from the channel.
      /// </summary>
      public byte[] m_imageData;

      public byte[] ImageData
      {
        get { return m_imageData; }
        set { m_imageData = value; }
      }

      internal void LoadPixelData(BinaryReverseReader reader)
      {
        Debug.WriteLine("Mask.LoadPixelData started at " + reader.BaseStream.Position.ToString());

        if (m_rect.IsEmpty || m_layer.SortedChannels.ContainsKey(-2) == false)
          return;

        Channel maskChannel = m_layer.SortedChannels[-2];


        maskChannel.Data = reader.ReadBytes((int)maskChannel.Length);


        using (BinaryReverseReader readerImg = maskChannel.DataReader)
        {
          maskChannel.ImageCompression = (ImageCompression)readerImg.ReadInt16();

          int bytesPerRow = 0;

          switch (m_layer.PsdFile.Depth)
          {
            case 1:
              bytesPerRow = ImageDecoder.BytesFromBits(m_layer.m_rect.Width);
              break;
            case 8:
              bytesPerRow = m_rect.Width;
              break;
            case 16:
              bytesPerRow = m_rect.Width * 2;
              break;
          }

          maskChannel.ImageData = new byte[m_rect.Height * bytesPerRow];
          // Fill Array
          for (int i = 0; i < maskChannel.ImageData.Length; i++)
          {
            maskChannel.ImageData[i] = 0xAB;
          }

          m_imageData = (byte[])maskChannel.ImageData.Clone();

          switch (maskChannel.ImageCompression)
          {
            case ImageCompression.Raw:
              readerImg.Read(maskChannel.ImageData, 0, maskChannel.ImageData.Length);
              break;
            case ImageCompression.Rle:
              {
                uint[] rowLengthList = new uint[m_rect.Height];

                for (int i = 0; i < rowLengthList.Length; i++)
                  rowLengthList[i] = readerImg.ReadUInt16();

                for (int i = 0; i < m_rect.Height; i++)
                {
                  int rowIndex = i * m_rect.Width;
                  RleHelper.DecodedRow(readerImg.BaseStream, maskChannel.ImageData, rowIndex, bytesPerRow);
                }
              }
              break;
            default:
              break;
          }

          m_imageData = (byte[])maskChannel.ImageData.Clone();

        }
      }


      ///////////////////////////////////////////////////////////////////////////

    }


    ///////////////////////////////////////////////////////////////////////////

    public class BlendingRanges
    {
      private Layer m_layer;
      /// <summary>
      /// The layer to which this channel belongs
      /// </summary>
      public Layer Layer
      {
        get { return m_layer; }
      }

      private byte[] m_data = new byte[0];

      public byte[] Data
      {
        get { return m_data; }
        set { m_data = value; }
      }

      ///////////////////////////////////////////////////////////////////////////

      public BlendingRanges(Layer layer)
      {
        m_layer = layer;
        m_layer.BlendingRangesData = this;
      }

      ///////////////////////////////////////////////////////////////////////////

      public BlendingRanges(BinaryReverseReader reader, Layer layer)
      {
        Debug.WriteLine("BlendingRanges started at " + reader.BaseStream.Position.ToString());

        m_layer = layer;
        int dataLength = reader.ReadInt32();
        if (dataLength <= 0)
          return;

        m_data = reader.ReadBytes(dataLength);
      }

      ///////////////////////////////////////////////////////////////////////////

      public void Save(BinaryReverseWriter writer)
      {
        Debug.WriteLine("BlendingRanges Save started at " + writer.BaseStream.Position.ToString());

        writer.Write((uint)m_data.Length);
        writer.Write(m_data);
      }
    }

    ///////////////////////////////////////////////////////////////////////////

    public class AdjustmentLayerInfo
    {
        protected Layer m_layer;
      /// <summary>
      /// The layer to which this info belongs
      /// </summary>
      public Layer Layer
      {
        get { return m_layer; }
      }

      protected string m_key;
      public string Key
      {
        get { return m_key; }
        set { m_key = value; }
      }

      protected byte[] m_data;
      public byte[] Data
      {
        get { return m_data; }
        set { m_data = value; }
      }

      protected AdjustmentLayerInfo()
      {
      }
      public AdjustmentLayerInfo(string key, Layer layer)
      {
        m_key = key;
        m_layer = layer;
        m_layer.AdjustmentInfo.Add(this);
      }

      public AdjustmentLayerInfo(BinaryReverseReader reader, Layer layer)
      {
        Debug.WriteLine("AdjustmentLayerInfo started at " + reader.BaseStream.Position.ToString());

        m_layer = layer;

        string signature = new string(reader.ReadChars(4));
        if (signature != "8BIM")
        {
          throw new IOException("Could not read an image resource");
        }

        m_key = new string(reader.ReadChars(4));

        uint dataLength = reader.ReadUInt32();
        m_data = reader.ReadBytes((int)dataLength);
      }



      public void Save(BinaryReverseWriter writer)
      {
        Debug.WriteLine("AdjustmentLayerInfo Save started at " + writer.BaseStream.Position.ToString());

        string signature = "8BIM";

        writer.Write(signature.ToCharArray());
        writer.Write(m_key.ToCharArray());
        writer.Write((uint)m_data.Length);
        writer.Write(m_data);
      }

      //////////////////////////////////////////////////////////////////

      public BinaryReverseReader DataReader
      {
        get
        {
          return new BinaryReverseReader(new System.IO.MemoryStream(this.m_data));
        }
      }
    }


    ///////////////////////////////////////////////////////////////////////////

    private PsdFile m_psdFile;
    internal PsdFile PsdFile
    {
      get { return m_psdFile; }
    }

    private Rectangle m_rect = Rectangle.Empty;
    /// <summary>
    /// The rectangle containing the contents of the layer.
    /// </summary>
    public Rectangle Rect
    {
      get { return m_rect; }
      set { m_rect = value; }
    }


    /// <summary>
    /// Channel information.
    /// </summary>
    private List<Channel> m_channels = new List<Channel>();

    public List<Channel> Channels
    {
      get { return m_channels; }
    }

    /// <summary>
    /// Returns channels with nonnegative IDs as an array, for faster indexing.
    /// </summary>
    public Channel[] ChannelsArray
    {
      get
      {
        short maxChannelId = -1;
        foreach (short channelId in SortedChannels.Keys)
        {
          if (channelId > maxChannelId)
            maxChannelId = channelId;
        }

        Channel[] channelsArray = new Channel[maxChannelId + 1];
        for (short i=0; i <= maxChannelId; i++)
        {
          if (SortedChannels.ContainsKey(i))
            channelsArray[i] = SortedChannels[i];
        }
        return channelsArray;
      }
    }

    /// <summary>
    /// Returns alpha channel if it exists, otherwise null.
    /// </summary>
    public Channel AlphaChannel
    {
      get
      {
        if (SortedChannels.ContainsKey(-1))
          return SortedChannels[-1];
        else
          return null;
      }
    }

    private SortedList<short, Channel> m_sortedChannels = new SortedList<short, Channel>();
    public SortedList<short, Channel> SortedChannels
    {
      get
      {
        return m_sortedChannels;
      }
    }

    private string m_blendModeKey="norm";
    /// <summary>
    /// The blend mode key for the layer
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// </item>
    /// <term>norm</term><description>normal</description>
    /// <term>dark</term><description>darken</description>
    /// <term>lite</term><description>lighten</description>
    /// <term>hue </term><description>hue</description>
    /// <term>sat </term><description>saturation</description>
    /// <term>colr</term><description>color</description>
    /// <term>lum </term><description>luminosity</description>
    /// <term>mul </term><description>multiply</description>
    /// <term>scrn</term><description>screen</description>
    /// <term>diss</term><description>dissolve</description>
    /// <term>over</term><description>overlay</description>
    /// <term>hLit</term><description>hard light</description>
    /// <term>sLit</term><description>soft light</description>
    /// <term>diff</term><description>difference</description>
    /// <term>smud</term><description>exlusion</description>
    /// <term>div </term><description>color dodge</description>
    /// <term>idiv</term><description>color burn</description>
    /// </list>
    /// </remarks>
    public string BlendModeKey
    {
      get { return m_blendModeKey; }
      set
      {
        if (value.Length != 4) throw new ArgumentException("Key length must be 4");
        m_blendModeKey = value;
      }
    }


    private byte m_opacity;
    /// <summary>
    /// 0 = transparent ... 255 = opaque
    /// </summary>
    public byte Opacity
    {
      get { return m_opacity; }
      set { m_opacity = value; }
    }


    private bool m_clipping;
    /// <summary>
    /// false = base, true = non–base
    /// </summary>
    public bool Clipping
    {
      get { return m_clipping; }
      set { m_clipping = value; }
    }

    private static int m_protectTransBit = BitVector32.CreateMask();
    private static int m_visibleBit = BitVector32.CreateMask(m_protectTransBit);

    BitVector32 m_flags = new BitVector32();

    /// <summary>
    /// If true, the layer is visible.
    /// </summary>
    public bool Visible
    {
      get { return !m_flags[m_visibleBit]; }
      set { m_flags[m_visibleBit] = !value; }
    }


    /// <summary>
    /// Protect the transparency
    /// </summary>
    public bool ProtectTrans
    {
      get { return m_flags[m_protectTransBit]; }
      set { m_flags[m_protectTransBit] = value; }
    }


    private string m_name;
    /// <summary>
    /// The descriptive layer name
    /// </summary>
    public string Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    private BlendingRanges m_blendingRangesData;
    public Layer.BlendingRanges BlendingRangesData
    {
      get { return m_blendingRangesData; }
      set { m_blendingRangesData = value; }
    }

    private Mask m_maskData;
    public Layer.Mask MaskData
    {
      get { return m_maskData; }
      set { m_maskData = value; }
    }

    private List<AdjustmentLayerInfo> m_adjustmentInfo = new List<AdjustmentLayerInfo>();
    public List<Layer.AdjustmentLayerInfo> AdjustmentInfo
    {
      get { return m_adjustmentInfo; }
      set { m_adjustmentInfo = value; }
    }

    ///////////////////////////////////////////////////////////////////////////

    public Layer(PsdFile psdFile)
    {
      m_psdFile = psdFile;
      m_psdFile.Layers.Add(this);
    }

    public Layer(BinaryReverseReader reader, PsdFile psdFile)
    {
      Debug.WriteLine("Layer started at " + reader.BaseStream.Position.ToString());

      m_psdFile = psdFile;
      m_rect = new Rectangle();
      m_rect.Y = reader.ReadInt32();
      m_rect.X = reader.ReadInt32();
      m_rect.Height = reader.ReadInt32() - m_rect.Y;
      m_rect.Width = reader.ReadInt32() - m_rect.X;

      //-----------------------------------------------------------------------

      int numberOfChannels = reader.ReadUInt16();
      this.m_channels.Clear();
      for (int channel = 0; channel < numberOfChannels; channel++)
      {
        Channel ch = new Channel(reader, this);
        m_channels.Add(ch);
        m_sortedChannels.Add(ch.ID, ch);
      }

      //-----------------------------------------------------------------------

      string signature = new string(reader.ReadChars(4));
      if (signature != "8BIM")
        throw (new IOException("Layer Channelheader error!"));

      m_blendModeKey = new string(reader.ReadChars(4));
      m_opacity = reader.ReadByte();

      m_clipping = reader.ReadByte() > 0;

      //-----------------------------------------------------------------------

      byte flags = reader.ReadByte();
      m_flags = new BitVector32(flags);

      //-----------------------------------------------------------------------

      reader.ReadByte(); //padding

      //-----------------------------------------------------------------------

      Debug.WriteLine("Layer extraDataSize started at " + reader.BaseStream.Position.ToString());

      // this is the total size of the MaskData, the BlendingRangesData, the 
      // Name and the AdjustmentLayerInfo
      uint extraDataSize = reader.ReadUInt32();

      // remember the start position for calculation of the 
      // AdjustmentLayerInfo size
      long extraDataStartPosition = reader.BaseStream.Position;

      m_maskData = new Mask(reader, this);
      m_blendingRangesData = new BlendingRanges(reader, this);

      //-----------------------------------------------------------------------

      long namePosition = reader.BaseStream.Position;

      m_name = reader.ReadPascalString();

      int paddingBytes =(int)((reader.BaseStream.Position - namePosition) % 4);

      Debug.Print("Layer {0} padding bytes after name", paddingBytes);
      reader.ReadBytes(paddingBytes);

      //-----------------------------------------------------------------------

      m_adjustmentInfo.Clear();

      long adjustmentLayerEndPos = extraDataStartPosition + extraDataSize;
      while (reader.BaseStream.Position < adjustmentLayerEndPos)
      {
        try
        {
            AdjustmentLayerInfo ali = new AdjustmentLayerInfo(reader, this);
            if (ali.Key.Equals("lrFX"))
            {
                //A sub-key - we want to merge its sub-layer info items with this dict.
                m_adjustmentInfo.AddRange(new Effects(ali)._resources.Values);
            } else 
                m_adjustmentInfo.Add(ali); // Just add the items
        }
        catch
        {
          reader.BaseStream.Position = adjustmentLayerEndPos;
        }
      }


      //-----------------------------------------------------------------------
      // make sure we are not on a wrong offset, so set the stream position 
      // manually
      reader.BaseStream.Position = adjustmentLayerEndPos;
    }

    ///////////////////////////////////////////////////////////////////////////

    public void PrepareSave(PrivateThreadPool threadPool)
    {
      foreach (Channel ch in m_channels)
      {
        CompressChannelContext ccc = new CompressChannelContext(ch);
        WaitCallback waitCallback = new WaitCallback(ccc.CompressChannel);
        threadPool.QueueUserWorkItem(waitCallback);
      }
    }

    public void Save(BinaryReverseWriter writer)
    {
      Debug.WriteLine("Layer Save started at " + writer.BaseStream.Position.ToString());

      writer.Write(m_rect.Top);
      writer.Write(m_rect.Left);
      writer.Write(m_rect.Bottom);
      writer.Write(m_rect.Right);

      //-----------------------------------------------------------------------

      writer.Write((short)m_channels.Count);
      foreach (Channel ch in m_channels)
        ch.Save(writer);

      //-----------------------------------------------------------------------

      string signature = "8BIM";
      writer.Write(signature.ToCharArray());
      writer.Write(m_blendModeKey.ToCharArray());
      writer.Write(m_opacity);
      writer.Write((byte)(m_clipping ? 1 : 0));

      writer.Write((byte)m_flags.Data);

      //-----------------------------------------------------------------------

      writer.Write((byte)0);

      //-----------------------------------------------------------------------

      using (new LengthWriter(writer))
      {
        m_maskData.Save(writer);
        m_blendingRangesData.Save(writer);

        long namePosition = writer.BaseStream.Position;

        writer.WritePascalString(m_name);

        int paddingBytes = (int)((writer.BaseStream.Position - namePosition) % 4);
        Debug.Print("Layer {0} write padding bytes after name", paddingBytes);

        for (int i = 0; i < paddingBytes;i++ )
          writer.Write((byte)0);

        foreach (AdjustmentLayerInfo info in m_adjustmentInfo)
        {
          info.Save(writer);
        }
      }
    }

    private class CompressChannelContext
    {
      private PhotoshopFile.Layer.Channel ch;

      public CompressChannelContext(PhotoshopFile.Layer.Channel ch)
      {
        this.ch = ch;
      }

      public void CompressChannel(object context)
      {
        ch.CompressImageData();
      }
    }

  }
}
