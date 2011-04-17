using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using System.Drawing.Imaging;
using System.Drawing;
using ImageResizer;
using ImageResizer.Plugins;
using System.IO;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Plugins.PrettyGifs {
    public class PrettyGifs :IEncoder, IPlugin, IQuerystringPlugin {
        public PrettyGifs() { }


        public PrettyGifs(Image original, ResizeSettings settings) {
            ResizeSettings q = settings;
            //Parse output format
            OutputFormat = GetFormatIfSuitable(original, settings); 
            //Parse colors
            int colors = -1;
            if (!string.IsNullOrEmpty(q["colors"]))
                if (int.TryParse(q["colors"], out colors))
                    this.Colors = colors;
            //Parse dither settings
            if (!string.IsNullOrEmpty(q["dither"])) {
                if ("true".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.Dither = true;
                else if ("4pass".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.FourPassDither = true;
                else {
                    int dither;
                    if (int.TryParse(q["dither"], out dither)) {
                        DitherPercent = dither;
                        Dither = true;
                    }
                }

            }
        }

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

        private ImageFormat _outputFormat = ImageFormat.Jpeg;
        /// <summary>
        /// If you set this to anything other than Gif, Png, or Jpeg, it will throw an exception. Defaults to Jpeg
        /// </summary>
        public ImageFormat OutputFormat {
            get { return _outputFormat; }
            set {
                if (!IsValidOutputFormat(value)) throw new ArgumentException(value.ToString() + " is not a valid OutputFormat for PrettyGifs.");
                _outputFormat = value;
            }
        }

        /// <summary>
        /// Returns an encoder instance if a Gif or 8-bit png is requested.
        /// </summary>
        /// <param name="originalImage"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IEncoder CreateIfSuitable(Image originalImage, ResizeSettings settings) {
            ImageFormat f = GetFormatIfSuitable(originalImage, settings);

            if (f == ImageFormat.Gif || (f == ImageFormat.Png && settings["colors"] != null)) return new PrettyGifs(originalImage, settings);
            return null;
        }

        public ImageFormat GetFormatIfSuitable(Image original, ResizeSettings settings) {
            //What format was the image originally (used as a fallback).
            ImageFormat originalFormat = DefaultEncoder.GetOriginalFormat(original);
            if (!IsValidOutputFormat(originalFormat)) originalFormat = null;//No valid info available about the original format.

            //What format was specified?
            ImageFormat requestedFormat = DefaultEncoder.GetRequestedFormat(settings.Format, originalFormat); //fallback to originalFormat if not specified.

            if (IsValidOutputFormat(requestedFormat)) return requestedFormat;
            return null;
        }

        /// <summary>
        /// Writes the indexed PNG or GIF file
        /// </summary>
        /// <param name="i"></param>
        /// <param name="s"></param>
        public void Write(Image i, System.IO.Stream s) {
            bool useMax = (Colors < 0);
            byte colors = (byte)Math.Min(Math.Max(this.Colors, 1), 255);

            if (OutputFormat == ImageFormat.Png) {

                if (useMax)
                    DefaultEncoder.SavePng(i, s);
                else
                    SaveIndexed(ImageFormat.Png, i, s, colors, Dither || FourPassDither, FourPassDither, DitherPercent);
            } else if (OutputFormat == ImageFormat.Gif) {

                if (useMax)
                    SaveIndexed(ImageFormat.Gif, i, s, 255, Dither || FourPassDither, FourPassDither, DitherPercent);
                else
                    SaveIndexed(ImageFormat.Gif, i, s, colors, Dither || FourPassDither, FourPassDither, DitherPercent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="img"></param>
        /// <param name="target"></param>
        /// <param name="colors"></param>
        /// <param name="dither"></param>
        /// <param name="fourPass"></param>
        /// <param name="ditherPercent"></param>
        /// <param name="useGdiQuantization"></param>
        public static void SaveIndexed(System.Drawing.Imaging.ImageFormat format, Image img, Stream target, byte colors, bool dither, bool fourPass, int ditherPercent) {
            //NeoQuant may be the best, but it's too slow
            //http://pngnq.sourceforge.net/index.html 
            //http://members.ozemail.com.au/~dekker/NEUQUANT.HTML


            //http://codebetter.com/blogs/brendan.tompkins/archive/2007/06/14/gif-image-color-quantizer-now-with-safe-goodness.aspx
            //TODO: add Octree and Grayscale quantizer
            //TODO: Preserve transparency

            OctreeQuantizer quantizer = new OctreeQuantizer(colors, GetBitsNeededForColorDepth(colors));
            quantizer.Dither = dither;
            quantizer.fourPass = fourPass;
            quantizer.DitherPercent = (float)ditherPercent / 100;
            using (Bitmap quantized = quantizer.Quantize(img)) {
                //If we are encoding in PNG, and writing to a non-seekable stream,
                //we have to buffer it all in memory or we'll get an exception
                if (!target.CanSeek && format == ImageFormat.Png) {
                    using (MemoryStream ms = new MemoryStream(4096)) {
                        quantized.Save(ms,format); //Recursive call
                        ms.WriteTo(target);
                    }
                } else {
                    //Everything else
                    quantized.Save(target, format);
                }
            }
        }
        /// <summary>
        /// Returns how many bits are required to store the specified number of colors. Performs a Log2() on the value.
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static int GetBitsNeededForColorDepth(byte colors) {
            return (int)Math.Ceiling(Math.Log(colors, 2));
        }


        /// <summary>
        /// Returns true if the this encoder supports the specified image format
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public bool IsValidOutputFormat(ImageFormat f) {
            return (f == ImageFormat.Gif || f == ImageFormat.Png);
        }


        public bool SupportsTransparency {
            get { return true; }
        }

        public string MimeType {
            get { return DefaultEncoder.GetContentTypeFromImageFormat(OutputFormat); }
        }

        public string Extension {
            get { return DefaultEncoder.GetExtensionFromImageFormat(OutputFormat); }
        }



        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "colors", "dither" };
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
