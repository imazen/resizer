using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using FreeImageAPI;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using System.Drawing.Imaging;
using System.Globalization;

namespace ImageResizer.Plugins.FreeImageEncoder {
    /// <summary>
    /// FreeImageEncoder can encode jpegs 2-3x as fast as GDI can, and offers more encoding options.
    /// </summary>
    public class FreeImageEncoderPlugin : IPlugin, IEncoder {

        FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_JPEG;


        public FREE_IMAGE_FORMAT Format {
            get { return format; }
            set { format = value; }
        }
        FREE_IMAGE_SAVE_FLAGS encodingOptions = FREE_IMAGE_SAVE_FLAGS.DEFAULT;

        public FREE_IMAGE_SAVE_FLAGS EncodingOptions {
            get { return encodingOptions; }
            set { encodingOptions = value; }
        }
        /// <summary>
        /// Adds a new instance of FreeImageEncoderPlugin
        /// </summary>
        public FreeImageEncoderPlugin() {
        }

        public FreeImageEncoderPlugin(ResizeSettings settings, object original) {
            ImageFormat originalFormat = DefaultEncoder.GetOriginalFormat(original);
            if (!IsValidOutputFormat(originalFormat)) originalFormat = ImageFormat.Jpeg;//No valid info available about the original format. Use Jpeg.

            //What format was specified?
            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, originalFormat); //fallback to originalFormat if not specified.
            if (!IsValidOutputFormat(requestedFormat))
                throw new ArgumentException("An unrecognized or unsupported output format (" + (settings.Format != null ? settings.Format : "(null)") + ") was specified in 'settings'.");
            this.format =  FreeImage.GetFormat(requestedFormat);

            //Parse JPEG settings.
            int quality = 90;
            if (string.IsNullOrEmpty(settings["quality"]) || !int.TryParse(settings["quality"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out quality)) quality = 90;
            if (format == FREE_IMAGE_FORMAT.FIF_JPEG) {
                if (quality >= 100) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYSUPERB;
                else if (quality >= 75) 
                    encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD;
                else if (quality >= 50) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYNORMAL;
                else if (quality >= 25) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYAVERAGE;
                else encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYBAD;


                if ("true".Equals(settings["progressive"])) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_PROGRESSIVE;

                if ("411".Equals(settings["subsampling"])) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_411;
                if ("420".Equals(settings["subsampling"])) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_420;
                if ("422".Equals(settings["subsampling"])) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_422;
                if ("444".Equals(settings["subsampling"])) encodingOptions |= FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_444;
            }
            if (string.IsNullOrEmpty(settings["colors"]) || !int.TryParse(settings["colors"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out colors)) colors = -1;


            if (format == FREE_IMAGE_FORMAT.FIF_GIF) {
                //encodingOptions = FREE_IMAGE_SAVE_FLAGS.
            }
            

        }
        int colors = -1;

        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEncoder CreateIfSuitable(ResizeSettings settings, object original) {
            
            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, ImageFormat.Jpeg);
            if (requestedFormat == null || !IsValidOutputFormat(requestedFormat)) return null; //An unsupported format was explicitly specified.
            if (!"freeimage".Equals(settings["encoder"], StringComparison.OrdinalIgnoreCase)) return null;
            if (!FreeImage.IsAvailable()) return null;
            return new FreeImageEncoderPlugin(settings, original);
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
            if (!(i is Bitmap)) throw new ArgumentException("FreeImageEncoder only works with bitmaps");
            FIBITMAP bit = FreeImage.CreateFromBitmap(i as Bitmap);
            if (bit.IsNull) throw new ImageProcessingException("FreeImageEncoder failed to convert System.Drawing.Bitmap to FIBITMAP");
            if (Format == FREE_IMAGE_FORMAT.FIF_GIF || ( Format == FREE_IMAGE_FORMAT.FIF_PNG && colors != -1)){
                FreeImage.SetTransparent(bit, true);
                FIBITMAP old = bit;
                //TODO - ColorQuantizeEx returns null no matter what we do.. Is it because the image is 32-bit?
                bit = FreeImage.ColorQuantizeEx(bit, FREE_IMAGE_QUANTIZE.FIQ_NNQUANT, 256,1, RGBQUAD.ToRGBQUAD(new Color[] { Color.Transparent }));
                if (bit.IsNull) bit = old;
                else if (bit != old) FreeImage.UnloadEx(ref old);
            }
            FreeImage.SaveToStream(ref bit, s, Format, EncodingOptions, true);
        }
        /// <summary>
        /// If the configured encoding settings support transparency.
        /// </summary>
        public bool SupportsTransparency {
            get { return Format != FREE_IMAGE_FORMAT.FIF_JPEG; }
        }
        /// <summary>
        /// The suggested mime-type for the output image produced by this encoder
        /// </summary>
        public string MimeType {
            get { return FreeImage.GetFIFMimeType(Format); }
        }
        /// <summary>
        /// The suggested extension for the output image produced by this encoder
        /// </summary>
        public string Extension {
            get { return FreeImage.GetPrimaryExtensionFromFIF(Format); }
        }
    }
}
