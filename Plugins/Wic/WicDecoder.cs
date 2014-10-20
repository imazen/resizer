using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Issues;
using System.Drawing;
using System.IO;
using ImageResizer.Util;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using ImageResizer.Plugins.Wic;
using System.Runtime.InteropServices;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.WicDecoder {
    /// <summary>
    /// Note: This decoder produces Bitmaps that require special disposal instructions.
    /// While ImageBuilder handles this, your code may not. It's best not to directly call LoadImage with &decoder=wic. 
    /// This decoder returns Bitmap instances with .Tag set to a GCHandle instance. You must call ((GCHandle)b.Tag).Free() after disposing the Bitmap.
    /// </summary>
    public class WicDecoderPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider {

        public WicDecoderPlugin() {
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
            return new string[] { }; //Same as default
        }

        public override System.Drawing.Bitmap DecodeStream(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            if (!"wic".Equals(settings["decoder"], StringComparison.OrdinalIgnoreCase)) return null;

            return Decode(s, settings);
        }

        public override System.Drawing.Bitmap DecodeStreamFailed(System.IO.Stream s, ResizeSettings settings, string optionalPath) {
            try {
                return Decode(s, settings);
            } catch {
                return null;
            }
        }

        public Bitmap Decode(Stream s, ResizeSettings settings) {
            //Get the underlying byte array
            long lData = 0;
            byte[] data = s.CopyOrReturnBuffer(out lData,false, 0x1000);


            var factory = (IWICComponentFactory)new WICImagingFactory();

            //Decode the image with WIC
            IWICBitmapSource frame;
            var streamWrapper = factory.CreateStream();
            streamWrapper.InitializeFromMemory(data, (uint)lData);
            var decoder = factory.CreateDecoderFromStream(streamWrapper, null,
                                                          WICDecodeOptions.WICDecodeMetadataCacheOnLoad);

            try {

                //Figure out which frame to work with
                int frameIndex = 0;
                if (!string.IsNullOrEmpty(settings["page"]) && !int.TryParse(settings["page"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out frameIndex))
                    if (!string.IsNullOrEmpty(settings["frame"]) && !int.TryParse(settings["frame"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out frameIndex))
                        frameIndex = 0;

                //So users can use 1-based numbers
                frameIndex--;

                if (frameIndex > 0) {
                    int frameCount = (int)decoder.GetFrameCount(); //Don't let the user go past the end.
                    if (frameIndex >= frameCount) frameIndex = frameCount - 1;
                }
                frameIndex = Math.Max(0, frameIndex);

                if ("true".Equals(settings["usepreview"]))
                    frame = decoder.GetPreview();
                else if ("true".Equals(settings["usethumb"]))
                    frame = decoder.GetThumbnail();
                else 
                    frame = decoder.GetFrame((uint)frameIndex);
                try {
                    Bitmap b = ConversionUtils.FromWic(frame);
                    GC.KeepAlive(data);
                    return b;
                } finally {
                    Marshal.ReleaseComObject(frame);
                }
            } finally {
                Marshal.ReleaseComObject(decoder);
                Marshal.ReleaseComObject(factory);
            }
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (Environment.OSVersion.Version.Major < 6) issues.Add(new Issue("WIC should only be used on Windows 7, Server 2008, or higher.", IssueSeverity.Critical));
            return issues;
        }
    }
}
