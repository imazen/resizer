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
    public class PixelData
    {
        int _width;
        int _height;
        int _bitsPerPixel;
        int _numChannels;
        bool _isMerged;

        //Dictionary<int, Channel> _channels;
        List<byte[]> _channelData;

        public enum Compression
        {
            None = 0,
            Rle,
            ZipNoPrediction,
            ZipPrediction,
            Jpeg
        }

        //public class Channel
        //{
        //    PixelData _pixelData;
        //    int _id;
        //    public byte[] Data;
        //}

        public PixelData(int width, int height, int bitsPerPixel, int numChannels, bool isMerged)
        {
            this._width = width;
            this._height = height;
            this._bitsPerPixel = bitsPerPixel;
            this._numChannels = numChannels;
            this._isMerged = isMerged;
            this._channelData = new List<byte[]>();
        }

        public byte[] GetChannelData(int index)
        {
            return this._channelData[index];
        }

        public void Read(BinaryPSDReader reader)
        {
            Compression compression = Compression.None;
            if (this._isMerged)
            {
                compression = (Compression)reader.ReadUInt16();
                for (int i = 0; i < this._numChannels; i++)
                    this.PreReadPixels(reader, compression);
            }

            for (int i = 0; i < this._numChannels; i++)
            {
                if (!this._isMerged)
                {
                    compression = (Compression)reader.ReadUInt16();
                    this.PreReadPixels(reader, compression);
                }
                this._channelData.Add(this.ReadPixels(reader, compression));
            }
        }

        public long[] Write(BinaryPSDWriter writer, byte[] channelData, Compression compression)
        {
            List<byte[]> channelsData = new List<byte[]>();
            channelsData.Add(channelData);
            return this.Write(writer, channelsData, compression);
        }

        public long[] Write(BinaryPSDWriter writer, List<byte[]> channelsData, Compression compression)
        { //returns saved length of each channel's data        
            if (channelsData == null)
                channelsData = this._channelData;
            List<long> headerPositions = new List<long>();
            List<byte[]> headers = new List<byte[]>();

            if (this._isMerged)
            {
                headers = new List<byte[]>();
                writer.Write((ushort)compression);
                for (int i = 0; i < this._numChannels; i++)
                {
                    headerPositions.Add(writer.BaseStream.Position);
                    this.PreWritePixels(writer, compression);
                }
            }

            long[] lengths = new long[channelsData.Count];

            int chNum = 0;
            foreach (byte[] channelData in channelsData)
            {
                long startPos = writer.BaseStream.Position;
                if (!this._isMerged)
                {
                    writer.Write((ushort)compression);
                    headerPositions.Add(writer.BaseStream.Position);
                    this.PreWritePixels(writer, compression);
                }

                byte[] header = this.WritePixels(writer, compression, channelData);
                headers.Add(header);

                lengths[chNum] = writer.BaseStream.Position - startPos;
                chNum++;
            }

            for (int i = 0; i < headers.Count; i++)
			{
                if (headers[i] != null)
                {
                    writer.BaseStream.Position = headerPositions[i];
                    writer.Write(headers[i]);
                }
			}
            writer.BaseStream.Position = writer.BaseStream.Length - 1;

            return lengths;
        }

        public byte[] PreReadPixels(BinaryPSDReader reader, Compression compression)
        {
            switch (compression)
            {
                case Compression.Rle:
                    //ignore rle "header" with bytes per row...
                    reader.BaseStream.Position += this._height * 2;
                    //ushort[] rowLenghtList = new ushort[height];
                    //for (int i = 0; i < height; i++)
                    //    rowLenghtList[i] = reader.ReadUInt16();
                    break;
            }
            return null;
        }

        public byte[] ReadPixels(BinaryPSDReader reader, Compression compression)
        {
            int bytesPerPixelPerChannel = this._bitsPerPixel / 8; // psd.Header.Depth / 8;
            if (bytesPerPixelPerChannel < 1)
                bytesPerPixelPerChannel = 1;

            int bytesPerRow = this._width * bytesPerPixelPerChannel;
            int totalBytes = bytesPerRow * this._height;
            
            byte[] pData = new byte[totalBytes];

            switch (compression)
            {
                case Compression.None:
                    reader.Read(pData, 0, totalBytes);
                    break;

                case Compression.Rle:
                    for (int i = 0; i < this._height; i++)
                    {
                        int offset = i * this._width;
                        int numDecodedBytes = 0;
                        int numChunks = 0;
                        while (numDecodedBytes < this._width)
                        {
                            numDecodedBytes += Endogine.Serialization.RleCodec.DecodeChunk(reader.BaseStream, pData, offset + numDecodedBytes);
                            numChunks++;
                        }
                    }
                    break;

                case Compression.ZipNoPrediction:
                    throw (new Exception("ZIP without prediction, no specification"));

                case Compression.ZipPrediction:
                    throw (new Exception("ZIP with prediction, no specification"));

                default:
                    throw (new Exception("Compression not defined: " + compression));
            }
            
            return pData;
        }


        public void PreWritePixels(BinaryPSDWriter writer, Compression compression)
        {
            if (compression == Compression.Rle)
            {
                for (int i = 0; i < this._height; i++)
                    writer.Write((ushort)0);
            }
        }

        public byte[] WritePixels(BinaryPSDWriter writer, Compression compression, byte[] data)
        {
            switch (compression)
            {
                case Compression.None:
                    writer.Write(data);
                    return null;
                    
                case Compression.Rle:
                    int[] rleRowLenghs = new int[this._height];

                    Endogine.Serialization.RleCodec codec = new Endogine.Serialization.RleCodec();
                    for (int row = 0; row < this._height; row++)
                        rleRowLenghs[row] = Endogine.Serialization.RleCodec.EncodeChunk(writer.BaseStream, data, (long)row * this._width, (long)this._width);

                    System.IO.MemoryStream memStream = new System.IO.MemoryStream();
                    BinaryPSDWriter memWriter = new BinaryPSDWriter(memStream);
                    for (int i = 0; i < rleRowLenghs.Length; i++)
                        memWriter.Write((short)rleRowLenghs[i]);

                    return memStream.ToArray();

                default:
                    throw new Exception("Compression not implemented: " + compression.ToString());
            }
            return null;
        }
    }
}
