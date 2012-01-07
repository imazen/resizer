#region Copyright & License
/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：ApplicationEx.cs
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
using System.Runtime.InteropServices;

namespace Jillzhang.GifUtility
{
    #region 结构ApplicationEx
    /// <summary>
    /// 应用程序扩展(Application Extension)-这是提供给应用程序自己使用的
    /// （需要89a版本），应用程序可以在这里定义自己的标识、信息等   
    /// </summary>  
    internal struct ApplicationEx
    {
        #region 结构字段  

        /// <summary>
        /// Block Size - 块大小，固定值11
        /// </summary>
        internal static readonly byte BlockSize = 0X0B;

        /// <summary>
        /// Application Identifier - 用来鉴别应用程序自身的标识(8个连续ASCII字符)
        /// </summary>      
        internal char[] ApplicationIdentifier;

        /// <summary>
        /// Application Authentication Code - 应用程序定义的特殊标识码(3个连续ASCII字符)
        /// </summary>
        internal char[] ApplicationAuthenticationCode;

        /// <summary>
        /// 应用程序自定义数据块 - 一个或多个数据块组成，保存应用程序自己定义的数据
        /// </summary>
        internal List<DataStruct> Datas;

     
        #endregion

        #region 方法函数
        /// <summary>
        /// 获取应用程序扩展的字节数组
        /// </summary>
        /// <returns></returns>
        internal byte[] GetBuffer()
        {
            List<byte> list = new List<byte>();
            list.Add(GifExtensions.ExtensionIntroducer);
            list.Add(GifExtensions.ApplicationExtensionLabel);
            list.Add(BlockSize);
            if (ApplicationIdentifier == null)
            {
                ApplicationIdentifier = "NETSCAPE".ToCharArray();
            }
            foreach (char c in ApplicationIdentifier)
            {
                list.Add((byte)c);
            }
            if (ApplicationAuthenticationCode == null)
            {
                ApplicationAuthenticationCode = "2.0".ToCharArray();
            }
            foreach (char c in ApplicationAuthenticationCode)
            {
                list.Add((byte)c);
            }
            if (Datas != null)
            {
                foreach (DataStruct ds in Datas)
                {
                    list.AddRange(ds.GetBuffer());
                }
            }
            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }
        #endregion
    }
    #endregion
}
