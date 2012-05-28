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
    internal unsafe class OcTreeNode
    {
        private static int[] mask = new int[8] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
        #region 公共属性
        internal int ColorDepth;
        internal int Level = 0;
        internal bool Leaf = false;     
        internal OcTreeNode[] Children;
        /// <summary>
        /// 红色的数量
        /// </summary>
        internal int Red = 0;
        /// <summary>
        /// 绿色的数量
        /// </summary>
        internal int Green = 0;
        /// <summary>
        /// 蓝色的数量
        /// </summary>
        internal int Blue = 0;
        //处理的颜色像素数量
        internal int PixelCount = 0;
        internal OcTreeNode NextReducible;
        private int paletteIndex = 0;
        #endregion

        #region 八叉树的构造函数
        /// <summary>
        /// 八叉树的构造函数
        /// </summary>
        /// <param name="leaf">是否是叶子节点</param>
        /// <param name="level">层级</param>
        /// <param name="parent">父节点</param>
        internal OcTreeNode(int colorDepth,int level,OcTree tree)
        {
            this.ColorDepth = colorDepth;
            this.Leaf = (colorDepth==level);
            this.Level = level;
            if (!Leaf)
            {
                NextReducible = tree.ReducibleNodes[level];
                tree.ReducibleNodes[level] = this;
                Children = new OcTreeNode[8];
            }
            else
            {
                tree.IncrementLeaves();
            }
        }
        #endregion   
    
        internal void GetPalltte( List<Color32> palltte)
        {
            if (Leaf)
            {
                paletteIndex++;
                //如果达到了叶子，则进行颜色处理
                Color32 color = new Color32();
                color.Alpha = 255;
                color.Red = (byte)(Red / PixelCount);
                color.Green = (byte)(Green / PixelCount);
                color.Blue = (byte)(Blue / PixelCount);
                palltte.Add(color);
            }
            else
            {
                for (int i = 0; i < ColorDepth; i++)
                {
                    if (Children[i] != null)
                    {
                        Children[i].GetPalltte(palltte);
                    }
                }
            }         
        }
        internal int Reduce()
        {
            Red = Green = Blue = 0;
            int childrenCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Children[i] != null)
                {
                    Red += Children[i].Red;
                    Blue += Children[i].Blue;
                    Green += Children[i].Green;
                    PixelCount += Children[i].PixelCount;
                    childrenCount++;
                    Children[i] = null;
                }
            }
            Leaf = true;
            return childrenCount - 1;
        }
        internal void AddColor(Color32* pixel, int level,OcTree tree)
        {
            //如果是树叶了，表示一个颜色像素添加完成
            if (this.Leaf)
            {
                Increment(pixel);
                tree.TracePrevious(this);
                return;
            }
            int shift = 7 - level;
            int index = ((pixel->Red & mask[level]) >> (shift - 2)) |
                          ((pixel->Green & mask[level]) >> (shift - 1)) |
                          ((pixel->Blue & mask[level]) >> (shift));
            OcTreeNode child = Children[index];
            if (child == null)
            {
                child = new OcTreeNode(ColorDepth, level+1, tree);
                Children[index] = child;
            }            
            child.AddColor(pixel, ++level,tree);            
        }

        internal int GetPaletteIndex(Color32* pixel,int level)
        {
            int pindex = paletteIndex;
            if (!Leaf)
            {
                int shift = 7 - level;
                int index = ((pixel->Red & mask[level]) >> (shift - 2)) |
                              ((pixel->Green & mask[level]) >> (shift - 1)) |
                              ((pixel->Blue & mask[level]) >> (shift));
                OcTreeNode child = Children[index];
                if (child != null)
                {
                    child.GetPaletteIndex(pixel, level + 1);
                }
                else
                    throw new Exception("不可预料的事情发生了!");
            }
            return pindex;
        }


        internal void Increment(Color32* pixel)
        {
            Red += pixel->Red;
            Green += pixel->Green;
            Blue += pixel->Blue;
            PixelCount++;
        }
    }
}
