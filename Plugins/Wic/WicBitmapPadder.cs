using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using System.Runtime;
using System.Runtime.InteropServices;

namespace ImageResizer.Plugins.Wic {
    public class WicBitmapPadder: IWICBitmapSource {

        IWICBitmapSource s;
        int top = 0;
        int left = 0;
        int bottom = 0;
        int right = 0;
        byte[] bgcolor = null;
        WICRect crop = null;
        
        /// <summary>
        /// Adds padding to a bitmap, optionally cropping it at the same time.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="bgcolor"></param>
        /// <param name="crop"></param>
        public WicBitmapPadder(IWICBitmapSource s, int left, int top, int right, int bottom, byte[] bgcolor, WICRect crop) {
            this.s = s;
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
            this.bgcolor = bgcolor;
            this.crop = crop;

            Guid pf;
            s.GetPixelFormat(out pf);
            if (bgcolor.Length != ConversionUtils.BytesPerPixel(pf)) throw new ArgumentException("bgcolor length must match the format bytes per pixel");
        }

        public void GetSize(out uint puiWidth, out uint puiHeight) {

            uint w, h;
            if (crop == null)
                s.GetSize(out w, out h);
            else {
                w = (uint)crop.Width;
                h = (uint)crop.Height;
            }

            puiWidth = (uint)(w + left + right);
            puiHeight = (uint)(h + top + bottom);
        }

        public void GetPixelFormat(out Guid pPixelFormat) {
            Guid pf;
            s.GetPixelFormat(out pf);
            pPixelFormat = pf;
        }

        public void GetResolution(out double pDpiX, out double pDpiY) {
            double x, y;
            s.GetResolution(out x, out y);
            pDpiX = x;
            pDpiY = y;
        }

        public void CopyPalette(IWICPalette pIPalette) {
            s.CopyPalette(pIPalette);
        }

        //Caching prebuilt arrays of the background color dramatically increases performance manyfold.
        byte[] cachedLeftPadding = null;
        byte[] cachedRightPadding = null;
        byte[] cachedRowPadding = null;

        public void CopyPixels(WICRect prc, uint destStride, uint destBufferSize, [Out]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] destBuffer) {
            //Get normal size of source rect
            uint sw, sh;
            s.GetSize(out sw, out sh);

            //Get normal (uncropped size) of this bitmap. Includes source cropping 
            uint fullWidth, fullHeight;
            this.GetSize(out fullWidth, out fullHeight);
            
            //Copy or generate source crop rectangle. 
            WICRect crop = this.crop == null ? new WICRect{X=0,Y=0,Width=(int)sw,Height=(int)sh}: new WICRect{X=this.crop.X,Y = this.crop.Y, Width = this.crop.Width,Height = this.crop.Height};

            //Copy padding values so we can modify them based on the dest crop area.
            int left = this.left;
            int top = this.top;
            int right = this.right;
            int bottom = this.bottom;


            int w = prc != null ? prc.Width : (int)fullWidth;
            int h = prc != null ? prc.Height : (int)fullHeight;

            //Adjust padding based on what they're asking to crop off
            if (prc != null) {
                left -= prc.X;
                top -= prc.Y;
                right -= (int)(fullWidth - prc.X - prc.Width);
                bottom -= (int)(fullHeight - prc.Y - prc.Height);
            }

            //Now, adjust negative padding to crop from the source bitmap. We should end up with only positive padding.
            if (left < 0) { crop.X += left * -1; crop.Width += left; left = 0; }
            if (top < 0) { crop.Y += top * -1; crop.Height += top;  top = 0; }
            if (right < 0) { crop.Width += right; right = 0; }
            if (bottom < 0) { crop.Height += bottom; bottom = 0; }

            //Now, if we don't have to add any padding (say the caller ended up cropping it all off), just pass the request on.
            if (left == 0 && top == 0 && right == 0 && bottom == 0) {
                s.CopyPixels(crop, destStride, destBufferSize, destBuffer);
                return;
            }
            //Ah, looks like we still have to do padding.
            
            
            //Get pixel format
            Guid format;
            s.GetPixelFormat(out format);

            //Calculate buffer and stride for source data
            uint bpp = (uint)ConversionUtils.BytesPerPixel(format); //Round up to nearest byte.
            uint srcStride = (((bpp*(uint)Math.Max(0,crop.Width)) + 4 - 1) / 4) * 4; //Round up to nearest 4-byte alignment
            uint sbufferSize = srcStride * (uint)Math.Max(0,crop.Height);

            //Allocate for source data
            byte[] sbuffer = new byte[sbufferSize];
         
            //Decode and store the source data
            if (sbufferSize > 0) s.CopyPixels(crop, srcStride, sbufferSize, sbuffer);


            //Ok, now it's time to start work.
            //Manually build a padding stride for left, top/bottom, and right, then use Array.Copy to do it more efficienctly. 

            //Favor doing complete row - don't let left/right overlap with top/bottom.
            if (left > 0 && h > (top + bottom)) {
                //Cache manually built array for speed
                if (cachedLeftPadding == null) {
                    cachedLeftPadding = new byte[left * bpp];
                    for (int i = 0; i < left; i++)
                        Array.Copy(bgcolor, 0, cachedLeftPadding, i * bpp, bpp);
                }
                //Then copy it down
                for (int j = 0; j < h - top - bottom; j++)
                    Array.Copy(cachedLeftPadding, 0, destBuffer, (destStride * (j + top)), left * bpp);
            }
            if (right > 0 && h > (top + bottom)) {
                //Cache manually built array for speed
                if (cachedRightPadding == null) {
                    cachedRightPadding = new byte[right * bpp];
                    for (int i = 0; i < right; i++)
                        Array.Copy(bgcolor, 0, cachedRightPadding, i * bpp, bpp);
                }
                //Then copy it down
                for (int j = 0; j < h - top - bottom; j++)
                    Array.Copy(cachedRightPadding, 0, destBuffer, (destStride * (j + top)) + (left + crop.Width) * bpp, right * bpp);
            }

            if (top > 0 || bottom > 0) {
                //Cache manually built array for speed
                if (cachedRowPadding == null) {
                    cachedRowPadding = new byte[(crop.Width + left + right) * bpp];
                    for (int i = 0; i < crop.Width + left + right; i++)
                        Array.Copy(bgcolor, 0, cachedRowPadding, i * bpp, bpp);
                }

                //Copy it down for both top and bottom
                if (top > 0) {
                    for (int j = 0; j < Math.Min(h,top); j++)
                        Array.Copy(cachedRowPadding, 0, destBuffer, (destStride * j), (crop.Width + left + right) * bpp);
                }
                if (bottom > 0) {
                    for (int j = 0; j < Math.Min(h,bottom); j++) //If the manual row was at the bottom, don't copy it again.
                        Array.Copy(cachedRowPadding, 0, destBuffer, (destStride * (j + top + Math.Max(0, crop.Height))), (crop.Width + left + right) * bpp);
                }

            }

            //Now, copy the image data. If we didn't use left or right padding we can copy the whole thing at once.
            if (sbufferSize > 0) {
                if (srcStride == destStride) {
                    Array.Copy(sbuffer, 0, destBuffer, top * destStride, sbuffer.Length);
                } else {
                    //Otherwise, one row at a time
                    for (int j = 0; j < crop.Height; j++)
                        Array.Copy(sbuffer, j * srcStride, destBuffer, (left * bpp) + (top + j) * destStride, crop.Width * bpp);
                }
            }


            //We're done!
        }
    }
}
