using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Collections.Specialized;
using fbs.ImageResizer.Plugins;

namespace fbs.ImageResizer.Encoding {
    public class DefaultEncoder :IImageEncoder, IUrlPlugin {
        /// <summary>
        /// Tries to parse an ImageFormat from the settings.Format value.
        /// If an unrecogized format is specified, returns null.
        /// If an unsupported format is specified, it is returned.
        /// If *no* format is specified, returns defaultValue.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual ImageFormat GetRequestedFormat(string format, ImageFormat defaultValue) {
            ImageFormat f = null;
            if (!string.IsNullOrEmpty(format)){
                f = DefaultEncoder.GetImageFormatFromExtension(format);
                return f;
            }
            //Fallback. No encoder was explicitly specified, so let's try to infer it from the image data.
            return defaultValue;

        }
        /// <summary>
        /// Attempts to determine the ImageFormat of the source image. First attempts to parse the path, if a string is present in original.Tag. 
        /// Falls back to using original.RawFormat. Returns null if both 'original' is null.
        /// RawFormat has a bad reputation, so this may return unexpected values, like MemoryBitmap or something in some situations.
        /// </summary>
        /// <param name="img">The image we are encoding</param>
        /// <param name="original">The source image that was loaded from a stream</param>
        /// <returns></returns>
        public virtual ImageFormat GetOriginalFormat(Image original){
            if (original == null) return null;
            //Try to parse the original file extension first.
            string path = original.Tag as string;
            //We have a path? Parse it!
            if (path != null) {
                ImageFormat f = DefaultEncoder.GetImageFormatFromPhysicalPath(path);
                if (f != null) return f; //From the path
            }
            //Ok, I guess it there (a) wasn't a path, or (b), it didn't have a recognizeable extension
            return original.RawFormat;
        }





        public DefaultEncoder() {
        }
        public DefaultEncoder(ImageFormat outputFormat) {
            this.OutputFormat = outputFormat;
        }
        public DefaultEncoder(ImageFormat outputFormat, int jpegQuality) {
            this.OutputFormat = outputFormat;
            this.Quality = jpegQuality;
        }

        public DefaultEncoder(Image original, ResizeSettings settings) {
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
                if (int.TryParse(settings["quality"], out quality))
                    this.Quality = quality;

        }
        
        public virtual IImageEncoder CreateIfSuitable(Image original, ResizeSettings settings) {
            ImageFormat requestedFormat = GetRequestedFormat(settings.Format, ImageFormat.Jpeg);
            if (requestedFormat == null || !IsValidOutputFormat(requestedFormat)) return null; //An unsupported format was explicitly specified.
            return new DefaultEncoder(original, settings);
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
            return (f == ImageFormat.Gif || f == ImageFormat.Png || f == ImageFormat.Jpeg);
        }


        private int quality = 90;
        /// <summary>
        /// The Jpeg compression quality. 90 is the best setting. Not relevant in Png or Gif compression
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
            if (OutputFormat == ImageFormat.Jpeg) SaveJpeg(image, s, this.Quality);
            else if (OutputFormat == ImageFormat.Png) SavePng(image, s);
            else if (OutputFormat == ImageFormat.Gif) SaveGif(image, s);
        }

        /// <summary>
        /// Returns true if the desired output type supports transparency.
        /// </summary>
        public bool SupportsTransparency {
            get {
                return (OutputFormat == ImageFormat.Gif || OutputFormat == ImageFormat.Png); //Does Gif transparency work?
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
        /// <param name="ext"></param>
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
            if (ext == null) return null;
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
        /// Supports Png, Jpeg, Gif, Bmp, and Tiff.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetContentTypeFromImageFormat(ImageFormat format)
        {
            if (format == ImageFormat.Png)
                return "image/x-png";
            else if (format == ImageFormat.Jpeg)
                return "image/jpeg";
            else if (format == ImageFormat.Gif)
                return "image/gif";
            else if (format == ImageFormat.Bmp)
                return "image/x-ms-bmp";
            else if (format == ImageFormat.Tiff)
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
            EncoderParameters p = new EncoderParameters(1);
            p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
            //save
            b.Save(target, GetImageCodeInfo("image/jpeg"), p);
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
                    SavePng(img,target); //Recursive call
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
        /// Returns a collection of the 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedFileExtensions() {
            ////Filter the imageExtensions dict using IsValidOutputFormat.
            //List<string> exts = new List<string>(10);
            //foreach (KeyValuePair<string, ImageFormat> p in DefaultEncoder.imageExtensions) {
            //    if (IsValidOutputFormat(p.Value)) exts.Add(p.Key);
            //}
            //DefaultEncoder.
            //return exts;
            return null;
        }

        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "quality", "format", "thumbnail" };
        }
    }
}
