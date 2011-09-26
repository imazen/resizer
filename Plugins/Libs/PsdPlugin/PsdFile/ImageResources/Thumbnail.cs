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
using System.IO;
using System.Drawing;

namespace PhotoshopFile
{
  /// <summary>
  /// Summary description for Thumbnail.
  /// </summary>
  public class Thumbnail : ImageResource
  {
    private Bitmap m_thumbnailImage;
    public Bitmap Image
    {
      get { return m_thumbnailImage; }
      set { m_thumbnailImage = value; }
    }

    public Thumbnail(ImageResource imgRes):base(imgRes)
    {
      using (BinaryReverseReader reader = DataReader)
      {
        int format = reader.ReadInt32();
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        int widthBytes = reader.ReadInt32();
        int size = reader.ReadInt32();
        int compressedSize = reader.ReadInt32();
        short bitPerPixel = reader.ReadInt16();
        short planes = reader.ReadInt16();

        if (format == 1)
        {

          byte[] imgData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

          using (MemoryStream strm = new MemoryStream(imgData))
          {
            m_thumbnailImage = (Bitmap)(Bitmap.FromStream(strm).Clone());
          }

          if (this.ID == 1033)
          {
            //// BGR
            //for(int y=0;y<m_thumbnailImage.Height;y++)
            //  for (int x = 0; x < m_thumbnailImage.Width; x++)
            //  {
            //    Color c=m_thumbnailImage.GetPixel(x,y);
            //    Color c2=Color.FromArgb(c.B, c.G, c.R);
            //    m_thumbnailImage.SetPixel(x, y, c);
            //  }
          }

        }
        else
        {
          m_thumbnailImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }
      }
    }
  }
}
