/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights */
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using PhotoshopFile;
using System.Web;
using System.IO;
using System;
namespace ImageResizer.Plugins.PsdReader {
    public class PsdReader : ImageResizer.Resizing.BuilderExtension, IPlugin, IFileExtensionPlugin {

        public PsdReader() { }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

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
            trace("Parsing and rendering PSD to a Bitmap instance took " + swRender.ElapsedMilliseconds.ToString() + "ms");

            return b;
        }

        private static void trace(string msg) {
            Trace.Write(msg);

        }

        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            //If a .psd is coming in, try first, before Bitmap tries to parse it.
            if (optionalPath != null && optionalPath.EndsWith(".psd", StringComparison.OrdinalIgnoreCase)) {
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
  