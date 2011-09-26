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
using System.Collections.Generic;
using System.Text;

namespace Endogine.Codecs.Photoshop
{
    public class Tools
    {
        public static void SplitImage(Document psd, int layerIndex, string outputFilePrefix)
        {
            ImageResources.GridGuidesInfo guidesInfo = (ImageResources.GridGuidesInfo)psd.GetResource(typeof(ImageResources.GridGuidesInfo));

            List<int> vertical = new List<int>();
            List<ImageResources.GridGuidesInfo.GridGuide> guides = guidesInfo.GetGuidesByAlignment(false);
            foreach (ImageResources.GridGuidesInfo.GridGuide gg in guides)
                vertical.Add((int)gg.LocationInPixels);
            vertical.Add((int)psd.Header.Rows);

            List<int> horizontal = new List<int>();
            guides = guidesInfo.GetGuidesByAlignment(true);
            foreach (ImageResources.GridGuidesInfo.GridGuide gg in guides)
                horizontal.Add((int)gg.LocationInPixels);
            horizontal.Add((int)psd.Header.Columns);

            Bitmap bmp = psd.Layers[layerIndex].Bitmap;
            int lastX = 0;
            int cnt = 0;
            foreach (int x in horizontal)
            {
                int lastY = 0;
                foreach (int y in vertical)
                {
                    ERectangle rct = ERectangle.FromLTRB(lastX, lastY, x, y);
                    Bitmap bmpNew = new Bitmap(rct.Width, rct.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpNew);
                    g.DrawImage(bmp, new Rectangle(0, 0, rct.Width, rct.Height), rct.ToRectangle(), GraphicsUnit.Pixel);
                    g.Dispose();

                    //TODO: examine bitmap and see if it can be reduced (e.g. middle parts are probably the same)

                    Endogine.BitmapHelpers.BitmapHelper.Save(bmpNew, outputFilePrefix + "_slice" + cnt + ".png");
                    cnt++;
                    lastY = y;
                }
                lastX = x;
            }
        }
    }
}
