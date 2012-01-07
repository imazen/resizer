#region Copyright & License
/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：GifEncoder.cs
// 文件功能描述：
// 
// 创建标识：jillzhang 
// 修改标识：
// 修改描述：
//
// 修改标识：
// 修改描述：
//----------------------------------------------------------------*/
/*-------------------------New BSD License ------------------
 Copyright (c) 2008, jillzhang
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of jillzhang nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;

namespace Jillzhang.GifUtility
{
    #region Gif编码器GifEncoder
    /// <summary>
    /// Gif编码器GifEncoder
    /// </summary>
    internal class GifEncoder
    {
        #region private fileds      
        byte[] gct;
        short width;
        short heigt = 0;
        Hashtable table = new Hashtable();
        #endregion  

       static  void SetFrames(List<GifFrame> frames,StreamHelper streamHelper,Stream fs)
        {
            foreach (GifFrame f in frames)
            {
                List<byte> list = new List<byte>();
                if (f.GraphicExtension != null)
                {
                    list.AddRange(f.GraphicExtension.GetBuffer());
                }
                f.ImageDescriptor.SortFlag = false;
                f.ImageDescriptor.InterlaceFlag = false;
                list.AddRange(f.ImageDescriptor.GetBuffer());
                if (f.ImageDescriptor.LctFlag)
                {
                    list.AddRange(f.LocalColorTable);
                }
                streamHelper.WriteBytes(list.ToArray());
                int transIndex = -1;

                if (f.GraphicExtension.TransparencyFlag)
                {
                    transIndex = f.GraphicExtension.TranIndex;
                }

                byte[] indexedPixel = GetImagePixels(f.Image, f.LocalColorTable, transIndex);

                LZWEncoder lzw = new LZWEncoder(indexedPixel, (byte)f.ColorDepth);
                lzw.Encode(fs);
                streamHelper.WriteBytes(new byte[] { 0 });
            }
            streamHelper.WriteBytes(new byte[] { 0x3B });
        }

        internal static void Encode(GifImage gifImage, string gifPath)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(gifPath, FileMode.Create);
                StreamHelper streamHelper = new StreamHelper(fs);
                streamHelper.WriteHeader(gifImage.Header);
                streamHelper.WriteLSD(gifImage.LogicalScreenDescriptor);
                if (gifImage.LogicalScreenDescriptor.GlobalColorTableFlag)
                {
                    streamHelper.SetGlobalColorTable(gifImage.GlobalColorTable);
                }
                streamHelper.SetApplicationExtensions(gifImage.ApplictionExtensions);
                streamHelper.SetCommentExtensions(gifImage.CommentExtensions);
                SetFrames(gifImage.Frames, streamHelper, fs);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        static Hashtable GetColotTable(byte[] table, int transIndex)
        {
            int[] tab = new int[table.Length / 3];
            Hashtable hashTab = new Hashtable();
            int i = 0;
            int j = 0;
            while (i < table.Length)
            {
                int color = 0;
                if (j == transIndex)
                {
                    i += 3;
                }
                else
                {
                    int r = table[i++];
                    int g = table[i++];
                    int b = table[i++];
                    int a = 255;
                    color = (int)(a << 24 | (r << 16) | (g << 8) | b);
                }
                if (!hashTab.ContainsKey(color))
                {
                    hashTab.Add(color, j);
                }
                tab[j++] = color;
            }
            return hashTab;
        }
        /**
         * Extracts image pixels into byte array "pixels"
         */
        static byte[] GetImagePixels(Bitmap image, byte[] colorTab, int transIndex)
        {
            int iw = image.Width;
            int ih = image.Height;

            byte[] pixels = new byte[iw * ih];
            Hashtable table = GetColotTable(colorTab, transIndex);
            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, iw, ih), ImageLockMode.ReadOnly, image.PixelFormat);
            unsafe
            {
                int* p = (int*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < iw * ih; i++)
                {
                    int color = p[i];
                    byte index = Convert.ToByte(table[color]);
                    pixels[i] = index;
                }
            }
            image.UnlockBits(bmpData);
            return pixels;
        }      
    }
    #endregion
}
