using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Issues;
using FreeImageAPI;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace ImageResizer.Plugins.FreeImageDecoder {
    public class FreeImageDecoderPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider {
        public FreeImageDecoderPlugin() {
        }
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
            if (!"freeimage".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase)) return null;

            return Decode(s,  settings);
        }

        public override System.Drawing.Bitmap DecodeStreamFailed(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            try {
                 return Decode(s,settings);
                 
            } catch (Exception){
                return null;
            }
        }

        public Bitmap Decode(Stream s, ResizeSettings settings) {
            return (Bitmap)DecodeAndCall(s, settings, delegate(FIBITMAP b, bool MayDispose) {
                return Convert(b, ("true".Equals(settings["autorotate"], StringComparison.OrdinalIgnoreCase)));
            });
        }
        public delegate object DecodeCallback(FIBITMAP b, bool mayUnloadOriginal);
        /// <summary>
        /// Decodes the given stream, selects the correct page or frame, rotates it correctly (if autorotate=true), then executes the callback, then cleans up.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="settings"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static object DecodeAndCall(Stream s, ResizeSettings settings, DecodeCallback callback){
            if (!FreeImageAPI.FreeImage.IsAvailable()) return null;

            FREE_IMAGE_LOAD_FLAGS flags = FREE_IMAGE_LOAD_FLAGS.DEFAULT;
            bool autorotate = ("true".Equals(settings["autorotate"], StringComparison.OrdinalIgnoreCase));
            if (autorotate) flags |= FREE_IMAGE_LOAD_FLAGS.JPEG_EXIFROTATE;

            int page = 0;
            if (!string.IsNullOrEmpty(settings["page"]) && !int.TryParse(settings["page"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out page))
                page = 0;

            int frame = 0;
            if (!string.IsNullOrEmpty(settings["frame"]) && !int.TryParse(settings["frame"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out frame))
                frame = 0;

            if (page == 0 && frame != 0) page = frame; 

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (page > 1) {
                FREE_IMAGE_FORMAT fmt = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
                FIMULTIBITMAP mb = FreeImage.OpenMultiBitmapFromStream(s, ref fmt, flags);
                //Prevent asking for a non-existent page
                int pages = FreeImage.GetPageCount(mb);
                if (page > pages) page = pages; 
                try {
                    if (mb.IsNull) return null;
                    FIBITMAP bPage = FreeImage.LockPage(mb, page - 1);
                    if (bPage.IsNull) return null;
                    try {
                        sw.Stop();
                        return callback(bPage, false);
                    } finally {
                        FreeImage.UnlockPage(mb, bPage, false);
                    }

                } finally {
                    if (!mb.IsNull) FreeImage.CloseMultiBitmapEx(ref mb, FREE_IMAGE_SAVE_FLAGS.DEFAULT);
                }

            } else {
                FIBITMAP original = FIBITMAP.Zero;
                try {
                    original = FreeImage.LoadFromStream(s, flags);
                    sw.Stop();
                    if (original.IsNull) return null;
                    return callback(original, true);
                } finally {
                    if (!original.IsNull) FreeImage.UnloadEx(ref original);
                }
            }
        }

        private Bitmap Convert(FIBITMAP fi, bool removeRotateFlag) {
            Bitmap b = FreeImage.GetBitmap(fi);
            if (removeRotateFlag) try { b.RemovePropertyItem(0x0112); } catch { }
            return b;
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }
    }
}
