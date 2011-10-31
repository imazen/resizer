using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.Wic {
    public class ConversionUtils {


        private static object _lockBppDict = new object();
        private static Dictionary<Guid,int> _bppDict = null;

        public static int BytesPerPixel(Guid format) {
            return (int)Math.Ceiling((float)BitsPerPixel(format) / 8f);
        }
        public static int BitsPerPixel(Guid format){
            if (_bppDict == null) lock(_lockBppDict) if (_bppDict == null){
                FieldInfo[] members = typeof(Consts).GetFields( System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                Dictionary<Guid,int> d = new Dictionary<Guid,int>();
                foreach(FieldInfo fi in members){
                    if (fi.FieldType != typeof(Guid)) continue;
                    Guid key = (Guid)fi.GetValue(null);

                    //Parse the member name - look for 'bpp', then use whatever number is preceeding it.
                    string name = (string)fi.Name;
                    int b = name.IndexOf("bpp", 0, StringComparison.OrdinalIgnoreCase);
                    if (b < 1) continue;
                    string bpp = "";
                    for (int i = b -1; b >= 0; i--) {
                        if (Char.IsDigit(name[i])) bpp = name[i] + bpp;
                        else break;
                    }
                    if (string.IsNullOrEmpty(bpp)) continue;
                    int ibpp;
                    if (int.TryParse(bpp, out ibpp)) d[key] = ibpp;
                }
                _bppDict = d;
            }
            int temp;
            if (!_bppDict.TryGetValue(format, out temp)) throw new Exception("No idea what format this is " + format.ToString()); 
            return temp;
        }
        public static System.Drawing.Imaging.PixelFormat GetPixelFormat(Guid wicFormat) {
            if (wicFormat == Consts.GUID_WICPixelFormat24bppBGR) return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            if (wicFormat == Consts.GUID_WICPixelFormat32bppBGRA) return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            if (wicFormat == Consts.GUID_WICPixelFormat32bppBGR) return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            if (wicFormat == Consts.GUID_WICPixelFormat32bppPBGRA) return System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
            if (wicFormat == Consts.GUID_WICPixelFormat8bppIndexed) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            throw new NotSupportedException();
        }
        public static Guid FromPixelFormat(PixelFormat f) {
            if (f == PixelFormat.Format24bppRgb) return Consts.GUID_WICPixelFormat24bppBGR;
            if (f == PixelFormat.Format32bppRgb) return Consts.GUID_WICPixelFormat32bppBGR;
            if (f == PixelFormat.Format32bppArgb) return Consts.GUID_WICPixelFormat32bppBGRA;
            if (f == PixelFormat.Format32bppPArgb) return Consts.GUID_WICPixelFormat32bppPBGRA;
            if (f == PixelFormat.Format8bppIndexed) return Consts.GUID_WICPixelFormat8bppIndexed;

            throw new NotSupportedException();
        }

        public static uint GetStride(IWICBitmapSource source) {
            uint w, h;
            source.GetSize(out w, out h);
            Guid format;
            source.GetPixelFormat(out format);
            uint dword = 4;
            uint stride = (uint)BytesPerPixel(format) * w; //1bpp, 2bpp, 4 bpp all take one byte? Not sure how to handle those.
            stride = ((stride + dword - 1) / dword) * dword; //Round up to multiple of 4. 
            return stride;
        }

        public static Bitmap FromWic(IWICBitmapSource source) {
            return null;
            //var factory = (IWICComponentFactory)new WICImagingFactory();
            //var converter = factory.CreateFormatConverter();
            //TODO: handle conversion. converter.Initialize(b, GUID_WICPixelFormat24bppBGR, WICBitmapDitherType.

            uint w, h;
            source.GetSize(out w, out h);
            Guid format;
            source.GetPixelFormat(out format);

            uint stride = GetStride(source);

            //Allocate managed memory to decode the image into. 
            uint bufferSize = stride * h;
            byte[] buffer = new byte[bufferSize];
            //Decode and store the data
            source.CopyPixels(new WICRect { X = 0, Y = 0, Width = (int)w, Height = (int)h }, stride, bufferSize, buffer);
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap b = new Bitmap((int)w, (int)h, (int)stride, GetPixelFormat(format), handle.AddrOfPinnedObject());
            b.Tag = handle;
            return b;
        }
    }
}
