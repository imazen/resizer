using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing.Imaging;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace ImageResizer.Plugins.Wic {
    public class ConversionUtils {

        private static HashSet<Guid> alphaFormats = new HashSet<Guid>(new Guid[]{
            Consts.GUID_WICPixelFormat112bpp6ChannelsAlpha, Consts.GUID_WICPixelFormat128bpp7ChannelsAlpha,
            Consts.GUID_WICPixelFormat128bppPRGBAFloat, Consts.GUID_WICPixelFormat128bppRGBAFixedPoint,
            Consts.GUID_WICPixelFormat128bppRGBAFloat, Consts.GUID_WICPixelFormat144bpp8ChannelsAlpha,
            Consts.GUID_WICPixelFormat32bpp3ChannelsAlpha, Consts.GUID_WICPixelFormat40bpp4ChannelsAlpha,
            Consts.GUID_WICPixelFormat56bpp6ChannelsAlpha, Consts.GUID_WICPixelFormat64bpp3ChannelsAlpha,
            Consts.GUID_WICPixelFormat16bppBGRA5551,  Consts.GUID_WICPixelFormat32bppBGRA, 
            Consts.GUID_WICPixelFormat32bppPBGRA, Consts.GUID_WICPixelFormat32bppPRGBA, 
            Consts.GUID_WICPixelFormat32bppRGBA, Consts.GUID_WICPixelFormat32bppRGBA1010102, 
            Consts.GUID_WICPixelFormat32bppRGBA1010102XR, Consts.GUID_WICPixelFormat40bpp4ChannelsAlpha,
            Consts.GUID_WICPixelFormat40bppCMYKAlpha, Consts.GUID_WICPixelFormat48bpp5ChannelsAlpha,
            Consts.GUID_WICPixelFormat56bpp6ChannelsAlpha, Consts.GUID_WICPixelFormat64bpp3ChannelsAlpha,
            Consts.GUID_WICPixelFormat64bpp7ChannelsAlpha, Consts.GUID_WICPixelFormat64bppBGRA,
            Consts.GUID_WICPixelFormat64bppBGRAFixedPoint, Consts.GUID_WICPixelFormat64bppPBGRA,
            Consts.GUID_WICPixelFormat64bppPRGBA, Consts.GUID_WICPixelFormat64bppRGBA,
            Consts.GUID_WICPixelFormat64bppRGBAFixedPoint, Consts.GUID_WICPixelFormat64bppRGBAHalf,
            Consts.GUID_WICPixelFormat72bpp8ChannelsAlpha, Consts.GUID_WICPixelFormat80bpp4ChannelsAlpha,
            Consts.GUID_WICPixelFormat80bppCMYKAlpha, Consts.GUID_WICPixelFormat8bppAlpha,
            Consts.GUID_WICPixelFormat96bpp5ChannelsAlpha, Consts.GUID_WICPixelFormat8bppIndexed,
            Consts.GUID_WICPixelFormat4bppIndexed, Consts.GUID_WICPixelFormat1bppIndexed,
            Consts.GUID_WICPixelFormat2bppIndexed

        });
        public static bool HasAlphaAbility(Guid format) {
            return alphaFormats.Contains(format);

        }

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
                    if (int.TryParse(bpp, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out ibpp)) d[key] = ibpp;
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
            if (wicFormat == Consts.GUID_WICPixelFormat4bppIndexed) return PixelFormat.Format4bppIndexed;
            if (wicFormat == Consts.GUID_WICPixelFormat48bppRGB) return PixelFormat.Format48bppRgb;
            if (wicFormat == Consts.GUID_WICPixelFormat1bppIndexed) return PixelFormat.Format1bppIndexed;
            if (wicFormat == Consts.GUID_WICPixelFormat64bppBGRA) return PixelFormat.Format64bppArgb;
            return PixelFormat.Undefined;
        }
        public static Guid FromPixelFormat(PixelFormat f) {
            if (f == PixelFormat.Alpha) return Consts.GUID_WICPixelFormat8bppAlpha;
            if (f == PixelFormat.Canonical) return Consts.GUID_WICPixelFormat32bppBGRA;
            if (f == PixelFormat.Format16bppArgb1555) return Consts.GUID_WICPixelFormat16bppBGRA5551;
            if (f == PixelFormat.Format16bppGrayScale) return Consts.GUID_WICPixelFormat16bppGray;
            if (f == PixelFormat.Format16bppRgb555) return Consts.GUID_WICPixelFormat16bppBGR555;
            if (f == PixelFormat.Format16bppRgb565) return Consts.GUID_WICPixelFormat16bppBGR565;
            if (f == PixelFormat.Format1bppIndexed) return Consts.GUID_WICPixelFormat1bppIndexed;
            if (f == PixelFormat.Format24bppRgb) return Consts.GUID_WICPixelFormat24bppBGR;
            if (f == PixelFormat.Format32bppRgb) return Consts.GUID_WICPixelFormat32bppBGR;
            if (f == PixelFormat.Format32bppArgb) return Consts.GUID_WICPixelFormat32bppBGRA;
            if (f == PixelFormat.Format32bppPArgb) return Consts.GUID_WICPixelFormat32bppPBGRA;
            if (f == PixelFormat.Format48bppRgb) return Consts.GUID_WICPixelFormat48bppBGR;
            if (f == PixelFormat.Format4bppIndexed) return Consts.GUID_WICPixelFormat4bppIndexed;
            if (f == PixelFormat.Format64bppArgb) return Consts.GUID_WICPixelFormat64bppBGRA;
            if (f == PixelFormat.Format64bppPArgb) return Consts.GUID_WICPixelFormat64bppPBGRA;
            if (f == PixelFormat.Format8bppIndexed) return Consts.GUID_WICPixelFormat8bppIndexed;
            return Guid.Empty;
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

        [DllImport("WindowsCodecs.dll", EntryPoint = "IWICImagingFactory_CreateBitmapFromMemory_Proxy")]
        internal static extern int CreateBitmapFromMemory(IWICComponentFactory factory, uint width, uint height, ref Guid pixelFormatGuid, uint stride, uint cbBufferSize, IntPtr pvPixels, out IWICBitmap ppIBitmap);

        public static IWICBitmap ToWic(IWICComponentFactory factory, Bitmap bit) {
            Guid pixelFormat = ConversionUtils.FromPixelFormat(bit.PixelFormat);
            if (pixelFormat == Guid.Empty) throw new NotSupportedException("PixelFormat " + bit.PixelFormat.ToString() + " not supported.");
            BitmapData bd = bit.LockBits(new Rectangle(0, 0, bit.Width, bit.Height), ImageLockMode.ReadOnly, bit.PixelFormat);
            IWICBitmap b = null;
            IWICPalette p = null;
            
            try {
                    //Create WIC bitmap directly from unmanaged memory
                long result = CreateBitmapFromMemory(factory, (uint)bit.Width, (uint)bit.Height, ref pixelFormat, (uint)bd.Stride, (uint)(bd.Stride * bd.Height), bd.Scan0, out b);
                //b = factory.CreateBitmapFromMemory((uint)bit.Width, (uint)bit.Height, ConversionUtils.FromPixelFormat(bit.PixelFormat), (uint)bd.Stride, (uint)(bd.Stride * bd.Height), bd.Scan0);
                if (result == 0x80070057) throw new ArgumentException();
                if (result < 0) throw new Exception("HRESULT " + result);

                //Copy the bitmap palette if it exists
                var sPalette = bit.Palette;
                if (sPalette.Entries.Length > 0)
                {
                    p = factory.CreatePalette();
                    uint[] colors = new uint[sPalette.Entries.Length];
                    for (int i = 0; i < sPalette.Entries.Length; i++) {
                        colors[i] = (uint)(((sPalette.Entries[i].A << 24) | (sPalette.Entries[i].R << 16) | (sPalette.Entries[i].G << 8) | sPalette.Entries[i].B) & 0xffffffffL);
                    }
                    p.InitializeCustom(colors, (uint)colors.Length);
                    b.SetPalette(p);
                }

                return b;
            } finally {
                bit.UnlockBits(bd);
                if(p != null) Marshal.ReleaseComObject(p);
            }
        }

    [DllImport("WindowsCodecs.dll", EntryPoint = "IWICBitmapSource_CopyPixels_Proxy")]
    internal static extern int CopyPixels(IWICBitmapSource bitmap, IntPtr rect, uint cbStride, uint cbBufferSize, IntPtr pvPixels);

    public static Bitmap FromWic(IWICBitmapSource source) {
            
        Guid format; //Get the WIC pixel format
        source.GetPixelFormat(out format);
        //Get the matching GDI format
        PixelFormat gdiFormat = ConversionUtils.GetPixelFormat(format);

        //If it's not GDI-supported format, convert it to one.
        IWICComponentFactory factory = null;
        IWICFormatConverter converter = null;
        try {
            if (gdiFormat == PixelFormat.Undefined) {
                factory = (IWICComponentFactory)new WICImagingFactory();
                converter = factory.CreateFormatConverter();
                converter.Initialize(source, Consts.GUID_WICPixelFormat32bppBGRA, WICBitmapDitherType.WICBitmapDitherTypeNone, null, 0.9f, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
                gdiFormat = PixelFormat.Format32bppArgb;
            }
            IWICBitmapSource data = converter != null ? converter : source;

            //Get the dimensions of the WIC bitmap
            uint w, h;
            data.GetSize(out w, out h);

            Bitmap b = new Bitmap((int)w, (int)h, gdiFormat);
            BitmapData bd = b.LockBits(new Rectangle(0, 0, (int)w, (int)h), ImageLockMode.WriteOnly, b.PixelFormat);
            try {
                long result = CopyPixels(data, IntPtr.Zero, (uint)bd.Stride, (uint)(bd.Stride * bd.Height), bd.Scan0);
                if (result == 0x80070057) throw new ArgumentException();
                if (result < 0) throw new Exception("HRESULT " + result);
                return b;
            } finally {
                b.UnlockBits(bd);
            }
        } finally {
            if (converter != null) Marshal.ReleaseComObject(converter);
            if (factory != null) Marshal.ReleaseComObject(factory);
        }
    }

        public static byte[] ConvertColor(Color color, Guid pixelFormat) {

            if (pixelFormat == Consts.GUID_WICPixelFormat24bppBGR)
                return new byte[] { color.B, color.G, color.R };
            if (pixelFormat == Consts.GUID_WICPixelFormat24bppRGB)
                return new byte[] { color.R, color.G, color.B };

            if (pixelFormat == Consts.GUID_WICPixelFormat32bppBGR || pixelFormat == Consts.GUID_WICPixelFormat32bppBGRA ||
                pixelFormat == Consts.GUID_WICPixelFormat32bppPBGRA)
                return new byte[] { color.B, color.G, color.R, color.A};

            if (pixelFormat == Consts.GUID_WICPixelFormat32bppPRGBA || pixelFormat == Consts.GUID_WICPixelFormat32bppRGBA)
                return new byte[] { color.R, color.G, color.B, color.A };

            if (pixelFormat == Consts.GUID_WICPixelFormat8bppGray)
                return new byte[] {(byte)Math.Min(0,Math.Max(255, (float)color.B * 0.081f +  (float)color.G * 0.419f + (float)color.R * 0.5f))};


            byte[] data = new byte[ConversionUtils.BytesPerPixel(pixelFormat)];


            return data;
        }
    }
}
