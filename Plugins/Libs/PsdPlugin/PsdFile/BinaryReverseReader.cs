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
using System.Text;

namespace PhotoshopFile
{
  /// <summary>
  /// Reads primitive data types as binary values in in big-endian format
  /// </summary>
  public class BinaryReverseReader : BinaryReader
  {
    public BinaryReverseReader(Stream a_stream)
          : base(a_stream, Encoding.GetEncoding("ISO-8859-1"))
    {
      
    }

    public override short ReadInt16()
    {
      short val = base.ReadInt16();
      unsafe
      {
        this.SwapBytes((byte*)&val, 2);
      }
      return val;
    }

    public override double ReadDouble()
    {
        double val = base.ReadDouble();
        unsafe
        {
            SwapBytes((byte*)&val, 8);
        }
        return val;
    }
    public override int ReadInt32()
    {
      int val = base.ReadInt32();
      unsafe
      {
        this.SwapBytes((byte*)&val, 4);
      }
      return val;
    }
    public override long ReadInt64()
    {
      long val = base.ReadInt64();
      unsafe
      {
        this.SwapBytes((byte*)&val, 8);
      }
      return val;
    }

    public override ushort ReadUInt16()
    {
      ushort val = base.ReadUInt16();
      unsafe
      {
        this.SwapBytes((byte*)&val, 2);
      }
      return val;
    }

    public override uint ReadUInt32()
    {
      uint val = base.ReadUInt32();
      unsafe
      {
        this.SwapBytes((byte*)&val, 4);
      }
      return val;
    }

    public override ulong ReadUInt64()
    {
      ulong val = base.ReadUInt64();
      unsafe
      {
        this.SwapBytes((byte*)&val, 8);
      }
      return val;
    }

    //////////////////////////////////////////////////////////////////

    public string ReadPascalString()
    {
      byte stringLength = base.ReadByte();

      char[] c = base.ReadChars(stringLength);

      if ((stringLength % 2) == 0)
        base.ReadByte();

      return new string(c);
    }
    /// <summary>
    /// Does not support code points past 255
    /// </summary>
    /// <returns></returns>
    public string ReadPSDUnicodeString()
    {
        string s = "";
        int nLength = (int)this.ReadUInt32();
        //ndj's version:
        //return System.Text.UTF32Encoding.BigEndianUnicode.GetString(base.ReadBytes(nLength));
       
        for (int i = 0; i < nLength * 2; i++)
        {
            
            char c = base.ReadChar();
            if (i % 2 == 1 && c != 0)
                s += c;
        }

        if (s.Length == 1 && s[0] == 0)
        {
            //String of a single null character, 
            return null;
        }

        return s;
    }

    public string ReadTdTaString()
    {
        StringBuilder sb = new StringBuilder(4096);
        //int nLength = (int)this.ReadUInt32();
        char c;
        while (BytesToEnd > 0)
        {
            c = base.ReadChar();
            if (c != 0x0) sb.Append(c);
            if (c == '>' && sb[sb.Length - 2] == '>' && sb[sb.Length - 3] == '\n')
            {
                //we hit the end.
                break;
            }
        }
        return sb.ToString();
    }

    public long BytesToEnd
    {
        get
        {
            return (this.BaseStream.Length - this.BaseStream.Position);
        }
    }


    
    public System.Drawing.Color ReadPSDColor(int bits, bool alpha)
    {
        if (bits == 8)
        {
            int a = (int)base.ReadByte();
            if (!alpha)
                a = 255;
            return System.Drawing.Color.FromArgb(a, this.ReadByte(), this.ReadByte(), this.ReadByte());
        }
        else
        {
            this.BaseStream.Position += 2; //Always?
            ushort a = ushort.MaxValue;
            if (alpha)
                a = this.ReadUInt16();
            ushort r = this.ReadUInt16();
            ushort g = this.ReadUInt16();
            ushort b = this.ReadUInt16();
            return System.Drawing.Color.FromArgb((int)a >> 8, (int)r >> 8, (int)g >> 8, (int)b >> 8);
        }
    }

    

    /// <summary>
    /// Standard ReadPSDChars() keeps reading until it has numBytes chars that are != 0. This one doesn't care
    /// </summary>
    /// <param name="numBytes"></param>
    /// <returns></returns>
    public char[] ReadPSDChars(int numBytes)
    {
        char[] chars = new char[numBytes];
        for (int i = 0; i < numBytes; i++)
            chars[i] = (char)this.ReadByte();
        return chars;
    }


    //////////////////////////////////////////////////////////////////

    unsafe protected void SwapBytes(byte* ptr, int nLength)
    {
      for (long i = 0; i < nLength / 2; ++i)
      {
        byte t = *(ptr + i);
        *(ptr + i) = *(ptr + nLength - i - 1);
        *(ptr + nLength - i - 1) = t;
      }
    }


    public bool CanReadByte()
    {
        return BytesToEnd > 0;
    }
  }

  //////////////////////////////////////////////////////////////////

  /// <summary>
  /// Writes primitive data types as binary values in in big-endian format
  /// </summary>
  public class BinaryReverseWriter : BinaryWriter
  {
    public BinaryReverseWriter(Stream a_stream)
      : base(a_stream)
    {
    }

    public bool AutoFlush;

    public void WritePascalString(string s)
    {
      char[] c;
      if (s.Length > 255)
        c = s.Substring(0, 255).ToCharArray();
      else
        c = s.ToCharArray();

      base.Write((byte)c.Length);
      base.Write(c);

      int realLength = c.Length + 1;

      if ((realLength % 2) == 0)
        return;

      for (int i = 0; i < (2 - (realLength % 2)); i++)
        base.Write((byte)0);

      if (AutoFlush)
        Flush();
    }

    public override void Write(short val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 2);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }
    public override void Write(int val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 4);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }
    public override void Write(long val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 8);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }

    public override void Write(ushort val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 2);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }

    public override void Write(uint val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 4);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }

    public override void Write(ulong val)
    {
      unsafe
      {
        this.SwapBytes((byte*)&val, 8);
      }
      base.Write(val);

      if (AutoFlush)
        Flush();
    }

    //////////////////////////////////////////////////////////////////

    unsafe protected void SwapBytes(byte* ptr, int nLength)
    {
      for (long i = 0; i < nLength / 2; ++i)
      {
        byte t = *(ptr + i);
        *(ptr + i) = *(ptr + nLength - i - 1);
        *(ptr + nLength - i - 1) = t;
      }
    }
  }


  class LengthWriter : IDisposable
  {
    long m_lengthPosition = long.MinValue;
    long m_startPosition;
    BinaryReverseWriter m_writer;

    public LengthWriter(BinaryReverseWriter writer)
    {
      m_writer = writer;

      // we will write the correct length later, so remember 
      // the position
      m_lengthPosition = m_writer.BaseStream.Position;
      m_writer.Write((uint)0xFEEDFEED);

      // remember the start  position for calculation Image 
      // resources length
      m_startPosition = m_writer.BaseStream.Position;
    }

    public void Write()
    {
      if (m_lengthPosition != long.MinValue)
      {
        long endPosition = m_writer.BaseStream.Position;

        m_writer.BaseStream.Position = m_lengthPosition;
        long length=endPosition - m_startPosition;
        m_writer.Write((uint)length);
        m_writer.BaseStream.Position = endPosition;

        m_lengthPosition = long.MinValue;
      }
    }

    public void Dispose()
    {
      Write();
    }
  }

}

