/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Collections.Specialized;
using ImageResizer.Plugins;
using ImageResizer.Encoding;
using ImageResizer.Resizing;
using System.Globalization;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Provides basic encoding functionality for Jpeg, png, and gif output. Allows adjustable Jpeg compression, but doesn't implement indexed PNG files or quantized GIF files.
    /// </summary>
    public class DefaultEncoder :IEncoder, IQuerystringPlugin, IPlugin, IFileSignatureProvider {

        public DefaultEncoder() {
        }
        public DefaultEncoder(ImageFormat outputFormat) {
            this.OutputFormat = outputFormat;
        }
        public DefaultEncoder(ImageFormat outputFormat, int jpegQuality) {
            this.OutputFormat = outputFormat;
            this.Quality = jpegQuality;
        }

        public DefaultEncoder(ResizeSettings settings, object original) {
            //What format was the image originally (used as a fallback).
            ImageFormat originalFormat = GetOriginalFormat(original);
            if (!IsValidOutputFormat(originalFormat)) originalFormat = ImageFormat.Jpeg;//No valid info available about the original format. Use Jpeg.

            //What format was specified?
            ImageFormat requestedFormat = GetRequestedFormat(settings.Format, originalFormat); //fallback to originalFormat if not specified.
            if (!IsValidOutputFormat(requestedFormat))
                throw new ArgumentException("An unrecognized or unsupported output format (" + (settings.Format != null ? settings.Format : "(null)") + ") was specified in 'settings'.");

            //Ok, we've found our format.
            this.OutputFormat = requestedFormat;

            //parse quality;
            int quality = 90;
            if (!string.IsNullOrEmpty(settings["quality"]))
                if (int.TryParse(settings["quality"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out quality))
                    this.Quality = quality;

        }
        
        public virtual IEncoder CreateIfSuitable(ResizeSettings settings, object original) {
            ImageFormat requestedFormat = GetRequestedFormat(settings.Format, ImageFormat.Jpeg);
            if (requestedFormat == null || !IsValidOutputFormat(requestedFormat)) return null; //An unsupported format was explicitly specified.
            return new DefaultEncoder(settings, original);
        }

        private ImageFormat _outputFormat = ImageFormat.Jpeg;
        /// <summary>
        /// If you set this to anything other than Gif, Png, or Jpeg, it will throw an exception. Defaults to Jpeg
        /// </summary>
        public ImageFormat OutputFormat {
            get { return _outputFormat; }
            set {
                if (!IsValidOutputFormat(value)) throw new ArgumentException(value.ToString() + " is not a valid OutputFormat for DefaultEncoder.");
                _outputFormat = value;
            }
        }
        /// <summary>
        /// Returns true if the this encoder supports the specified image format
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public bool IsValidOutputFormat(ImageFormat f) {
            return (ImageFormat.Gif.Equals(f) || ImageFormat.Png.Equals(f) || ImageFormat.Jpeg.Equals(f));
        }


        private int quality = 90;
        /// <summary>
        /// 0..100 value. The Jpeg compression quality. 90 is the best setting. Not relevant in Png or Gif compression
        /// </summary>
        public int Quality {
            get { return quality; }
            set { quality = value; }
        }
        
        /// <summary>
        /// Writes the specified image to the stream using Quality and OutputFormat
        /// </summary>
        /// <param name="image"></param>
        /// <param name="s"></param>
        public void Write(Image image, System.IO.Stream s) {
            if (ImageFormat.Jpeg.Equals(OutputFormat)) SaveJpeg(image, s, this.Quality);
            else if (ImageFormat.Png.Equals(OutputFormat)) SavePng(image, s);
            else if (ImageFormat.Gif.Equals(OutputFormat)) SaveGif(image, s);
        }

        /// <summary>
        /// Returns true if the desired output type supports transparency.
        /// </summary>
        public bool SupportsTransparency {
            get {
                return ImageFormat.Png.Equals(OutputFormat) || ImageFormat.Gif.Equals(OutputFormat); //Does Gif transparency work?
            }
        }

        /// <summary>
        /// Returns the default mime-type for the OutputFormat
        /// </summary>
        public string MimeType {
            get { return DefaultEncoder.GetContentTypeFromImageFormat(OutputFormat); }
        }
        /// <summary>
        /// Returns the default file extesnion for OutputFormat
        /// </summary>
        public string Extension {
            get { return DefaultEncoder.GetExtensionFromImageFormat(OutputFormat); }
            
        }


        #region Static methods
        /// <summary>
        /// Tries to parse an ImageFormat from the settings.Format value.
        /// If an unrecogized format is specified, returns null.
        /// If an unsupported format is specified, it is returned.
        /// If *no* format is specified, returns defaultValue.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static ImageFormat GetRequestedFormat(string format, ImageFormat defaultValue) {
            ImageFormat f = null;
            if (!string.IsNullOrEmpty(format)) {
                f = DefaultEncoder.GetImageFormatFromExtension(format);
                return f;
            }
            //Fallback. No encoder was explicitly specified, so let's try to infer it from the image data.
            return defaultValue;

        }
        /// <summary>
        /// Attempts to determine the ImageFormat of the source image. First attempts to parse the path, if a string is present in original.Tag. (or if 'original' is a string)
        /// Falls back to using original.RawFormat. Returns null if both 'original' is null.
        /// RawFormat has a bad reputation, so this may return unexpected values, like MemoryBitmap or something in some situations.
        /// </summary>
        /// <param name="original">The source image that was loaded from a stream, or a string path</param>
        /// <returns></returns>
        public static ImageFormat GetOriginalFormat(object original) {
            if (original == null) return null;
            //Try to parse the original file extension first.
            string path = original as string;
            
            if (path == null && original is Image) path = ((Image)original).Tag as string;

            if (path == null && original is Image && ((Image)original).Tag is BitmapTag) path = ((BitmapTag)((Image)original).Tag).Path;

            //We have a path? Parse it!
            if (path != null) {
                ImageFormat f = DefaultEncoder.GetImageFormatFromPhysicalPath(path);
                if (f != null) return f; //From the path
            }
            //Ok, I guess it there (a) wasn't a path, or (b), it didn't have a recognizeable extension
            if (original is Image) return ((Image)original).RawFormat;
            return null;
        }

        /// <summary>
        /// Returns the ImageFormat enumeration value based on the extension in the specified physical path. Extensions can lie, just a guess.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromPhysicalPath(string path)
        {
            return GetImageFormatFromExtension(System.IO.Path.GetExtension(path));
        }

        /// <summary>
        /// Returns an string instance from the specfied ImageFormat. First matching entry in imageExtensions is used.
        /// Returns null if not recognized.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetExtensionFromImageFormat(ImageFormat format)
        {
            lock (_syncExts) {
                foreach (KeyValuePair<string, ImageFormat> p in imageExtensions) {
                    if (p.Value.Guid.Equals(format.Guid)) return p.Key;
                }
            }
            return null;
        }
        

        private static object _syncExts = new object();
        /// <summary>
        /// Returns a dict of (lowercase invariant) image extensions and ImageFormat values
        /// </summary>
        private static IDictionary<String,ImageFormat> _imageExtensions = null;
        private static IDictionary<String,ImageFormat> imageExtensions{
            get{
                lock (_syncExts) {
                    if (_imageExtensions == null) {
                        _imageExtensions = new Dictionary<String, ImageFormat>();
                        addImageExtension("jpg", ImageFormat.Jpeg);
                        addImageExtension("jpeg", ImageFormat.Jpeg);
                        addImageExtension("jpe", ImageFormat.Jpeg);
                        addImageExtension("jif", ImageFormat.Jpeg);
                        addImageExtension("jfif", ImageFormat.Jpeg);
                        addImageExtension("jfi", ImageFormat.Jpeg);
                        addImageExtension("exif", ImageFormat.Jpeg);
                        addImageExtension("bmp", ImageFormat.Bmp);
                        addImageExtension("gif", ImageFormat.Gif);
                        addImageExtension("png", ImageFormat.Png);
                        addImageExtension("tif", ImageFormat.Tiff);
                        addImageExtension("tiff", ImageFormat.Tiff);
                        addImageExtension("tff", ImageFormat.Tiff);
                        //"bmp","gif","exif","png","tif","tiff","tff","jpg","jpeg", "jpe","jif","jfif","jfi"
                    }
                    return _imageExtensions;
                }
            }
        }

        /// <summary>
        /// Returns an ImageFormat instance from the specfied file extension. Extensions lie sometimes, just a guess.
        /// returns null if not recognized.
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return null;
            lock (_syncExts) {
                ext = ext.Trim(' ', '.').ToLowerInvariant();
                if (!imageExtensions.ContainsKey(ext)) return null;
                return imageExtensions[ext];
            }
        }
        /// <summary>
        /// NOT thread-safe! 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="matchingFormat"></param>
        private static void addImageExtension(string extension, ImageFormat matchingFormat) {
            //In case first call is to this method, use the property. Will be recursive, but that's fine, since it won't be null.
            imageExtensions.Add(extension.TrimStart('.', ' ').ToLowerInvariant(), matchingFormat);
        }

        public static void AddImageExtension(string extension, ImageFormat matchingFormat){
            lock (_syncExts) {//In case first call is to this method, use the property. Will be recursive, but that's fine, since it won't be null.
                imageExtensions.Add(extension.TrimStart('.', ' ').ToLowerInvariant(), matchingFormat);
            }
        }

        /// <summary>
        /// Supports Png, Jpeg, Gif, Bmp, and Tiff. Throws a ArgumentOutOfRangeException if not png, jpeg, gif, bmp, or tiff
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetContentTypeFromImageFormat(ImageFormat format)
        {
            if (format == null) throw new ArgumentNullException();

            if (ImageFormat.Png.Equals(format))
                return "image/png"; //Changed from image/x-png to image/png on May 14, 2011, per http://www.w3.org/Graphics/PNG/
            else if (ImageFormat.Jpeg.Equals(format))
                return "image/jpeg";
            else if (ImageFormat.Gif.Equals(format))
                return "image/gif";
            else if (ImageFormat.Bmp.Equals(format))
                return "image/bmp";
            else if (ImageFormat.Tiff.Equals(format))
                return "image/tiff";
            else
            {
                throw new ArgumentOutOfRangeException("Unsupported format " + format.ToString());
            }

        }

        /// <summary>
        /// Returns the first ImageCodeInfo instance with the specified mime type. Returns null if there are no matches.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetImageCodeInfo(string mimeType) {
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in info)
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)) return ici;
            return null;
        }


        /// <summary>
        /// Saves the specified image to the specified stream using jpeg compression of the specified quality.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="quality">A number between 0 and 100. Defaults to 90 if passed a negative number. Numbers over 100 are truncated to 100. 
        /// 90 is a *very* good setting.
        /// </param>
        /// <param name="target"></param>
        public static void SaveJpeg(Image b, Stream target, int quality) {
            #region Encoder paramater notes
            //image/jpeg
            //  The parameter list requires 172 bytes.
            //  There are 4 EncoderParameter objects in the array.
            //    Parameter[0]
            //      The category is Transformation.
            //      The data type is Long.
            //      The number of values is 5.
            //    Parameter[1]
            //      The category is Quality.
            //      The data type is LongRange.
            //      The number of values is 1.
            //    Parameter[2]
            //      The category is LuminanceTable.
            //      The data type is Short.
            //      The number of values is 0.
            //    Parameter[3]
            //      The category is ChrominanceTable.
            //      The data type is Short.
            //      The number of values is 0.


            //  http://msdn.microsoft.com/en-us/library/ms533845(VS.85).aspx
            // http://msdn.microsoft.com/en-us/library/ms533844(VS.85).aspx
            // TODO: What about ICC profiles
            #endregion
            
            //Validate quality
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;
            //Prepare paramater for encoder
            using (EncoderParameters p = new EncoderParameters(1)) {
                using (var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality))
                {
                    p.Param[0] = ep;
                    //save
                    b.Save(target, GetImageCodeInfo("image/jpeg"), p);
                }
            }
        }
        
         /// <summary>
        /// Saves the image in png form. If Stream 'target' is not seekable, a temporary MemoryStream will be used to buffer the image data into the stream
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        public static void SavePng(Image img, Stream target)
        {
            if (!target.CanSeek) {
                //Write to an intermediate, seekable memory stream (PNG compression requires it)
                using (MemoryStream ms = new MemoryStream(4096)) {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.WriteTo(target);
                }
            } else {
                //image/png
                //  The parameter list requires 0 bytes.
                img.Save(target, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        public static void SaveBmp(Image img, Stream target)
        {
            //  image/bmp
            //  The parameter list requires 0 bytes.
            img.Save(target, ImageFormat.Bmp);
        }


        public static void SaveGif(Image img, Stream target) {
            //image/gif
            //  The parameter list requires 0 bytes.
            img.Save(target, ImageFormat.Gif);
        }

        #endregion



     
        /// <summary>
        /// Returns the querystring keys used by DefaultEncoder (quality, format, and thumbnail)
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "quality", "format", "thumbnail" };
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Returns signatures for jpeg, bmp, gif, png, wmf, ico, and tif
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FileSignature> GetSignatures()
        {
            //Source http://www.filesignatures.net/
            return new FileSignature[]{
                new FileSignature(new byte[] {0xFF, 0xD8, 0xFF}, "jpg", "image/jpeg"),
                new FileSignature(new byte[] {0x42, 0x4D}, "bmp", "image/x-ms-bmp"), //Can be a BMP or DIB
                new FileSignature(new byte[] {0x47,0x49,0x46, 0x38}, "gif", "image/gif"),
                new FileSignature(new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, "png","image/png"),
                new FileSignature(new byte[] {0xD7, 0xCD, 0xC6, 0x9A}, "wmf", "image/x-wmf"),
                new FileSignature(new byte[] {0x00, 0x00,0x01, 0x00}, "ico", "image/x-icon"), //Can be a printer spool or an icon
                new FileSignature(new byte[] {0x49, 0x20, 0x49}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x49, 0x49, 0x2A, 0x00}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x4D, 0x4D, 0x00, 0x2A}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x4D, 0x4D, 0x00, 0x2B}, "tif", "image/tiff")
            };
        }
    }
}
