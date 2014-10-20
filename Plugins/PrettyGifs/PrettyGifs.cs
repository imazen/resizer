/* Copyright (c) 2014 Imazen See license.txt for your rights */
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
using System.Security.Permissions;
using System.Security;
using ImageResizer.Util;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.PrettyGifs {
    /// <summary>
    /// Replaces .NET's poor default GIF encoding algorithm with Octree quantization and dithering, and allows 8-bit PNG creation. Compatible with all plugins.
    /// </summary>
    public class PrettyGifs :IEncoder, IPlugin, IQuerystringPlugin {
        /// <summary>
        /// Creates a new instance of the plugin
        /// </summary>
        public PrettyGifs() { }

        private ResizeSettings query;

        /// <summary>
        /// Creates a new instance of the plugin as an encoder 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="original"></param>
        public PrettyGifs(ResizeSettings settings, object original) {

            this.query = new ResizeSettings(settings);
            ResizeSettings q = settings;
            //Parse output format
            OutputFormat = GetFormatIfSuitable(settings, original); 
            //Parse colors
            int colors = -1;
            if (!string.IsNullOrEmpty(q["colors"]))
                if (int.TryParse(q["colors"], NumberStyles.Integer,NumberFormatInfo.InvariantInfo, out colors))
                    this.Colors = colors;
            //Parse dither settings
            if (!string.IsNullOrEmpty(q["dither"])) {
                if ("true".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.Dither = true;
                else if ("4pass".Equals(q["dither"], StringComparison.OrdinalIgnoreCase))
                    this.FourPassDither = true;
                else {
                    int dither;
                    if (int.TryParse(q["dither"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out dither)) {
                        DitherPercent = dither;
                        Dither = true;
                    }
                }

            }
            
            PreservePalette = q.Get<bool>("preservePalette", PreservePalette);
            if (PreservePalette && original is Image && ((Image)original).Palette.Entries.Length > 0) {
                originalPalette = ((Image)original).Palette;
            }
        }




        private ColorPalette originalPalette = null;

        private bool _preservePalette = false;
        /// <summary>
        /// If true, the original palette will be used if it exists. May cause serious color problems if new content has been added to the image.
        /// </summary>
        public bool PreservePalette {
            get { return _preservePalette; }
            set { _preservePalette = value; }
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

        /// <summary>
        /// 
        /// </summary>
        public bool FourPassDither = false;

        /// <summary>
        /// How much of the error should be passed on (in negative form) to neighbor pixels
        /// </summary>
        public int DitherPercent = 30;

        private ImageFormat _outputFormat = ImageFormat.Gif;
        /// <summary>
        /// If you set this to anything other than Gif or Png, it will throw an exception. Defaults to GIF.
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
        /// <param name="original"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IEncoder CreateIfSuitable( ResizeSettings settings, object original) {
            //Opt out if the user is requesting a different one.
            if (!string.IsNullOrEmpty(settings["encoder"]) && !"prettygifs".Equals(settings["encoder"], StringComparison.OrdinalIgnoreCase)) return null;
            ImageFormat f = GetFormatIfSuitable(settings, original);
            
            if (ImageFormat.Gif.Equals(f) || (ImageFormat.Png.Equals(f) && settings["colors"] != null)) return new PrettyGifs(settings, original);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        public ImageFormat GetFormatIfSuitable(ResizeSettings settings, object original) {
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

            if (ImageFormat.Png.Equals(OutputFormat)) {

                if (useMax)
                    DefaultEncoder.SavePng(i, s);
                else
                    SaveIndexed(ImageFormat.Png, i, s, colors, Dither || FourPassDither, FourPassDither, DitherPercent);
            } else if (ImageFormat.Gif.Equals(OutputFormat)) {

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
        public void SaveIndexed(System.Drawing.Imaging.ImageFormat format, Image img, Stream target, byte colors, bool dither, bool fourPass, int ditherPercent) {
      

            if (PreservePalette && originalPalette != null) {
                img.Palette = originalPalette;
                //If we are encoding in PNG, and writing to a non-seekable stream,
                //we have to buffer it all in memory or we'll get an exception
                if (!target.CanSeek && ImageFormat.Png.Equals(format)) {
                    using (MemoryStream ms = new MemoryStream(4096)) {
                        img.Save(ms,format); //Recursive call
                        ms.WriteTo(target);
                    }
                } else {
                    //Everything else
                    img.Save(target, format);
                }
                return;
            }
            

            OctreeQuantizer quantizer = new OctreeQuantizer(colors, GetBitsNeededForColorDepth(colors));

            if (query.Get<bool>("fulltrust",HasFullTrust)) {
                quantizer.Dither = dither;
                quantizer.FourPass = fourPass;
                quantizer.DitherPercent = (float)ditherPercent / 100;
            } else {
                quantizer.FullTrust = false;
                quantizer.OmitFinalStage = true;
                quantizer.ResizeForFirstPass = true;

                quantizer.FirstPassPixelCount = (long)Math.Pow(query.Get<int>( "pixelCount", (int)Math.Sqrt(quantizer.FirstPassPixelCount)),2);
                quantizer.FirstPassPixelThreshold = (long)Math.Pow(query.Get<int>( "pixelThreshold", (int)Math.Sqrt(quantizer.FirstPassPixelThreshold)), 2);
                
            }
            using (Bitmap quantized = quantizer.Quantize(img)) {
                //If we are encoding in PNG, and writing to a non-seekable stream,
                //we have to buffer it all in memory or we'll get an exception
                if (!target.CanSeek && ImageFormat.Png.Equals(format)) {
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

        private static bool _hasFullTrust = false;
        private static bool _hasFullTrustSet = false;
        /// <summary>
        /// Returns true if the assembly can call unmanged code (i.e, has full trust)
        /// </summary>
        public static bool HasFullTrust {
            get {
                if (_hasFullTrustSet) return _hasFullTrust;
                
                try {
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                    _hasFullTrust = true;
                } catch (SecurityException) {
                    _hasFullTrust = false;
                }
                _hasFullTrustSet = true;
                return _hasFullTrust;
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
            return (ImageFormat.Gif.Equals(f) || ImageFormat.Png.Equals(f));
        }

        /// <summary>
        /// If the configured encoding settings support transparency.
        /// </summary>
        public bool SupportsTransparency {
            get { return true; }
        }

        /// <summary>
        /// The suggested mime-type for the output image produced by this encoder
        /// </summary>
        public string MimeType {
            get { return DefaultEncoder.GetContentTypeFromImageFormat(OutputFormat); }
        }

        /// <summary>
        /// The suggested extension for the output image produced by this encoder
        /// </summary>
        public string Extension {
            get { return DefaultEncoder.GetExtensionFromImageFormat(OutputFormat); }
        }


        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "colors", "dither" };
        }

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
    }
}
