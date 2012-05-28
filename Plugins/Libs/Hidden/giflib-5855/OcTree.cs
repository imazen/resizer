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
    #region 八叉树OcTree
    /// <summary>
    /// 八叉树
    /// </summary>
    internal unsafe class OcTree
   {
       #region 私有字段
       OcTreeNode _rootNode;
       int _prefixColor;
       OcTreeNode _prefixNode;
       int _colorDepth = 8;
       int _maxColors = 0;
       int leaves = 0;
       internal OcTreeNode[] ReducibleNodes;
       #endregion

       internal OcTree(int colorDepth)
       {
           this._colorDepth = colorDepth;
           _maxColors = 1<<_colorDepth;
           ReducibleNodes = new OcTreeNode[colorDepth];
           _rootNode = new OcTreeNode(colorDepth, 0, this);         
       }

       internal void TracePrevious(OcTreeNode node)
       {
           this._prefixNode = node;
       }

       internal void IncrementLeaves()
       {
           leaves++;
       }

       internal Color32[] Pallette()
       {
           while(leaves>_maxColors)
           {
               Reduce();
           }
           List<Color32> palltte = new List<Color32>();
           _rootNode.GetPalltte(palltte);
           return palltte.ToArray();
       }

       void Reduce()
       {
           int index;
           for( index = _colorDepth-1;(index>0&&ReducibleNodes[index]==null);index--)
           {

           }
           OcTreeNode node = ReducibleNodes[index];
           ReducibleNodes[index] = node.NextReducible;
           leaves -= node.Reduce();
           _prefixNode = null;
       }

       internal int GetPaletteIndex(Color32* pixel)
       {
           return _rootNode.GetPaletteIndex(pixel, 0);
       }

       internal void AddColor(Color32* pixel)
       {
           //如果当前处理颜色与前一个颜色相同
           if (_prefixColor == pixel->ARGB)
           {
               if (_prefixNode == null)
               {
                   //如果是第一次处理
                   _prefixColor = pixel->ARGB;
                   _rootNode.AddColor(pixel, 0, this);
                   return;
               }          
               //如果颜色与前一个相同，且不是第一次处理，可以复制拷贝上一次的处理结果
               _prefixNode.Increment(pixel);
               return;
           }
           //如果与上一个颜色不同,则需要添加颜色像素到八叉树
           _prefixColor = pixel->ARGB;
           _rootNode.AddColor(pixel, 0, this);
           return;
       }
   }
    #endregion
}
