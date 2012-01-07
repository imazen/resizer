#region Copyright & License
/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：StreamHelper.cs
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

namespace Jillzhang.GifUtility
{
    internal class StreamHelper
    {
        Stream stream;
        internal StreamHelper(Stream stream)
        {
            this.stream = stream;
        }
        //读取指定长度的字节字节
        internal byte[] ReadByte(int len)
        {
            byte[] buffer = new byte[len];
            stream.Read(buffer, 0, len);
            return buffer;
        }
        /// <summary>
        /// 读取一个字节
        /// </summary>
        /// <returns></returns>
        internal int Read()
        {
            return stream.ReadByte();
        }

        internal short ReadShort()
        {
            byte[] buffer = new byte[2];
            stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }

        internal string ReadString(int length)
        {
            return new string(ReadChar(length));
        }

        internal char[] ReadChar(int length)
        {
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            char[] charBuffer = new char[length];
            buffer.CopyTo(charBuffer, 0);
            return charBuffer;
        }

        internal void WriteString(string str)
        {
            char[] charBuffer = str.ToCharArray();
            byte[] buffer = new byte[charBuffer.Length];
            int index = 0;
            foreach (char c in charBuffer)
            {
                buffer[index] = (byte)c;
                index++;
            }
            WriteBytes(buffer);
        }

        internal void WriteBytes(byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        #region 从文件流中读取应用程序扩展块
        /// <summary>
        /// 从文件流中读取应用程序扩展块
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal ApplicationEx GetApplicationEx(Stream stream)
        {
            ApplicationEx appEx = new ApplicationEx();
            int blockSize = Read();
            if (blockSize != ApplicationEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            appEx.ApplicationIdentifier = ReadChar(8);
            appEx.ApplicationAuthenticationCode = ReadChar(3);
            int nextFlag = Read();
            appEx.Datas = new List<DataStruct>();
            while (nextFlag != 0)
            {
                DataStruct data = new DataStruct(nextFlag, stream);
                appEx.Datas.Add(data);
                nextFlag = Read();
            }
            return appEx;
        }
        #endregion

        #region 从文件数据流中读取注释扩展块
        internal CommentEx GetCommentEx(Stream stream)
        {
            CommentEx cmtEx = new CommentEx();
            StreamHelper streamHelper = new StreamHelper(stream);
            cmtEx.CommentDatas = new List<string>();
            int nextFlag = streamHelper.Read();
            cmtEx.CommentDatas = new List<string>();
            while (nextFlag != 0)
            {
                int blockSize = nextFlag;
                string data = streamHelper.ReadString(blockSize);
                cmtEx.CommentDatas.Add(data);
                nextFlag = streamHelper.Read();
            }
            return cmtEx;
        }
        #endregion

        #region 从文件数据流中读取注释扩展块
        /// <summary>
        /// 从文件数据流中读取图形文本扩展(Plain Text Extension)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal PlainTextEx GetPlainTextEx(Stream stream)
        {
            PlainTextEx pltEx = new PlainTextEx();
            int blockSize = Read();
            if (blockSize != PlainTextEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            pltEx.XOffSet = ReadShort();
            pltEx.YOffSet = ReadShort();
            pltEx.Width = ReadShort();
            pltEx.Height = ReadShort();
            pltEx.CharacterCellWidth = (byte)Read();
            pltEx.CharacterCellHeight = (byte)Read();
            pltEx.ForegroundColorIndex = (byte)Read();
            pltEx.BgColorIndex = (byte)Read();
            int nextFlag = Read();
            pltEx.TextDatas = new List<string>();
            while (nextFlag != 0)
            {
                blockSize = nextFlag;
                string data = ReadString(blockSize);
                pltEx.TextDatas.Add(data);
                nextFlag = Read();
            }
            return pltEx;
        }
        #endregion

        #region 从文件数据流中读取注释扩展块
        /// <summary>
        /// 从文件数据流中读取 图象标识符(Image Descriptor)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal ImageDescriptor GetImageDescriptor(Stream stream)
        {
            ImageDescriptor ides = new ImageDescriptor();
            ides.XOffSet = ReadShort();
            ides.YOffSet = ReadShort();
            ides.Width = ReadShort();
            ides.Height = ReadShort();

            ides.Packed = (byte)Read();
            ides.LctFlag = ((ides.Packed & 0x80) >> 7) == 1;
            ides.InterlaceFlag = ((ides.Packed & 0x40) >> 6) == 1;
            ides.SortFlag = ((ides.Packed & 0x20) >> 5) == 1;
            ides.LctSize = (2 << (ides.Packed & 0x07));
            return ides;
        }
        #endregion

        #region 从文件数据流中读取图形控制扩展(Graphic Control Extension)
        /// <summary>
        /// 从文件数据流中读取图形控制扩展(Graphic Control Extension)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal GraphicEx GetGraphicControlExtension(Stream stream)
        {
            GraphicEx gex = new GraphicEx();
            int blockSize = Read();
            if (blockSize != GraphicEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            gex.Packed = (byte)Read();
            gex.TransparencyFlag = (gex.Packed & 0x01) == 1;
            gex.DisposalMethod = (gex.Packed & 0x1C) >> 2;
            gex.Delay = ReadShort();
            gex.TranIndex = (byte)Read();
            Read();
            return gex;
        }
        #endregion

        #region 从文件数据流中逻辑屏幕标识符(Logical Screen Descriptor)
        /// <summary>
        /// 从文件数据流中读取图形控制扩展(Graphic Control Extension)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal LogicalScreenDescriptor GetLCD(Stream stream)
        {
            LogicalScreenDescriptor lcd = new LogicalScreenDescriptor();
            lcd.Width = ReadShort();
            lcd.Height = ReadShort();
            lcd.Packed = (byte)Read();
            lcd.GlobalColorTableFlag = ((lcd.Packed & 0x80) >> 7) == 1;
            lcd.ColorResoluTion = (byte)((lcd.Packed & 0x60) >> 5);
            lcd.SortFlag = (byte)(lcd.Packed & 0x10) >> 4;
            lcd.GlobalColorTableSize = 2 << (lcd.Packed & 0x07);
            lcd.BgColorIndex = (byte)Read();
            lcd.PixcelAspect = (byte)Read();
            return lcd;
        }
        #endregion

        #region 写文件头
        /// <summary>
        /// 写文件头
        /// </summary>
        /// <param name="header">文件头</param>
        internal void WriteHeader(string header)
        {
            WriteString(header);
        }
        #endregion

        #region 写逻辑屏幕标识符
        /// <summary>
        /// 写逻辑屏幕标识符
        /// </summary>
        /// <param name="lsd"></param>
        internal void WriteLSD(LogicalScreenDescriptor lsd)
        {
            WriteBytes(lsd.GetBuffer());         
        }
        #endregion

        #region 写全局颜色表
        /// <summary>
        /// 写全局颜色表
        /// </summary>
        /// <param name="buffer">全局颜色表</param>
        internal void SetGlobalColorTable(byte[] buffer)
        {
            WriteBytes(buffer);           
        }
        #endregion

        #region 写入注释扩展集合
        /// <summary>
        /// 写入注释扩展集合
        /// </summary>
        /// <param name="comments">注释扩展集合</param>
        internal void SetCommentExtensions(List<CommentEx> comments)
        {
            foreach (CommentEx ce in comments)
            {
                WriteBytes(ce.GetBuffer());
            }
        }
        #endregion

        #region 写入应用程序展集合
        /// <summary>
        /// 写入应用程序展集合
        /// </summary>
        /// <param name="comments">写入应用程序展集合</param>
        internal void SetApplicationExtensions(List<ApplicationEx> applications)
        {
            foreach (ApplicationEx ap in applications)
            {
                WriteBytes(ap.GetBuffer());
            }
        }
        #endregion
    }
}
