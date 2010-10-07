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
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace Endogine.Codecs.Photoshop
{
    /// <summary>
    /// Based on code from Igor Tolmachev's article "Reading Adobe Photoshop images"
    /// http://www.codeproject.com/csharp/SimplePsd.asp
    /// </summary>
    public class PixelsProcessing
    {
        //public static Bitmap ConvertToBitmap(byte[] pData, )
        //{
        //}

    //    public static void WritePixels(BinaryPSDWriter writer, Compression compression, int width, int height, int bitsPerPixel, byte[] data, bool isMergedBitmap)
    //    {
    //        //ColorModes colorMode
    //        if (!isMergedBitmap)
    //            writer.Write((short)compression);

    //        switch (compression)
    //        {
    //            case Compression.None:
    //                writer.Write(data);
    //                break;

    //            case Compression.Rle:
    //                long lengthPosition = writer.BaseStream.Position;
    //                int[] rleRowLenghs = new int[height];

    //                for (int i = 0; i < rleRowLenghs.Length; i++)
    //                    writer.Write((short)0);

    //                for (int row = 0; row < height; row++)
    //                    rleRowLenghs[row] = RleCodec.EncodeChunk(writer.BaseStream, data, (long)row * width, (long)width);

    //                writer.BaseStream.Position = lengthPosition;

    //                for (int i = 0; i < rleRowLenghs.Length; i++)
    //                    writer.Write((short)rleRowLenghs[i]);

    //                writer.BaseStream.Position = writer.BaseStream.Length - 1;
    //                break;
    //        }
    //    }

    //    public static void WritePixelsMerged(BinaryPSDWriter writer, Compression compression, int width, int height, int numChannels, byte[][] data)
    //    {
    //        writer.Write((short)compression);

    //        switch (compression)
    //        {
    //            case Compression.None:
    //                break;

    //            case Compression.Rle:
    //                long lengthPosition = writer.BaseStream.Position;
    //                int[] rleRowLenghs = new int[height * numChannels];

    //                for (int i = 0; i < rleRowLenghs.Length; i++)
    //                    writer.Write((short)0);

    //                for (int ch = 0; ch < numChannels; ch++)
    //                {
    //                    int startIdx = ch * height;

    //                    for (int row = 0; row < height; row++)
    //                        rleRowLenghs[row + startIdx] = RleCodec.EncodeChunk(writer.BaseStream, data[ch], (long)row * width, (long)width);
    //                }

    //                writer.BaseStream.Position = lengthPosition;

    //                for (int i = 0; i < rleRowLenghs.Length; i++)
    //                    writer.Write((short)rleRowLenghs[i]);

    //                writer.BaseStream.Position = writer.BaseStream.Length - 1;
    //                break;
    //        }
    //    }

    //    public static byte[] ReadPixels(BinaryPSDReader reader, int width, int height, int bitsPerPixel, bool isMergedBitmap)
    //    {
    //        if (!isMergedBitmap)
    //        {
    //            //TODO: read mask data (always uncompressed)
    //            //reader.ReadBytes((int)maskChannel.Length);
    //        }


    //        short nCompression = reader.ReadInt16();
    //        Compression compression = (Compression)nCompression;

    //        int bytesPerPixelPerChannel = bitsPerPixel / 8; // psd.Header.Depth / 8;
    //        if (bytesPerPixelPerChannel < 1)
    //            bytesPerPixelPerChannel = 1;

    //        int bytesPerRow = width * bytesPerPixelPerChannel;
    //        int totalBytes = bytesPerRow * height;
            
    //        byte[] pData = new byte[totalBytes];

    //        switch (compression)
    //        {
    //            case Compression.None:
    //                reader.Read(pData, 0, totalBytes);
    //                break;

    //            case Compression.Rle:
    //                // If it's NOT the FinalBitmap, the RLE-compressed data is proceeded by a 2-byte data count for each row in the data
    //                if (!isMergedBitmap)
    //                {
    //                    ushort[] rowLenghtList = new ushort[height];
    //                    for (int i = 0; i < height; i++)
    //                        rowLenghtList[i] = reader.ReadUInt16();
    //                }

    //                for (int i = 0; i < height; i++)
    //                {
    //                    int offset = i * width;
    //                    int numDecodedBytes = 0;
    //                    int numChunks = 0;
    //                    while (numDecodedBytes < width)
    //                    {
    //                        numDecodedBytes += RleCodec.DecodeChunk(reader.BaseStream, pData, offset + numDecodedBytes);
    //                        numChunks++;
    //                    }
    //                }
    //                break;

    //            case Compression.ZipNoPrediction:
    //                throw (new Exception("ZIP without prediction, no specification"));

    //            case Compression.ZipPrediction:
    //                throw (new Exception("ZIP with prediction, no specification"));

    //            default:
    //                throw (new Exception("Unknown compression format: " + nCompression.ToString()));
    //        }
            
    //        return pData;
    //    }

    //    public BitmapHelpers.Canvas ConvertToCanvas(List<byte[]> data, ColorModes colorMode, int bitsPerChannelPerPixel)
    //    {
    //        return null;
    //    }

    //    public BitmapHelpers.Canvas ConvertToCanvas(byte[] data, ColorModes colorMode, int numChannels, int bitsPerChannelPerPixel)
    //    {
    //        return null;
    //    }
    }
}


            //                    case 1:	// RLE compression
            //                    {
            //                        //for(long i=0;i<nTotalBytes;i++) pData[i] = 254;
            //                        //for(long i=0;i<nTotalBytes;i++) pDest[i] = 254;

            //                        byte ByteValue = 0x00;

            //                        int nPointer = 0;

            //                        // which we're going to just skip.
            //                        if (a_bFinalBitmap)
            //                            reader.BaseStream.Position += nHeight * psd.Header.Channels*2;
            //                        else
            //                        {
            //                            if (psd.Header.Channels == 3)
            //                                reader.BaseStream.Position += nHeight *4;
            //                            else
            //                                reader.BaseStream.Position += nHeight *4 + nHeight*psd.Header.Channels*2;
            //                        }

            //                        for (int channel=0; channel<psd.Header.Channels; channel++)
            //                        {
            //                            // Read the RLE data.

            //                            if (!a_bFinalBitmap)
            //                            {
            //                                reader.BaseStream.Position +=2; //TODO: read each channel separately as 8-bit rle!
            //                                reader.BaseStream.Position += nHeight *2;
            //                                //reader.JumpToEvenNthByte(4);
            //                            }
            //                            int nNumWrittenPixelsInChannel = 0;
            //                            while (nNumWrittenPixelsInChannel < nPixels)
            //                            {
            //                                nPointer += RleCodec.DecodeChunk(reader.BaseStream, pData, nPointer);
            //                            }
            //                            //TODO: if not final, each channel is encoded separately with RLE or not
            ////							if (!a_bFinalBitmap)
            ////								reader.BaseStream.Position+=2;
            //                        }

            //                        int nPixelCounter = 0;
            //                        nPointer = 0;

            //                        for(int nColour = 0; nColour<psd.Header.Channels; ++nColour)
            //                        {
            //                            nPixelCounter = nColour*bytesPerPixelPerChannel;
            //                            for(int nPos=0; nPos<nPixels; ++nPos)
            //                            {
            //                                for(int j=0;j<bytesPerPixelPerChannel;j++)
            //                                    pDest[nPixelCounter+j] = pData[nPointer+j];

            //                                nPointer++;

            //                                nPixelCounter += psd.Header.Channels*bytesPerPixelPerChannel;
            //                            }
            //                        }

            //                        for(int i=0;i<nTotalBytes;i++) pData[i]=pDest[i];
            //                    }
            //                        break;


            //protected static Bitmap CreateBitmap(int cx, int cy, int ppm_x, int ppm_y, short BitCount)
            //{
            //    PixelFormat format = PixelFormat.Format24bppRgb;
            //    if (BitCount == 8)
            //        format = PixelFormat.Format8bppIndexed;
            //    else if (BitCount == 48)
            //        format = PixelFormat.Format48bppRgb;

            //    Bitmap bmp = new Bitmap(cx, cy, format);
            //    bmp.SetResolution((float)ppm_x/39.37f, (float)ppm_y/39.37f); //pixelsPerMeter -> dpi
            //    Graphics g = Graphics.FromImage(bmp);
            //    g.FillRectangle(new SolidBrush(Color.White), 0,0,cx,cy);
            //    return bmp;
            //}

            //        protected static void ProccessBuffer(byte [] pData, Bitmap bmp)
            //        {
            //            int nHeight = (int)psd.Header.Rows; //.nHeight;
            //            int nWidth = (int)psd.Header.Columns; //.nWidth;
            //            short bytesPerPixelPerChannel = (short)(psd.Header.Depth/8); //.nBitsPerPixel/8);
            //            int nPixels = nWidth * nHeight;
            //            int nTotalBytes = nPixels * bytesPerPixelPerChannel * psd.Header.Channels;

            //            switch (psd.Header.ColorMode)
            //            {
            //                case ColorModes.Grayscale:
            //                case ColorModes.Duotone:
            //                {
            //                    //hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
            //                    //hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);
            //                    int nCounter = 0;

            //                    byte [] ColorValue = new byte[64];

            //                    for(int nRow=0; nRow<nHeight; ++nRow)
            //                    {
            //                        for(int nCol=0; nCol<nWidth; ++nCol)
            //                        {
            //                            for(int i=0;i<bytesPerPixelPerChannel;i++)
            //                                ColorValue[i] = pData[nCounter+i];

            //                            SwapBytes(ColorValue,bytesPerPixelPerChannel);

            //                            int nValue = BitConverter.ToInt32(ColorValue,0);
            //                            if(psd.Header.Depth == 16) //nBitsPerPixel
            //                                nValue = nValue/256;

            //                            nValue = Math.Max(0,Math.Min(255,nValue));
            //                            bmp.SetPixel(nCol, nRow, Color.FromArgb(nValue,nValue,nValue));
            //                            nCounter += bytesPerPixelPerChannel;
            //                        }
            //                    }
            //                    //WinInvoke32.SelectObject(hdcMemory, hbmpOld);
            //                    //WinInvoke32.DeleteDC(hdcMemory);
            //                }
            //                    break;

            //                case ColorModes.Indexed:
            //                {
            ////					// pData holds the indices of loop through the palette and set the correct RGB
            ////					// 8bpp are supported
            //                    if(psd.ColorData.Length==768 && psd.NumColors>0)
            //                    {
            //                        int nRow = 0;
            //                        int nCol = 0;

            //                        for(int nCounter=0; nCounter<nTotalBytes; ++nCounter)
            //                        {
            //                            int nIndex = (int)pData[nCounter];
            //                            int nRed = (int)psd.ColorData[nIndex];
            //                            int nGreen = (int)psd.ColorData[nIndex+256];
            //                            int nBlue = (int)psd.ColorData[nIndex+2*256];

            //                            bmp.SetPixel(nCol,nRow,Color.FromArgb(nRed, nGreen, nBlue));
            //                            nCol++;
            //                            if (nWidth <= nCol)
            //                            {
            //                                nCol = 0;
            //                                nRow++;
            //                            }
            //                        }
            //                    }
            //                }
            //                    break;

            //                case ColorModes.RGB:
            //                {
            //                    int nBytesToRead = psd.Header.Depth/8; //.nBitsPerPixel/8;
            //                    if (nBytesToRead == 2)
            //                        nBytesToRead = 1;

            //                    int nRow = 0;
            //                    int nCol = 0;
            //                    byte [] ColorValue = new byte[8];

            //                    for(int nCounter = 0; nCounter < nTotalBytes; nCounter += psd.Header.Channels * nBytesToRead)
            //                    {
            //                        Array.Copy(pData,nCounter,ColorValue,0,nBytesToRead);
            //                        SwapBytes(ColorValue,nBytesToRead);
            //                        int nRed = BitConverter.ToInt32(ColorValue,0);

            //                        Array.Copy(pData,nCounter+nBytesToRead,ColorValue,0,nBytesToRead);
            //                        SwapBytes(ColorValue,nBytesToRead);
            //                        int nGreen = BitConverter.ToInt32(ColorValue,0);

            //                        Array.Copy(pData,nCounter+2*nBytesToRead,ColorValue,0,nBytesToRead);
            //                        SwapBytes(ColorValue,nBytesToRead);
            //                        int nBlue = BitConverter.ToInt32(ColorValue,0);

            //                        bmp.SetPixel(nCol, nRow, Color.FromArgb(nRed, nGreen, nBlue));
            //                        nCol++;
            //                        if (nWidth <= nCol )
            //                        {
            //                            nCol = 0;
            //                            nRow++;
            //                        }
            //                    }
            //                }
            //                    break;

            //                case ColorModes.CMYK:
            //                {
            //                    double C, M, Y, K;
            //                    double exC, exM, exY, exK;

            //                    int nRow = 0;
            //                    int nCol = 0;

            //                    byte [] ColorValue = new byte[8];

            //                    double dMaxColours = Math.Pow(2, psd.Header.Depth); //.nBitsPerPixel);

            //                    Color crPixel = Color.White;

            //                    for(int nCounter = 0; nCounter < nTotalBytes; nCounter += 4*bytesPerPixelPerChannel)
            //                    {
            //                        Array.Copy(pData,nCounter,ColorValue,0,bytesPerPixelPerChannel);
            //                        SwapBytes(ColorValue,bytesPerPixelPerChannel);
            //                        exC = (double)BitConverter.ToUInt32(ColorValue,0);

            //                        Array.Copy(pData,nCounter+bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            //                        SwapBytes(ColorValue,bytesPerPixelPerChannel);
            //                        exM = (double)BitConverter.ToUInt32(ColorValue,0);

            //                        Array.Copy(pData,nCounter+2*bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            //                        SwapBytes(ColorValue,bytesPerPixelPerChannel);
            //                        exY = (double)BitConverter.ToUInt32(ColorValue,0);

            //                        Array.Copy(pData,nCounter+3*bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            //                        SwapBytes(ColorValue,bytesPerPixelPerChannel);
            //                        exK = (double)BitConverter.ToUInt32(ColorValue,0);

            //                        C = (1.0 - exC/dMaxColours);
            //                        M = (1.0 - exM/dMaxColours);
            //                        Y = (1.0 - exY/dMaxColours);
            //                        K = (1.0 - exK/dMaxColours);

            //                        crPixel = (new ColorEx.ColorCmyk((float)C, (float)M, (float)Y, (float)K)).ColorRGBA;

            //                        bmp.SetPixel(nCol, nRow, crPixel);
            //                        nCol++;
            //                        if(nWidth<= nCol)
            //                        {
            //                            nCol = 0;
            //                            nRow++;
            //                        }
            //                    }
            //                }
            //                    break;

            //                case ColorModes.Multichannel:
            //                {
            ////					double C, M, Y, K;
            ////					double exC, exM, exY, exK;
            ////
            ////					int nRow = 0;
            ////					int nCol = 0;
            ////					int nColor = 0;
            ////
            ////					byte [] ColorValue = new byte[8];
            ////
            ////					double dMaxColours = Math.Pow(2, psd.Header.Depth); //.nBitsPerPixel);
            ////
            ////					Color crPixel = Color.White;
            ////
            ////					// assume format is in either CMY or CMYK
            ////					if(psd.Header.Channels>=3)
            ////					{
            ////						for(int nCounter = 0; nCounter < nTotalBytes; nCounter += psd.Header.Channels * bytesPerPixelPerChannel)
            ////						{
            ////							Array.Copy(pData,nCounter,ColorValue,0,bytesPerPixelPerChannel);
            ////							SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////							exC = (double)BitConverter.ToUInt32(ColorValue,0);
            ////
            ////							Array.Copy(pData,nCounter+bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            ////							SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////							exM = (double)BitConverter.ToUInt32(ColorValue,0);
            ////
            ////							Array.Copy(pData,nCounter+2*bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            ////							SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////							exY = (double)BitConverter.ToUInt32(ColorValue,0);
            ////							
            ////							C = (1.0 - exC/dMaxColours);
            ////							M = (1.0 - exM/dMaxColours);
            ////							Y = (1.0 - exY/dMaxColours);
            ////							K = 0;
            ////							
            ////							if(psd.Header.Channels == 4)
            ////							{
            ////								Array.Copy(pData,nCounter+3*bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            ////								SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////								exK = (double)BitConverter.ToUInt32(ColorValue,0);
            ////
            ////								K = (1.0 - exK/dMaxColours);
            ////							}
            ////
            ////							crPixel = Endogine.ColorFunctions.CMYKToRGB(C, M, Y, K);
            ////
            ////							nColor = ColorTranslator.ToWin32(crPixel);
            ////							WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
            ////							nCol++;
            ////							if ( nWidth <= nCol )
            ////							{
            ////								nCol = 0;
            ////								nRow++;
            ////							}
            ////						}
            ////					}
            //                }
            //                    break;

            //                case ColorModes.Lab:
            //                {
            ////					int L, a, b;
            ////
            ////					int nRow = 0;
            ////					int nCol = 0;
            ////					int nColor = 0;
            ////
            ////					byte [] ColorValue = new byte[64];
            ////
            ////					double exL, exA, exB;
            ////					double L_coef, a_coef, b_coef;
            ////					double dMaxColours = Math.Pow(2, psd.Header.Depth); //.nBitsPerPixel);
            ////
            ////					L_coef = dMaxColours/100.0;
            ////					a_coef = dMaxColours/256.0;
            ////					b_coef = dMaxColours/256.0;
            ////
            ////					Color crPixel = Color.White;
            ////					
            ////					for(int nCounter = 0; nCounter < nTotalBytes; nCounter += 3 * bytesPerPixelPerChannel)
            ////					{
            ////						Array.Copy(pData,nCounter,ColorValue,0,bytesPerPixelPerChannel);
            ////						SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////						exL = (double)BitConverter.ToUInt32(ColorValue,0);
            ////						
            ////						Array.Copy(pData,nCounter+bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            ////						SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////						exA = (double)BitConverter.ToUInt32(ColorValue,0);
            ////
            ////						Array.Copy(pData,nCounter+2*bytesPerPixelPerChannel,ColorValue,0,bytesPerPixelPerChannel);
            ////						SwapBytes(ColorValue,bytesPerPixelPerChannel);
            ////						exB = (double)BitConverter.ToUInt32(ColorValue,0);
            ////
            ////						L = (int)(exL/L_coef);
            ////						a = (int)(exA/a_coef - 128.0);
            ////						b = (int)(exB/b_coef - 128.0);
            ////
            ////						crPixel = Endogine.ColorFunctions.LabToRGB(L, a, b);
            ////
            ////						nColor = ColorTranslator.ToWin32(crPixel);
            ////						WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
            ////						nCol++;
            ////						if(nWidth<=nCol)
            ////						{
            ////							nCol = 0;
            ////							nRow++;
            ////						}
            ////					}
            //                }
            //                    break;
            //            }
            //        }

            //protected void SwapBytes(byte [] array, int nLength)
            //{
            //        for(long i=0; i<nLength/2; ++i) 
            //        {
            //            byte t = array[i];
            //            array[i] = array[nLength - i - 1];
            //            array[nLength - i - 1] = t;
            //        }

            //}