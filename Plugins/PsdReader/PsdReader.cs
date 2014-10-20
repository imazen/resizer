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
    /// Adds support for .PSD source files. No configuration required.
    /// </summary>
    public class PsdReader : ImageResizer.Resizing.BuilderExtension, IPlugin, IFileExtensionPlugin {

        /// <summary>
        /// Creates a new instance of PsdReader
        /// </summary>
        public PsdReader() { }

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
        /// <summary>
        /// Additional file types this plugin adds support for decoding.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedFileExtensions() {
            return new string[] { ".psd" };
        }

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

        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            bool requested = "psdreader".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(settings["decoder"]) && !requested) return null; //Don't take it away from the requested decoder

            //If a .psd is coming in, try first, before Bitmap tries to parse it.
            if (requested || (optionalPath != null && optionalPath.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))) {
                return Decode(s);
            }
            return null;
        }
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
  