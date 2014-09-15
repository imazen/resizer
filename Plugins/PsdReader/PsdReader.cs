/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using PhotoshopFile;
using System.Web;
using System.IO;
using System;
using System.Globalization;
namespace ImageResizer.Plugins.PsdReader {

    /// <summary>
    /// PsdReader Reads Photoshop Psd files
    /// </summary>
    public class PsdReader : ImageResizer.Resizing.BuilderExtension, IPlugin, IFileExtensionPlugin {

        /// <summary>
        /// Initialize the PsdReader class
        /// </summary>
        public PsdReader() { }

        /// <summary>
        /// Install the PsdReader to the given config
        /// </summary>
        /// <param name="c">ImageResizer configuration</param>
        /// <returns>PsdReader plugin that was added to the config</returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the plugin from the given config
        /// </summary>
        /// <param name="c">ImageResizer config</param>
        /// <returns>true if the plugin has been removed</returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// IEnumerable colleciton of supported Psd File Extensions supported by the plugin
        /// </summary>
        /// <returns>IEnumerable collection of File extensions</returns>
        public IEnumerable<string> GetSupportedFileExtensions() {
            return new string[] { ".psd" };
        }

        /// <summary>
        /// Decodes the given stream into a Bitmap
        /// </summary>
        /// <param name="s">image I/O stream</param>
        /// <returns>Decoded Bitmap</returns>
        public Bitmap Decode(Stream s) {
            //Bitmap we will render to
            System.Drawing.Bitmap b = null;

            //Time just the parsing/rendering
            Stopwatch swRender = new Stopwatch();
            swRender.Start();

            PsdFile psdFile = new PsdFile();
            psdFile.Load(s);
            //Load background layer
            b = ImageDecoder.DecodeImage(psdFile); //Layers collection doesn't include the composed layer

            //How fast?
            swRender.Stop();
            trace("Parsing and rendering PSD to a Bitmap instance took " + swRender.ElapsedMilliseconds.ToString(NumberFormatInfo.InvariantInfo) + "ms");

            return b;
        }

        private static void trace(string msg) {
            Trace.Write(msg);

        }

        /// <summary>
        /// Decodes teh stream applying ImageResizer settings 
        /// </summary>
        /// <param name="s">image I/O Stream</param>
        /// <param name="settings">ImageResizer settings</param>
        /// <param name="optionalPath">path to image to decode directly</param>
        /// <returns></returns>
        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            bool requested = "psdreader".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(settings["decoder"]) && !requested) return null; //Don't take it away from the requested decoder

            //If a .psd is coming in, try first, before Bitmap tries to parse it.
            if (requested || (optionalPath != null && optionalPath.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))) {
                return Decode(s);
            }
            return null;
        }

        /// <summary>
        /// Decode the image into a Bitmap
        /// </summary>
        /// <param name="s">Image I/O stream</param>
        /// <param name="settings">ImageResizer settings</param>
        /// <param name="optionalPath">path to image</param>
        /// <returns>Decoded Bitmap or null if it can't be decoded</returns>
        public override Bitmap DecodeStreamFailed(Stream s, ResizeSettings settings, string optionalPath) {
            //Catch Photoshop files not ending in .psd
            try {
                return Decode(s);
            } catch {

                return null;
            }
        }
    }
}
  