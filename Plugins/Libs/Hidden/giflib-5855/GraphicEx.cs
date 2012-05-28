#region Copyright & License
/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：GraphicEx.cs
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
    #region 类GraphicEx
    /// <summary>
    /// 图形控制扩展(Graphic Control Extension)这一部分是可选的（需要89a版本），
    /// 可以放在一个图象块(包括图象标识符、局部颜色列表和图象数据)或文本扩展块的前面，
    /// 用来控制跟在它后面的第一个图象（或文本）的渲染(Render)形式
    /// </summary>
    internal class GraphicEx:ExData
    {
        #region private fields
        byte _packed;
        short _delay;
        byte _tranIndex;
        bool _transFlag;
        int _disposalMethod;
        #endregion

        /// <summary>
        /// Block Size - 不包括块终结器，固定值4
        /// </summary>
        internal static readonly byte BlockSize = 4;    

        /// <summary>
        /// i - 用户输入标志
        /// </summary>
        internal bool TransparencyFlag
        {
            get { return _transFlag; }
            set { _transFlag = value; }
        }
      
        /// <summary>
        /// 处置方法(Disposal Method)：指出处置图形的方法，当值为：
        /// 0 - 不使用处置方法
        /// 1 - 不处置图形，把图形从当前位置移去
        /// 2 - 回复到背景色
        /// 3 - 回复到先前状态
        /// 4-7 - 自定义
        /// </summary>
        internal int DisposalMethod
        {
            get { return _disposalMethod; }
            set { _disposalMethod = value; }
        }	
        /// <summary>
        /// Packed
        /// </summary>
        internal byte Packed
        {
            get
            {
                return _packed;
            }
            set
            {
                _packed = value;
            }
        }
        /// <summary>
        /// Delay Time - 单位1/100秒，如果值不为1，表示暂停规定的时间后再继续往下处理数据流
        /// </summary>
        internal short Delay
        {
            get
            {
                return _delay;
            }
            set
            {
                _delay = value;
            }
        }
        /// <summary>
        /// Transparent Color Index - 透明色索引值
        /// </summary>
        internal byte TranIndex
        {
            get
            {
                return _tranIndex;
            }
            set
            {
                _tranIndex = value;
            }
        }
        internal GraphicEx()
        {
        }
        
        internal byte[] GetBuffer()
        {
            List<byte> list = new List<byte>();
            list.Add(GifExtensions.ExtensionIntroducer);
            list.Add(GifExtensions.GraphicControlLabel);
            list.Add(BlockSize);
            int t = 0;
            if (_transFlag)
            {
                t = 1;
            }
            _packed = (byte)((_disposalMethod << 2) | t);
            list.Add(_packed);
            list.AddRange(BitConverter.GetBytes(_delay));
            list.Add(_tranIndex);
            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }
    }
    #endregion
}
