/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 **/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageQuantization;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Configuration;

namespace fbs.ImageResizer
{
    /// <summary>
    /// Extracts the image output and encoding settings from the querystring. 
    /// Handles the saving of Image instances to a Stream using the .SaveImage method
    /// Doesn't handle animated image files - only single frames. (Neither does GDI)
    /// </summary>
    public class ImageOutputSettings
    {
        public ImageOutputSettings(ImageFormat targetFormat) {
            setCustomQuantizationDefault();
            this.OutputFormat = targetFormat;
        }
        public ImageOutputSettings(yrl request)
        {
            setCustomQuantizationDefault(); 
            parseFromQuerystring(ImageOutputSettings.GetImageFormatFromExtension(request.Extension), request.QueryString);
        }
        public ImageOutputSettings(ImageFormat originalFormat, NameValueCollection q)
        {
            setCustomQuantizationDefault();
            parseFromQuerystring(originalFormat, q);
           
        }

        private void setCustomQuantizationDefault()
        {
            DisableCustomQuantization =
                ("true".Equals(ConfigurationManager.AppSettings["DisableCustomQuantization"], StringComparison.OrdinalIgnoreCase));
        }

        /// thumbnail|format=jpg|jpeg|png|gif|bmp (default original)
        /// colors=2-256|max
        /// quality=0-100 (default 90) (jpeg compression quality)
        /// </summary>
        /// <param name="q"></param>
        public void parseFromQuerystring(ImageFormat originalFormat, NameValueCollection q)
        {
            ImageFormat requested = GetRequestedType(q);
            //Use the original image type if the requested type is not present.
            if (requested == null) requested = originalFormat;
            if (requested != null) OutputFormat = requested;

            //Parse colors
            int colors = -1;
            if (!string.IsNullOrEmpty(q["colors"]))
                if (int.TryParse(q["colors"], out colors))
                    this.Colors = colors;

            //parse quality;
            int quality = 90;
            if (!string.IsNullOrEmpty(q["quality"]))
                if (int.TryParse(q["quality"], out quality))
                    this.Quality = quality;

            if (!string.IsNullOrEmpty(q["dither"])){
                if ("true".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.Dither = true;
                else if ("4pass".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.FourPassDither = true;
                else
                {
                    int dither;
                    if (int.TryParse(q["dither"], out dither))
                    {
                        DitherPercent = dither;
                        Dither = true;
                    }
                }

            }

        }

        public ImageFormat OriginalFormat = null;
        private ImageFormat _outputFormat = ImageFormat.Jpeg;
        /// <summary>
        /// If you set this to an unsupported output format (anything other than png, gif, or jpg), it will be the same as settting it to Jpeg. Input is autocorrected.
        /// </summary>
        public ImageFormat OutputFormat
        {
            get { return _outputFormat; }
            set { _outputFormat = GetSupportedOutputFormat(value); }
        }
        /// <summary>
        /// The Jpeg compression quality. 90 is the best setting. 
        /// </summary>
        public int Quality = 90;
        /// <summary>
        /// The number of colors to use. Only applicable for png, gif, and bmp photos where palletes can be used.
        /// The default is -1, which means "as much color fidelity as possible". 
        /// </summary>
        public int Colors = -1;

        /// <summary>
        /// Enables dithering for PNG8 and GIF
        /// </summary>
        public bool Dither = false;
        public bool FourPassDither = false;
        public int DitherPercent = 30;

        /// <summary>
        /// Prevents custom quantization for GIFs, 8-bit PNGs, and 8-bit BMPs. When disabled, the default GDI quantization is used, which 
        /// produces poor-quality web-palette images. 
        /// The default quantization is faster, and sometimes is the only method that works 
        /// (such as in a low-trust environment or where the Marshal class is banned)
        /// </summary>
        public bool DisableCustomQuantization = false;

        /// <summary>
        /// Returns the file extension for the current OutputFormat
        /// </summary>
        /// <returns></returns>
        public string GetFinalExtension()
        {
            return ImageOutputSettings.GetExtensionFromImageFormat(OutputFormat);
        }
        /// <summary>
        /// Returns the content type for the current OutputFormat
        /// </summary>
        /// <returns></returns>
        public string GetContentType()
        {
            return ImageOutputSettings.GetContentTypeFromImageFormat(OutputFormat);
        }

        /// <summary>
        /// Safe for use with non-seekable streams. Writes the Png memory to an intermediate MemoryStream.
        /// Does use more memory on PNGs. FileStreams are seekable.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="i"></param>
        public void SaveImageToNonSeekableStream(Stream s, Image i)
        {
            if (OutputFormat == ImageFormat.Png)
            {
                //Write to an intermediate, seekable memory stream (PNG compression requires it)
                using (MemoryStream ms = new MemoryStream(2048))
                {
                    SaveImage(ms, i);
                    ms.WriteTo(s);
                }
            }
            else
            {
                SaveImage(s, i);
            }
           //June 3: Fixed typo: SaveImage(s,i) was duplicated here... without an else statement
        }

        /// <summary>
        /// Requires a seekable string for Png encoding. Use SaveImageToNonSeekableStream for non-seekable streams.
        /// </summary>
        /// <param name="s"></param>
        public void SaveImage(Stream s, Image i )
        {
            bool useMax = (Colors < 0);
            byte colors = (byte)Math.Min(Math.Max(this.Colors, 1), 255);

            if (OutputFormat == ImageFormat.Jpeg)
            {
                ImageOutputSettings.SaveJpeg(i, this.Quality, s);
            }
            else if (OutputFormat == ImageFormat.Png)
            {

                if (useMax)
                    ImageOutputSettings.SavePng(i, s);
                else
                    ImageOutputSettings.SaveIndexed(ImageFormat.Png, i, s, colors, Dither || FourPassDither, FourPassDither,DitherPercent,DisableCustomQuantization);
            }
            else if (OutputFormat == ImageFormat.Gif)
            {

                if (useMax)
                    ImageOutputSettings.SaveIndexed(ImageFormat.Gif, i, s, 255, Dither || FourPassDither, FourPassDither, DitherPercent,DisableCustomQuantization);
                else
                    ImageOutputSettings.SaveIndexed(ImageFormat.Gif, i, s, colors, Dither || FourPassDither, FourPassDither, DitherPercent,DisableCustomQuantization);
            }
            
        }
        /// <summary>
        /// If the specified ImageFormat is not supported for output, ImageFormat.Jpeg is returned.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static ImageFormat GetSupportedOutputFormat(ImageFormat format)
        {
            if (format == ImageFormat.Png) return format;
            if (format == ImageFormat.Gif) return format;
            return ImageFormat.Jpeg;
        }

        /// <summary>
        /// Returns true if the desired output type supports transparency.
        /// </summary>
        public bool SupportsTransparency
        {
            get
            {
                return (OutputFormat == ImageFormat.Gif || OutputFormat == ImageFormat.Png) ;
            }
        }
     


        /// <summary>
        /// Returns the ImageFormat requested in the querystring.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static ImageFormat GetRequestedType(NameValueCollection q){
            string type = "";
            //Use the value from 'thumbnail' if available.
            if (!string.IsNullOrEmpty(q["thumbnail"])) type = q["thumbnail"].ToLowerInvariant().Trim();
            //If that didn't work, try using the value from 'format'
            if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(q["format"])) type = q["format"].ToLowerInvariant().Trim();
            if (type == null) return null;
            return  ImageOutputSettings.GetImageFormatFromExtension(type);

        }
 

        /// <summary>
        /// Returns the ImageFormat enumeration value based on the extension in the specified physical path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromPhysicalPath(string path)
        {
            return GetImageFormatFromExtension(System.IO.Path.GetExtension(path));
        }

        /// <summary>
        /// Returns an ImageFormat instance from the specfied extension. Supports jpg, jpeg, bmp, gif, png, tiff, and tff.
        /// returns null if not recognized.
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromExtension(string ext)
        {
            ext = ext.Trim(' ','.').ToLowerInvariant();
            switch (ext)
            {
                case "jpg": return ImageFormat.Jpeg; 
                case "bmp": return ImageFormat.Bmp;
                case "gif": return ImageFormat.Gif;
                case "jpeg": return ImageFormat.Jpeg; 
                case "png": return  ImageFormat.Png;
                case "tiff": return ImageFormat.Tiff;
                case "tff": return ImageFormat.Tiff;
                case "tif": return ImageFormat.Tiff;
                  
            }
            return null;
        }
        /// <summary>
        /// Returns an string instance from the specfied ImageFormat. Supports jpg, bmp, gif, png, and tiff,
        /// Returns null if not recognized.
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static string GetExtensionFromImageFormat(ImageFormat format)
        {
            if (format == ImageFormat.Jpeg) return "jpg";
            if (format == ImageFormat.Png) return "png";
            if (format == ImageFormat.Bmp) return "bmp";
            if (format == ImageFormat.Gif) return "gif";
            if (format == ImageFormat.Tiff) return "tif";
            return null;
        }
        /// <summary>
        /// Returns true if the extension on the virtual path 'path' is one of the accepted types
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAcceptedImageType(string path){
            string extension = System.IO.Path.GetExtension(path).ToLowerInvariant().Trim('.');
            return _acceptedImageExtensions.Contains(extension);

        }
        /// <summary>
        /// Returns a list of (lowercase invariant) image extensions that the module works with.
        /// </summary>
        private static IList<String> _acceptedImageExtensions = new List<String>(new String[] { "jpg", "jpeg", "bmp", "gif", "png", "tff","tiff","tif" });

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


        
        //effects

        //On entire target
        //alpha = 0-100

        // http://msdn.microsoft.com/en-us/library/ms533846(VS.85).aspx

            //Per-pixel filters:
            //http://www.codeproject.com/KB/GDI-plus/csharpgraphicfilters11.aspx


            /* TODO
             * 
             * blur = 0-100
             * hue, saturation, lightness adjustments (h,s,l)
             * 
             */


            //alpha = 0-100
            //      = fade(20,60),angle(0)

            //Cool upscaling code for image enhancement
            //http://www.codeproject.com/KB/GDI-plus/imgresizoutperfgdiplus.aspx

            //return null;

            // http://msdn.microsoft.com/en-us/library/ms533846(VS.85).aspx
       // }


        /// <summary>
        /// Saves the specified image to the specified stream using jpeg compression of the specified quality.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="quality">A number between 0 and 100. Defaults to 90 if passed a negative number. Numbers over 100 are truncated to 100. 
        /// 90 is a *very* good setting.
        /// </param>
        /// <param name="target"></param>
        public static void SaveJpeg(Image b, int quality, Stream target)
        {
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
            // TODO: accept quality argument
            // TODO: What about ICC profiles
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;


            System.Drawing.Imaging.EncoderParameters encoderParameters;
            encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
            b.Save(target, GetImageCodeInfo("image/jpeg"), encoderParameters);
        }
        /// <summary>
        /// Colors argument has no effect when  useGdiQuantization is true.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        /// <param name="colors"></param>
        /// <param name="useGdiQuantization"></param>
        public static void SaveGif(Image img, Stream target, byte colors, bool useGdiQuantization)
        {
            SaveIndexed(ImageFormat.Gif, img, target, colors,false,false,0,useGdiQuantization);
        }
        /// <summary>
        /// Requires seekable stream, i.e. MemoryString or FileStream. Colors argument has no effect when  useGdiQuantization is true.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        /// <param name="colors"></param>
        public static void SavePng(Image img, Stream target, byte colors, bool useGdiQuantization)
        {
            SaveIndexed(ImageFormat.Png, img, target, colors,false,false,0, useGdiQuantization);
            
        }
        /// <summary>
        /// Colors, dither, fourPass, and ditherPercent arguments have no effect when  useGdiQuantization is true.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="img"></param>
        /// <param name="target"></param>
        /// <param name="colors"></param>
        /// <param name="dither"></param>
        /// <param name="fourPass"></param>
        /// <param name="ditherPercent"></param>
        /// <param name="useGdiQuantization"></param>
        public static void SaveIndexed(System.Drawing.Imaging.ImageFormat format, Image img, Stream target, byte colors, bool dither, bool fourPass, int ditherPercent, bool useGdiQuantization)
        {
            //image/gif
            //  The parameter list requires 0 bytes.
            //NeoQuant may be the best, but it's too slow
            //http://pngnq.sourceforge.net/index.html 
            //http://members.ozemail.com.au/~dekker/NEUQUANT.HTML


            //http://codebetter.com/blogs/brendan.tompkins/archive/2007/06/14/gif-image-color-quantizer-now-with-safe-goodness.aspx
            //TODO: add Octree and Grayscale quantizer
            //TODO: Preserve transparency

            if (useGdiQuantization)
            {
                img.Save(target, format);
                //TODO - handle changing the color count of img to match 'colors'
            }
            else
            {
                //image/png
                //  The parameter list requires 0 bytes.
                OctreeQuantizer quantizer = new OctreeQuantizer(colors, GetBitsNeededForColorDepth(colors));
                quantizer.Dither = dither;
                quantizer.fourPass = fourPass;
                quantizer.DitherPercent = (float)ditherPercent / 100;
                using (Bitmap quantized = quantizer.Quantize(img))
                {
                    quantized.Save(target, format);
                }
            }

        }
        /// <summary>
        /// Requires seekable stream, i.e. MemoryString or FileStream
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        public static void SavePng(Image img, Stream target)
        {
            //image/png
            //  The parameter list requires 0 bytes.
            img.Save(target, System.Drawing.Imaging.ImageFormat.Png);

        }
        public static void SaveBmp(Image img, Stream target)
        {
            //  image/bmp
            //  The parameter list requires 0 bytes.
            img.Save(target, ImageFormat.Bmp);
        }
        /// <summary>
        /// Saves the bitmap. colors argument has no effect when useGdiQuantization is true.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        /// <param name="colors"></param>
        public static void SaveBmp(Image img, Stream target, byte colors, bool useGdiQuantization)
        {
            if (useGdiQuantization)
            {
                img.Save(target, ImageFormat.Bmp); //Add color support later
            }
            else
            {
                //  image/bmp
                //  The parameter list requires 0 bytes.
                OctreeQuantizer quantizer = new OctreeQuantizer(colors, GetBitsNeededForColorDepth(colors));
                using (Bitmap quantized = quantizer.Quantize(img))
                {
                    quantized.Save(target, System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
        }
        /// <summary>
        /// Returns how many bits are required to store the specified number of colors. Performs a Log2() on the value.
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static int GetBitsNeededForColorDepth(byte colors)
        {
            return (int)Math.Ceiling(Math.Log(colors, 2));
        }
        /// <summary>
        /// Returns the first ImageCodeInfo instance with the specified mime type. Returns null if there are no matches.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetImageCodeInfo(string mimeType)
        {
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in info)
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)) return ici;
            return null;
        }

 
 

    }
}
