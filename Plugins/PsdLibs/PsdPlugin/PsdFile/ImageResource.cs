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
// This code is adapted from code in the Endogine sprite engine by Jonas Beckeman.
// http://www.endogine.com/CS/
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;

namespace PhotoshopFile
{

  public enum ResourceIDs
  {
    Undefined = 0,
    MacPrintInfo = 1001,
    ResolutionInfo = 1005,
    AlphaChannelNames = 1006,
    DisplayInfo = 1007,
    Caption = 1008,
    BorderInfo = 1009,
    BgColor = 1010,
    PrintFlags = 1011,
    MultiChannelHalftoneInfo = 1012,
    ColorHalftoneInfo = 1013,
    DuotoneHalftoneInfo = 1014,
    MultiChannelTransferFunctions = 1015,
    ColorTransferFunctions = 1016,
    DuotoneTransferFunctions = 1017,
    DuotoneImageInfo = 1018,
    BlackWhiteRange = 1019,
    EPSOptions = 1021,
    QuickMaskInfo = 1022, //2 bytes containing Quick Mask channel ID, 1 byte boolean indicating whether the mask was initially empty.
    LayerStateInfo = 1024, //2 bytes containing the index of target layer. 0=bottom layer.
    WorkingPathUnsaved = 1025,
    LayersGroupInfo = 1026, //2 bytes per layer containing a group ID for the dragging groups. Layers in a group have the same group ID.
    IPTC_NAA = 1028,
    RawFormatImageMode = 1029,
    JPEGQuality = 1030,
    GridGuidesInfo = 1032,
    Thumbnail1 = 1033,
    CopyrightInfo = 1034,
    URL = 1035,
    Thumbnail2 = 1036,
    GlobalAngle = 1037,
    ColorSamplers = 1038,
    ICCProfile = 1039, //The raw bytes of an ICC format profile, see the ICC34.pdf and ICC34.h files from the Internation Color Consortium located in the documentation section
    Watermark = 1040,
    ICCUntagged = 1041, //1 byte that disables any assumed profile handling when opening the file. 1 = intentionally untagged.
    EffectsVisible = 1042, //1 byte global flag to show/hide all the effects layer. Only present when they are hidden.
    SpotHalftone = 1043, // 4 bytes for version, 4 bytes for length, and the variable length data.
    DocumentSpecific = 1044,
    UnicodeAlphaNames = 1045, // 4 bytes for length and the string as a unicode string
    IndexedColorTableCount = 1046, // 2 bytes for the number of colors in table that are actually defined
    TransparentIndex = 1047,
    GlobalAltitude = 1049,  // 4 byte entry for altitude
    Slices = 1050,
    WorkflowURL = 1051, //Unicode string, 4 bytes of length followed by unicode string
    JumpToXPEP = 1052, //2 bytes major version, 2 bytes minor version,
    //4 bytes count. Following is repeated for count: 4 bytes block size,
    //4 bytes key, if key = 'jtDd' then next is a Boolean for the dirty flag
    //otherwise it’s a 4 byte entry for the mod date
    AlphaIdentifiers = 1053, //4 bytes of length, followed by 4 bytes each for every alpha identifier.
    URLList = 1054, //4 byte count of URLs, followed by 4 byte long, 4 byte ID, and unicode string for each count.
    VersionInfo = 1057, //4 byte version, 1 byte HasRealMergedData, unicode string of writer name, unicode string of reader name, 4 bytes of file version.
    Unknown4 = 1058, //pretty long, 302 bytes in one file. Holds creation date, maybe Photoshop license number
    XMLInfo = 1060, //some kind of XML definition of file. The xpacket tag seems to hold binary data
    Unknown = 1061, //seems to be common!
    Unknown2 = 1062, //seems to be common!
    Unknown3 = 1064, //seems to be common!
    PathInfo = 2000, //2000-2999 actually I think?
    ClippingPathName = 2999,
    PrintFlagsInfo = 10000
  }


  /// <summary>
  /// Summary description for ImageResource.
  /// </summary>
  public class ImageResource
  {
    private short m_id;
    public short ID
    {
      get { return m_id; }
      set { m_id = value; }
    }

    private string m_name=String.Empty;
    public string Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    private byte[] m_data;
    public byte[] Data
    {
      get { return m_data; }
      set { m_data = value; }
    }

    private string m_osType = String.Empty;

    public string OSType
    {
      get { return m_osType; }
      set { m_osType = value; }
    }

    public ImageResource()
    {
    }

    public ImageResource(short id)
    {
      m_id = id;
    }

    public ImageResource(ImageResource imgRes)
    {
      m_id = imgRes.m_id;
      m_name = imgRes.m_name;

      m_data = new byte[imgRes.m_data.Length];
      imgRes.m_data.CopyTo(m_data, 0);
    }

    //////////////////////////////////////////////////////////////////

    public ImageResource(BinaryReverseReader reader)
    {
      m_osType = new string(reader.ReadChars(4));
      if (m_osType != "8BIM" && m_osType != "MeSa")
      {
        throw new InvalidOperationException("Could not read an image resource");
      }

      m_id = reader.ReadInt16();
      m_name = reader.ReadPascalString();

      uint settingLength = reader.ReadUInt32();
      m_data = reader.ReadBytes((int)settingLength);

      if (reader.BaseStream.Position % 2 == 1)
        reader.ReadByte();
    }

    //////////////////////////////////////////////////////////////////

    public void Save(BinaryReverseWriter writer)
    {
      StoreData();

      if (m_osType == String.Empty)
        m_osType = "8BIM";

      writer.Write(m_osType.ToCharArray());
      writer.Write(m_id);

      writer.WritePascalString(m_name);

      writer.Write((int)m_data.Length);
      writer.Write(m_data);

      if (writer.BaseStream.Position % 2 == 1)
        writer.Write((byte)0);
    }

    //////////////////////////////////////////////////////////////////

    protected virtual void StoreData()
    {

    }

    //////////////////////////////////////////////////////////////////

    public BinaryReverseReader DataReader
    {
      get
      {
        return new BinaryReverseReader(new System.IO.MemoryStream(this.m_data));
      }
    }

    //////////////////////////////////////////////////////////////////

    public override string ToString()
    {
      return String.Format("{0} {1}", (ResourceIDs)m_id, m_name);
    }
  }
}
