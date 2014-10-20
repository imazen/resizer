using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using System.Drawing.Imaging;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using ImageResizer.Plugins.Wic.InteropServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ImageResizer.Plugins.Wic;
using System.Runtime.InteropServices.ComTypes;

namespace ImageResizer.Plugins.WicEncoder {
    public class WicEncoderPlugin : DefaultEncoder, IPlugin, IEncoder {


        public Guid GetOutputFormatWicGuid() {
            Guid guidEncoder = Consts.GUID_ContainerFormatJpeg;
            if (MimeType.Equals("image/jpeg")) guidEncoder = Consts.GUID_ContainerFormatJpeg;
            if (MimeType.Equals("image/png")) guidEncoder = Consts.GUID_ContainerFormatPng;
            if (MimeType.Equals("image/gif")) guidEncoder = Consts.GUID_ContainerFormatGif;
            return guidEncoder;
        }
        /// <summary>
        /// The Jpeg subsampling. Supported values for WIC are 420, 422, and 444. FreeImage supports 411 in addition to the aforementioned values.
        /// </summary>
        public string Subsampling { get; set; }


        /// <summary>
        /// The number of colors to use. Only applicable for png, gif, and bmp photos where palletes can be used.
        /// The default is -1, which means "as much color fidelity as possible". 
        /// </summary>
        public int Colors { get; set; }

        /// <summary>
        /// If true, error-diffusion dithering will be applied to 8-bit output images
        /// </summary>
        public bool Dither { get; set; }

        /// <summary>
        /// If true, PNGs will be encoded in interlaced form.
        /// </summary>
        public bool? Interlace { get; set; }

        public WicEncoderPlugin() {
            this.Dither = true;
            this.Colors = -1;
        }
        public WicEncoderPlugin(ResizeSettings settings, object original):base(settings,original) {
            Dither = true;
            Subsampling = settings["subsampling"];

            this.Colors = -1;
            //Parse colors
            int colors = -1;
            if (!string.IsNullOrEmpty(settings["colors"]))
                if (int.TryParse(settings["colors"], out colors))
                    this.Colors = colors;

            if ("false".Equals(settings["dither"], StringComparison.OrdinalIgnoreCase) ||
                "0".Equals(settings["dither"], StringComparison.OrdinalIgnoreCase)) Dither = false;

            Interlace = settings.Get<bool>("interlace");
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
      

        public void Write(System.Drawing.Image i, System.IO.Stream s) {
            //A list of COM objects to destroy
            List<object> com = new List<object>();
            try {
                var factory = (IWICComponentFactory)new WICImagingFactory();
                com.Add(factory);


                Stopwatch conversion = new Stopwatch();
                conversion.Start();

                IWICBitmap b = ConversionUtils.ToWic(factory,i as Bitmap);

                conversion.Stop();

                Stopwatch encoding = new Stopwatch();
                encoding.Start();

                //Prepare output stream
                var outputStream = new MemoryIStream();

                EncodeToStream(factory, b, i.Size, outputStream);
                encoding.Stop();

                Stopwatch streaming = new Stopwatch();
                streaming.Start();
                outputStream.WriteTo(s);
                streaming.Stop();
            } finally {
                //Manually cleanup all the com reference counts, aggressively
                while (com.Count > 0) {
                    Marshal.ReleaseComObject(com[com.Count - 1]); //In reverse order, so no item is ever deleted out from under another.
                    com.RemoveAt(com.Count - 1);
                }
            }
        }


        public void EncodeToStream(IWICComponentFactory factory, IWICBitmapSource data, Size imageSize, IStream outputStream) {
            //A list of COM objects to destroy
            List<object> com = new List<object>();
            try {
                
                //Find the GUID of the destination format
                Guid guidEncoder = GetOutputFormatWicGuid();

                //Find out the data's pixel format
                Guid pFormat = Guid.Empty;
                data.GetPixelFormat(out pFormat);

                //Create the encoder
                var encoder = factory.CreateEncoder(guidEncoder, null);
                com.Add(encoder);
                //And initialize it
                encoder.Initialize(outputStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

                // Create the output frame and property bag
                IWICBitmapFrameEncode outputFrame;
                var propertyBagArray = new IPropertyBag2[1];
                encoder.CreateNewFrame(out outputFrame, propertyBagArray); //An array is used instead of an out parameter... I have no idea why
                com.Add(outputFrame);
                //The property bag is a COM object...
                var propBag = propertyBagArray[0];
                com.Add(propBag);

                //Adjust encoder settings if it's a jpegs
                if (guidEncoder.Equals(Consts.GUID_ContainerFormatJpeg)) {
                    //Jpeg
                    //ImageQuality 0..1
                    //"JpegYCrCbSubsampling"  WICJpegYCrCbSubsamplingOption.

                    //Configure encoder settings (see http://msdn.microsoft.com/en-us/library/windows/desktop/ee719871(v=vs.85).aspx#encoderoptions)
                    var qualityOption = new PROPBAG2[1];
                    qualityOption[0].pstrName = "ImageQuality";
                    
                    propBag.Write(1, qualityOption, new object[] { ((float)Math.Max(0,Math.Min(100,Quality))) / 100 });

                    WICJpegYCrCbSubsamplingOption subsampling = WICJpegYCrCbSubsamplingOption.WICJpegYCrCbSubsamplingDefault;
                    //411 NOT SUPPPORTED BY WIC - only by freeimage
                    if ("420".Equals(Subsampling)) subsampling = WICJpegYCrCbSubsamplingOption.WICJpegYCrCbSubsampling420;
                    if ("422".Equals(Subsampling)) subsampling = WICJpegYCrCbSubsamplingOption.WICJpegYCrCbSubsampling422;
                    if ("444".Equals(Subsampling)) subsampling = WICJpegYCrCbSubsamplingOption.WICJpegYCrCbSubsampling444;

                    if (subsampling != WICJpegYCrCbSubsamplingOption.WICJpegYCrCbSubsamplingDefault) {
                        var samplingOption = new PROPBAG2[1];
                        samplingOption[0].pstrName = "JpegYCrCbSubsampling";
                        samplingOption[0].vt = VarEnum.VT_UI1;
                        propBag.Write(1, samplingOption, new object[] { (byte)subsampling });
                    }
                }
                //PNG interlace
                if (guidEncoder.Equals(Consts.GUID_ContainerFormatPng)) {
                    var interlaceOption = new PROPBAG2[1];
                    interlaceOption[0].pstrName = "InterlaceOption";

                    propBag.Write(1, interlaceOption, new object[] { Interlace ?? false });

                }

                //Apply the property bag
                outputFrame.Initialize(propBag);


                //Convert the bitmap to the correct pixel format for encoding.

                //Jpeg: encodes as GUID_WICPixelFormat24bppBGR or GUID_WICPixelFormat8bppGray.
                //If the original pixel format has an alpha chanel, we need to specify a matte color.
                //UPDATE - IWICFormatConverter doesn't let you specify a matte color. Disabling code
                if (false && guidEncoder.Equals(Consts.GUID_ContainerFormatJpeg)) {
                    ConversionUtils.HasAlphaAbility(pFormat);
                    var conv = factory.CreateFormatConverter();
                    com.Add(conv);
                    if (conv.CanConvert(pFormat, Consts.GUID_WICPixelFormat24bppBGR)) {
                        /*dither, pIPalette, alphaThresholdPercent, and paletteTranslate are used to mitigate color loss when 
                         * converting to a reduced bit-depth format. For conversions that do not need these settings, the 
                         * following parameters values should be used: dither set to WICBitmapDitherTypeNone, pIPalette set to NULL, 
                         * alphaThresholdPercent set to 0.0f, and paletteTranslate set to WICBitmapPaletteTypeCustom.*/
                        conv.Initialize(data, Consts.GUID_WICPixelFormat24bppBGR, WICBitmapDitherType.WICBitmapDitherTypeNone, null, 0.0f, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
                        data = conv;
                        //Oops, we didn't do anything - there's no way to specify a matte color!
                    }
                }

                //GIF encodes as GUID_WICPixelFormat8bppIndexed
                //If the current format is > 8bpp, quantization may be required, and we may need to manually build the palette with Median Cut.

                //PNG encodeds as EVERYTHING! Way too many formats supported.
                // If the user is specifying a colors setting, we need to
                // convert to GUID_WICPixelFormat8bppIndexed, GUID_WICPixelFormat4bppIndexed, GUID_WICPixelFormat2bppIndexed, or GUID_WICPixelFormat1bppIndexed

                if ((guidEncoder.Equals(Consts.GUID_ContainerFormatPng) && this.Colors != -1) || (guidEncoder.Equals(Consts.GUID_ContainerFormatGif))) {

                    Guid target = Consts.GUID_WICPixelFormat8bppIndexed;

                    int colors = this.Colors;

                    if (colors > 0 && guidEncoder.Equals(Consts.GUID_ContainerFormatPng)) {
                        if (colors <= 2) target = Consts.GUID_WICPixelFormat1bppIndexed;
                        if (colors <= 4) target = Consts.GUID_WICPixelFormat2bppIndexed;
                        if (colors <= 32) target = Consts.GUID_WICPixelFormat4bppIndexed;
                    }
                    if (colors < 0) colors = 256;
                    if (colors < 2) colors = 2;
                    if (colors > 256) colors = 256;

                    var conv = factory.CreateFormatConverter();
                    com.Add(conv);
                    if (conv.CanConvert(pFormat, target)) {

                        var palette = factory.CreatePalette();
                        com.Add(palette);
                        palette.InitializeFromBitmap(data, (uint)colors, true);

                        /*dither, pIPalette, alphaThresholdPercent, and paletteTranslate are used to mitigate color loss when 
                         * converting to a reduced bit-depth format. For conversions that do not need these settings, the 
                         * following parameters values should be used: dither set to WICBitmapDitherTypeNone, pIPalette set to NULL, 
                         * alphaThresholdPercent set to 0.0f, and paletteTranslate set to WICBitmapPaletteTypeCustom.*/
                        conv.Initialize(data, target, this.Dither ? WICBitmapDitherType.WICBitmapDitherTypeErrorDiffusion :  WICBitmapDitherType.WICBitmapDitherTypeNone,
                                        palette, 50.0f, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
                        data = conv;

                    }
                }

                //Get size
                uint fw, fh;
                data.GetSize(out fw, out fh);

                //Set destination frame size
                outputFrame.SetSize(fw,fh);

                // Write the data to the output frame
                outputFrame.WriteSource(data, null);
                outputFrame.Commit();
                encoder.Commit();


            } finally {
                //Manually cleanup all the com reference counts, aggressively
                while (com.Count > 0) {
                    Marshal.ReleaseComObject(com[com.Count - 1]); //In reverse order, so no item is ever deleted out from under another.
                    com.RemoveAt(com.Count - 1);
                }
            }
        }

    }
}
