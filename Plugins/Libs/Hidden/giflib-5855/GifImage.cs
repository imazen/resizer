#region Copyright & License
/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：GifImage.cs
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
using System.Collections;

namespace Jillzhang.GifUtility
{
    #region 类GifImage - 描述Gif的类
    /// <summary>
    /// 类GifImage - 描述Gif的类
    /// </summary>
    internal class GifImage
    {
        #region 背景图片的长度 
        /// <summary>
        /// 背景图片的长度
        /// </summary>
        internal short Width
        {
            get
            {
                return lcd.Width;
            }         
        }
        #endregion

        #region 背景图片的高度
        /// <summary>
        /// 背景图片的高度
        /// </summary>
        internal short Height
        {
            get
            {
                return lcd.Height;
            }          
        }
        #endregion

        #region gif文件头，可能情况有两种:GIF89a或者GIF87a
        string header = "";
        internal string Header
        {
            get { return header; }
            set { header = value; }
        }
        #endregion

        #region 全局颜色列表
        private byte[] gct;
        /// <summary>
        /// 全局颜色列表
        /// </summary>
        internal byte[] GlobalColorTable
        {
            get
            {
                return gct;
            }
            set
            {
                gct = value;
            }
        }
        #endregion

        #region Gif的调色板
        /// <summary>
        /// Gif的调色板
        /// </summary>
        internal Color32[] Palette
        {
            get
            {
                Color32[] act = PaletteHelper.GetColor32s(GlobalColorTable);
                act[lcd.BgColorIndex] = new Color32(0);
                return act;
            }
        }
        #endregion

        #region 全局颜色的索引表
        Hashtable table = new Hashtable();
        /// <summary>
        /// 全局颜色的索引表
        /// </summary>
        internal Hashtable GlobalColorIndexedTable
        {
            get { return table; }
        }
        #endregion

        #region 注释扩展块集合
        List<CommentEx> comments = new List<CommentEx>();
        /// <summary>
        /// 注释块集合
        /// </summary>
        internal List<CommentEx> CommentExtensions
        {
            get
            {
                return comments;
            }
            set
            {
                comments = value;
            }
        }
        #endregion

        #region 应用程序扩展块集合
        List<ApplicationEx> applictions = new List<ApplicationEx>();
        /// <summary>
        /// 应用程序扩展块集合
        /// </summary>
        internal List<ApplicationEx> ApplictionExtensions
        {
            get
            {
                return applictions;
            }
            set
            {
                applictions = value;
            }
        }
        #endregion

        #region 图形文本扩展集合
        List<PlainTextEx> texts = new List<PlainTextEx>();
        /// <summary>
        /// 图形文本扩展集合
        /// </summary>
        internal List<PlainTextEx> PlainTextEntensions
        {
            get
            {
                return texts;
            }
            set
            {
                texts = value;
            }
        }
        #endregion

        #region 逻辑屏幕描述
        LogicalScreenDescriptor lcd;
        /// <summary>
        /// 逻辑屏幕描述
        /// </summary>
        internal LogicalScreenDescriptor LogicalScreenDescriptor
        {
            get
            {
                return lcd;
            }
            set
            {
                lcd = value;
            }
        }
        #endregion

        #region 解析出来的帧集合
        List<GifFrame> frames = new List<GifFrame>();
        /// <summary>
        /// 解析出来的帧集合
        /// </summary>
        internal List<GifFrame> Frames
        {
            get
            {
                return frames;
            }
            set
            {
                frames = value;
            }
        }
        #endregion
    }
    #endregion
}
