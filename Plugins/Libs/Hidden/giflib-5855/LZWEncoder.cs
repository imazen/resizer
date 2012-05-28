/*----------------------------------------------------------------
// Copyright (C) 2008 jillzhang 版权所有。 
//  
// 文件名：LZW.cs
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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace Jillzhang.GifUtility
{
    internal class LZWEncoder
    {
        /// <summary>
        /// GIF规定编码最大为12bit，最大值即为4096
        /// </summary>
        protected static readonly int MaxStackSize = 4096;
        protected static readonly byte NULLCODE = 0;
        byte colorDepth;
        byte initDataSize;
        byte[] indexedPixel;

        internal LZWEncoder(byte[] indexedPixel, byte colorDepth)
        {
            this.indexedPixel = indexedPixel;
            this.colorDepth = (byte)Math.Max((byte)2, colorDepth);
            initDataSize = this.colorDepth;
        }

        internal void Encode(Stream os)
        {
            //是否是第一步
            bool first = true;
            //初始化前缀
            int prefix = NULLCODE;
            //初始化后缀
            int suffix = NULLCODE;
            //初始化实体
            string entry = string.Format("{0},{1}", prefix, suffix);

            //清除标志
            int ClearFlag = (1 << colorDepth) ;
            //结束标志
            int EndFlag = ClearFlag + 1;

            //编码表
            Dictionary<string, int> CodeTable = new Dictionary<string, int>();

            //当前已处理的索引字节个数
            int releaseCount = 0;

            //可用的编码
            byte codeSize = (byte)(colorDepth + 1);
            int availableCode = EndFlag + 1;
            int mask_Code = (1 << codeSize)-1;

            BitEncoder bitEncoder = new BitEncoder(codeSize);

            os.WriteByte(colorDepth);
            bitEncoder.Add(ClearFlag);
            while (releaseCount < indexedPixel.Length)
            {
                #region 如果是第一个字节
                if (first)
                {
                    //第一次，将后缀suffix设置为第一个索引字节
                    suffix = indexedPixel[releaseCount++];
                    if (releaseCount == indexedPixel.Length)
                    {
                        bitEncoder.Add(suffix);
                        bitEncoder.Add(EndFlag);
                        bitEncoder.End();
                        os.WriteByte((byte)(bitEncoder.Length));
                        os.Write(bitEncoder.OutList.ToArray(), 0, bitEncoder.Length);
                        bitEncoder.OutList.Clear();
                        break;
                    }              
                    first = false;
                    continue;
                }
                #endregion

                #region 前后缀调换,并组成实体
                //后缀变前缀
                prefix = suffix;            
                //后缀再从索引字节中读取
                suffix = indexedPixel[releaseCount++];
                entry = string.Format("{0},{1}", prefix, suffix);
                #endregion               

                #region 如果不认识当前实体，对实体进行编码，并输出前缀
                if (!CodeTable.ContainsKey(entry))
                {
                    //如果当前实体没有被编码过，那么输出前缀          
                    bitEncoder.Add(prefix);
                
                    //并对当前实体进行编码                 
                    CodeTable.Add(entry, availableCode++);
                    
                    if (availableCode > (MaxStackSize-3) )
                    {
                        //插入清除标记，推倒重来
                        CodeTable.Clear();
                        colorDepth = initDataSize;
                        codeSize = (byte)(colorDepth + 1);
                        availableCode = EndFlag + 1;
                        mask_Code = (1 << codeSize) - 1;
                      
                        bitEncoder.Add(ClearFlag);
                        bitEncoder.inBit = codeSize;
                    }
                    else if (availableCode > (1<<codeSize))
                    {
                        //如果当前可用编码大于当前编码位可表示值
                        colorDepth++;                       
                        codeSize = (byte)(colorDepth + 1);
                        bitEncoder.inBit = codeSize;
                        mask_Code = (1 << codeSize) -1;
                    }   
                    if (bitEncoder.Length >= 255)
                    {
                        os.WriteByte((byte)255);
                        os.Write(bitEncoder.OutList.ToArray(), 0,255);
                        if (bitEncoder.Length > 255)
                        {
                            byte[] left_buffer = new byte[bitEncoder.Length - 255];
                            bitEncoder.OutList.CopyTo(255, left_buffer, 0, left_buffer.Length);
                            bitEncoder.OutList.Clear();
                            bitEncoder.OutList.AddRange(left_buffer);
                        }
                        else
                        {
                            bitEncoder.OutList.Clear();
                        }                    
                    }
                }
                #endregion

                #region 如果认识当前实体，将后缀设置为当前实体索引值
                else
                {
                    //将后缀设置为当前实体编码
                    suffix = (int)CodeTable[entry];
                }
                #endregion                 
             
                #region 到了一幅图像的未尾了,写结束标识，并输出当前编码流中剩余数据
                if (releaseCount == indexedPixel.Length)
                {
                    bitEncoder.Add(suffix);
                    bitEncoder.Add(EndFlag);
                    bitEncoder.End();
                    if (bitEncoder.Length > 255)
                    {
                        byte[] left_buffer = new byte[bitEncoder.Length - 255];
                        bitEncoder.OutList.CopyTo(255, left_buffer, 0, left_buffer.Length);
                        bitEncoder.OutList.Clear();
                        bitEncoder.OutList.AddRange(left_buffer);
                        os.WriteByte((byte)left_buffer.Length );
                        os.Write(left_buffer,0,left_buffer.Length);
                    }
                    else
                    {
                        os.WriteByte((byte)(bitEncoder.Length));                   
                        os.Write(bitEncoder.OutList.ToArray(), 0, bitEncoder.Length);
                        bitEncoder.OutList.Clear();
                    }    
                    break;
                }
                #endregion
            }
        }
    }
}
