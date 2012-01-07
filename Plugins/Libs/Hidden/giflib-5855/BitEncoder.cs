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

namespace Jillzhang.GifUtility
{

    internal class BitEncoder
    {
        /// <summary>
        /// 上一次处理剩余的bit数
        /// </summary>
        private int current_Bit = 0;

        /// <summary>
        /// 输出字节数据的集合
        /// </summary>
        internal List<Byte> OutList = new List<byte>();

        /// <summary>
        /// 当前输出字节数据长度
        /// </summary>
        internal int Length
        {
            get
            {
                return OutList.Count;
            }
        }
        int current_Val;     

        internal int inBit = 8;

        internal BitEncoder(int init_bit)
        {
            this.inBit = init_bit;
        }

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="inByte">输入数据</param>
        /// <param name="inBit">输入数据的bit位数</param>
        internal void Add(int inByte)
        {         
         
            current_Val  |= (inByte << (current_Bit));

            current_Bit += inBit;
         
            while (current_Bit >= 8)
            {   
                byte out_Val = (byte)(current_Val & 0XFF);
                current_Val = current_Val >> 8;
                current_Bit -= 8;             
                OutList.Add(out_Val);
            }
        }


        internal void End()
        {
            while (current_Bit > 0)
            {
                byte out_Val = (byte)(current_Val & 0XFF);
                current_Val = current_Val >> 8;
                current_Bit -= 8;
                OutList.Add((byte)out_Val);
            }
        }
    }
}
