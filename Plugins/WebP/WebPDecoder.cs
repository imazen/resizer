using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.ExtensionMethods;
using System.Drawing;
using System.IO;
using Imazen.WebP;

namespace ImageResizer.Plugins.WebPDecoder {
    public class WebPDecoderPlugin:BuilderExtension, IPlugin, IFileExtensionPlugin {



        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            bool requested = "webp".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(settings["decoder"]) && !requested) return null; //Don't take it away from the requested decoder

            //If a .webp is coming in, try first, before Bitmap tries to parse it.
            if (requested || (optionalPath != null && optionalPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))) {
                return Decode(s);
            }
            return null;
        }
        public override Bitmap DecodeStreamFailed(Stream s, ResizeSettings settings, string optionalPath) {
            //Catch WebP files not ending in .webp
            try {
                return Decode(s);
            } catch {

                return null;
            }
        }


        private Bitmap Decode(Stream s) {
            long length;
            byte[] buffer = s.CopyOrReturnBuffer(out length,false,4096);
            return new SimpleDecoder().DecodeFromBytes(buffer, length);
        }


        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedFileExtensions() {
            return new string[] { ".webp" };
        }
    }
}
