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
using System;
using System.Collections.Generic;

namespace PhotoshopFile
{
  /// <summary>
  /// The names of the alpha channels
  /// </summary>
  public class AlphaChannels : ImageResource
  {
    private List<string> m_channelNames=new List<string>();
    public List<string> ChannelNames
    {
      get { return m_channelNames; }
    }

    public AlphaChannels(): base((short)ResourceIDs.AlphaChannelNames)
    {
    }

    public AlphaChannels(ImageResource imgRes)
      : base(imgRes)
    {
      
      BinaryReverseReader reader = imgRes.DataReader;
      // the names are pascal strings without padding!!!
      while ((reader.BaseStream.Length - reader.BaseStream.Position) > 0)
      {
        byte stringLength = reader.ReadByte();
        string s = new string(reader.ReadChars(stringLength));
        if(s.Length>0)
          m_channelNames.Add(s);
      }
      reader.Close();
    }

    protected override void StoreData()
    {
      System.IO.MemoryStream stream = new System.IO.MemoryStream();
      BinaryReverseWriter writer = new BinaryReverseWriter(stream);

      foreach (string name in m_channelNames)
      {
        writer.Write((byte)name.Length);
        writer.Write(name.ToCharArray());
      }

      writer.Close();
      stream.Close();

      Data = stream.ToArray();
    }
  }
}
