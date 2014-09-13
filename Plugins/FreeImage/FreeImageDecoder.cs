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
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.FreeImageDecoder {
    /// <summary>
    /// Tone Mapping or reproduction algorithms available
    /// </summary>
    public enum ToneMappingAlgorithm {
        None,
        Reinhard,
        Drago,
        Fattal
    }

    /// <summary>
    /// FreeIMageDecoderPlugin to decode Free Image bitmap files
    /// </summary>
    public class FreeImageDecoderPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider, IQuerystringPlugin {

        /// <summary>
        /// Empty constructor creates an instance of the FreeImageDecoder Plugin
        /// </summary>
        public FreeImageDecoderPlugin() {
        }
        private static IEnumerable<string> supportedExts = null;

        /// <summary>
        /// Install the FreeImageDecoder plugin to the given config
        /// </summary>
        /// <param name="c">ImageResizer Configuration to install the plugin</param>
        /// <returns>FreeImageDecoder plugin that was installed</returns>
        public IPlugin Install(Configuration.Config c) {
            if (supportedExts == null && FreeImage.IsAvailable()) {
                supportedExts = BuildSupportedList();
            }
            c.Plugins.add_plugin(this);
            return this;
        }


        /// <summary>
        /// Builds a list of potential Free Image Formats
        /// </summary>
        /// <returns>Collection of known free image formats</returns>
        public IEnumerable<string> BuildSupportedList() {
            FREE_IMAGE_FORMAT[] formats = (FREE_IMAGE_FORMAT[])Enum.GetValues(typeof(FREE_IMAGE_FORMAT));
            List<string> extensions = new List<string>();
            foreach (FREE_IMAGE_FORMAT format in formats)
                if (format != FREE_IMAGE_FORMAT.FIF_UNKNOWN)
                    extensions.AddRange(FreeImage.GetFIFExtensionList(format).Split(','));
            return extensions;

        }

        /// <summary>
        /// Uninstalls the plugin from the given ImageResizer Configuration
        /// </summary>
        /// <param name="c">ImageResizer Configuration</param>
        /// <returns>true if plugin uninstalled</returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Gets an IEnumerable collection of supported file extensions 
        /// </summary>
        /// <returns>collection of supported file extensions</returns>
        public IEnumerable<string> GetSupportedFileExtensions() {
            if (supportedExts == null) return new string[] { };
            else return supportedExts;
        }

        /// <summary>
        /// Decodes a IO stream to extrace a Bitmap image
        /// </summary>
        /// <param name="s">IO stream of data</param>
        /// <param name="settings">ImageResizer settings to decode with</param>
        /// <param name="optionalPath">optional extension path</param>
        /// <returns>Decoded Bitmap</returns>
        public override System.Drawing.Bitmap DecodeStream(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            if (!"freeimage".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase)) return null;

            return Decode(s,  settings);
        }

        /// <summary>
        /// Try to decode a stream and if it can't be decoded return null
        /// </summary>
        /// <param name="s">IO stream of data</param>
        /// <param name="settings">ImageResizer Settings to decode with </param>
        /// <param name="optionalPath">option extension path</param>
        /// <returns>Decoded Bitmap or null</returns>
        public override System.Drawing.Bitmap DecodeStreamFailed(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            try {
                 return Decode(s,settings);
                 
            } catch (Exception){
                return null;
            }
        }

        /// <summary>
        /// Decode a Stream using the ImageResizer settings
        /// </summary>
        /// <param name="s">IO stream of the data</param>
        /// <param name="settings">ImageResizer settings to decode with</param>
        /// <returns>Decoded BItmap image</returns>
        public Bitmap Decode(Stream s, ResizeSettings settings) {
            return (Bitmap)DecodeAndCall(s, settings, delegate(ref FIBITMAP b, bool MayDispose) {
                 bool usethumb = ("true".Equals(settings["usepreview"], StringComparison.OrdinalIgnoreCase));
                bool autorotate = ("true".Equals(settings["autorotate"], StringComparison.OrdinalIgnoreCase));

                return Convert(b, autorotate && !usethumb); //because usepreview prevents autorotate from working at the freeimage level
            });
        }

        /// <summary>
        /// delegate object that can be used in a decode callback used to unload the original bitmap when it's done being used
        /// </summary>
        /// <param name="b">Free Image Bitmap</param>
        /// <param name="mayUnloadOriginal">option to unload original bitmap</param>
        /// <returns>delegate object</returns>
        public delegate object DecodeCallback(ref FIBITMAP b, bool mayUnloadOriginal);

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

            //If we're not tone-mapping the raw file, convert it for display
            if (!HasToneMappingCommands(settings)) flags |= FREE_IMAGE_LOAD_FLAGS.RAW_DISPLAY;

            bool usethumb = ("true".Equals(settings["usepreview"], StringComparison.OrdinalIgnoreCase));
            if (usethumb) flags |= FREE_IMAGE_LOAD_FLAGS.RAW_PREVIEW;

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
                        return ToneMap(ref bPage, false, settings, callback);
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
                    return ToneMap(ref original, true, settings, callback);
                } finally {
                    if (!original.IsNull) FreeImage.UnloadEx(ref original);
                }
            }
        }
        private static bool HasToneMappingCommands(ResizeSettings settings) {
            return false; //Tone mapping is disabled, not yet functional
            return settings.Get<ToneMappingAlgorithm>("fi.tonemap", ToneMappingAlgorithm.None) != ToneMappingAlgorithm.None;

        }

        private static object ToneMap(ref FIBITMAP b, bool mayUnloadOriginal, ResizeSettings settings, DecodeCallback callback) {
            return callback(ref b, mayUnloadOriginal);//Tone mapping is disabled, not yet functional

            FIBITMAP m = FIBITMAP.Zero;
            try {
                var alg = settings.Get<ToneMappingAlgorithm>("fi.tonemap", ToneMappingAlgorithm.None);
                if (alg == ToneMappingAlgorithm.Drago){
                    m = FreeImage.TmoDrago03(b, 2.2, 0);
                }else if (alg == ToneMappingAlgorithm.Reinhard){
                    m = FreeImage.TmoReinhard05(b, 0, 0);
                }else if (alg == ToneMappingAlgorithm.Fattal){
                    m = FreeImage.TmoFattal02(b, 0.5, 0.85);
                }else{
                    return callback(ref b, mayUnloadOriginal);
                }
                if (mayUnloadOriginal) FreeImage.UnloadEx(ref b);

                return callback(ref m, true);
            } finally {
                if (!m.IsNull) FreeImage.UnloadEx(ref m); 
            }
        }

        private Bitmap Convert(FIBITMAP fi, bool removeRotateFlag) {
            Bitmap b = FreeImage.GetBitmap(fi);
            if (removeRotateFlag) try { b.RemovePropertyItem(0x0112); } catch { }
            return b;
        }

        /// <summary>
        /// Collection of issues found using the FreeImage API
        /// </summary>
        /// <returns>IEnumberable collection of issues found</returns>
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }

        /// <summary>
        /// Gets a collection of supported query strings
        /// </summary>
        /// <returns>IEnumerable Collection of supported query strings</returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "usepreview", "autorotate", "page", "frame" };
        }
    }
}
