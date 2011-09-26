using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using FreeImageAPI;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.FreeImageEncoder {
    public class FreeImageEncoderPlugin : IPlugin, IEncoder {
        public FreeImageEncoderPlugin(ResizeSettings settings, object original) {
            ImageFormat originalFormat = DefaultEncoder.GetOriginalFormat(original);
            if (!IsValidOutputFormat(originalFormat)) originalFormat = ImageFormat.Jpeg;//No valid info available about the original format. Use Jpeg.

            //What format was specified?
            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, originalFormat); //fallback to originalFormat if not specified.
            if (!IsValidOutputFormat(requestedFormat))
                throw new ArgumentException("An unrecognized or unsupported output format (" + (settings.Format != null ? settings.Format : "(null)") + ") was specified in 'settings'.");
            this.format =  FreeImage.GetFormat(requestedFormat);

            //TODO: parse quality settings.
        }

        public FreeImageEncoderPlugin() {
            // TODO: Complete member initialization
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
            if (!FreeImage.IsAvailable()) return null;
            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, ImageFormat.Jpeg);
            if (requestedFormat == null || !IsValidOutputFormat(requestedFormat)) return null; //An unsupported format was explicitly specified.
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


        FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_JPEG;
        FREE_IMAGE_SAVE_FLAGS encodingOptions = FREE_IMAGE_SAVE_FLAGS.DEFAULT;
   
        public void Write(System.Drawing.Image i, System.IO.Stream s) {
            if (!(i is Bitmap)) throw new ArgumentException("FreeImageEncoder only works with bitmaps");
            FIBITMAP bit = FreeImage.CreateFromBitmap(i as Bitmap);
            if (bit.IsNull) throw new ImageProcessingException("FreeImageEncoder failed to convert System.Drawing.Bitmap to FIBITMAP");

            FreeImage.SaveToStream(ref bit, s, format, encodingOptions, true);
        }

        public bool SupportsTransparency {
            get { return format != FREE_IMAGE_FORMAT.FIF_JPEG; }
        }

        public string MimeType {
            get { return FreeImage.GetFIFMimeType(format); }
        }

        public string Extension {
            get { return FreeImage.GetPrimaryExtensionFromFIF(format); }
        }
    }
}
