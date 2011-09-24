using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Issues;
using FreeImageAPI;
using System.Drawing;
using System.IO;

namespace ImageResizer.Plugins.FreeImageDecoder {
    public class FreeImageDecoderPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider {

        private static IEnumerable<string> supportedExts = null;
        public IPlugin Install(Configuration.Config c) {
            if (supportedExts == null && FreeImage.IsAvailable()) {
                supportedExts = BuildSupportedList();
            }
            c.Plugins.add_plugin(this);
            return this;
        }



        public IEnumerable<string> BuildSupportedList() {
            FREE_IMAGE_FORMAT[] formats = (FREE_IMAGE_FORMAT[])Enum.GetValues(typeof(FREE_IMAGE_FORMAT));
            List<string> extensions = new List<string>();
            foreach (FREE_IMAGE_FORMAT format in formats)
                if (format != FREE_IMAGE_FORMAT.FIF_UNKNOWN)
                    extensions.AddRange(FreeImage.GetFIFExtensionList(format).Split(','));
            return extensions;

        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedFileExtensions() {
            if (supportedExts == null) return new string[] { };
            else return supportedExts;
        }

        public override System.Drawing.Bitmap DecodeStream(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            if (!"true".Equals(settings["freeimage"], StringComparison.OrdinalIgnoreCase)) return null;

            return Decode(s,  settings,);
        }

        public override System.Drawing.Bitmap DecodeStreamFailed(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            try {
                return Decode(s,settings);
            } catch {
                return null;
            }
        }

        public Bitmap Decode(Stream s, ResizeSettings settings) {
            if (!FreeImageAPI.FreeImage.IsAvailable()) return null;

            FIBITMAP original = FreeImage.LoadFromStream(s, FREE_IMAGE_LOAD_FLAGS.JPEG_FAST);
            if (original.IsNull) return null;
            try {
                return FreeImage.GetBitmap(original);

            } finally {
                if (!original.IsNull) FreeImage.UnloadEx(ref original);
            }
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }
    }
}
