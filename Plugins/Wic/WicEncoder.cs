using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using System.Drawing.Imaging;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using WicResize.InteropServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ImageResizer.Plugins.Wic;

namespace ImageResizer.Plugins.WicEncoder {
    public class WicEncoderPlugin : DefaultEncoder, IPlugin, IEncoder {


   
        public WicEncoderPlugin(ResizeSettings settings, object original):base(settings,original) {

        }

        public WicEncoderPlugin() {
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEncoder CreateIfSuitable(ResizeSettings settings, object original) {

            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, ImageFormat.Jpeg);
            if (requestedFormat == null || !IsValidOutputFormat(requestedFormat)) return null; //An unsupported format was explicitly specified.
            if (!"wic".Equals(settings["encoder"], StringComparison.OrdinalIgnoreCase)) return null;
            return new WicEncoderPlugin(settings, original);
        }

        /// <summary>
        /// Returns true if the this encoder supports the specified image format
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private static bool IsValidOutputFormat(ImageFormat f) {
            return (ImageFormat.Gif.Equals(f) || ImageFormat.Png.Equals(f) || ImageFormat.Jpeg.Equals(f));
        }
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        

        public void Write(System.Drawing.Image i, System.IO.Stream s) {
            Stopwatch conversion = new Stopwatch();
            
            var factory = (IWICComponentFactory)new WICImagingFactory();

            conversion.Start();

            Bitmap bit = i as Bitmap;
            IWICBitmap b = null;
            BitmapData bd =  bit.LockBits(new Rectangle(0,0,bit.Width,bit.Height), ImageLockMode.ReadOnly, bit.PixelFormat);
            int size = bd.Stride * bd.Height;

            byte[] data = new byte[size];
            Marshal.Copy(bd.Scan0, data, 0, size);
            b = factory.CreateBitmapFromMemory((uint)bit.Width, (uint)bit.Height, ConversionUtils.FromPixelFormat(bit.PixelFormat), (uint)bd.Stride, (uint)size, data);
            
            
            bit.UnlockBits(bd);
            //IntPtr hbit = ((Bitmap)i).GetHbitmap();
            //b = factory.CreateBitmapFromHBITMAP(hbit, IntPtr.Zero, WICBitmapAlphaChannelOption.WICBitmapUseAlpha);
            conversion.Stop();

            Stopwatch encoding = new Stopwatch();
            encoding.Start();

            Guid guidEncoder = Consts.GUID_ContainerFormatJpeg;
            if (MimeType.Equals("image/jpeg")) guidEncoder = Consts.GUID_ContainerFormatJpeg;
            if (MimeType.Equals("image/png")) guidEncoder = Consts.GUID_ContainerFormatPng;
            if (MimeType.Equals("image/gif")) guidEncoder = Consts.GUID_ContainerFormatGif;
            var encoder = factory.CreateEncoder(guidEncoder, null);

            int quality = this.Quality;
            //Validate quality
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;

            //Configure it, prepare output stream
            var outputStream = new MemoryIStream();
            encoder.Initialize(outputStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
            // Prepare output frame
            IWICBitmapFrameEncode outputFrame;
            var arg = new IPropertyBag2[1];
            encoder.CreateNewFrame(out outputFrame, arg);
            var propBag = arg[0];
            var propertyBagOption = new PROPBAG2[1];
            propertyBagOption[0].pstrName = "ImageQuality";
            propBag.Write(1, propertyBagOption, new object[] { ((float)quality) / 100 });
            outputFrame.Initialize(propBag);

            outputFrame.WriteSource(b, new WICRect { X = 0, Y = 0, Width = (int)i.Width, Height = (int)i.Height });
            outputFrame.Commit();
            encoder.Commit();
            encoding.Stop();

            Stopwatch disposal = new Stopwatch();
            disposal.Start();
            Marshal.FinalReleaseComObject(outputFrame);
            //DeleteObject(hbit);
            Marshal.FinalReleaseComObject(encoder);
            Marshal.FinalReleaseComObject(factory);
            disposal.Stop();

            Stopwatch streaming = new Stopwatch();
            streaming.Start();
            outputStream.WriteTo(s);
            streaming.Stop();
        }

    }
}
