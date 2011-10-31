using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Issues;
using System.Drawing;
using System.IO;
using ImageResizer.Util;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using ImageResizer.Plugins.Wic;
using System.Runtime.InteropServices;

namespace ImageResizer.Plugins.WicDecoder {
    public class WicDecoderPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider {

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

            //Make it a memory stream
            if (!(s is MemoryStream)) {
                s = StreamUtils.CopyStream((Stream)s);
            }

            //Get the underlying byte array
            byte[] data = null;
            long lData = 0;
            try {
                data = ((MemoryStream)s).GetBuffer();
                lData = s.Length;
            } catch (UnauthorizedAccessException) {
                data = ((MemoryStream)s).ToArray();
                lData = data.Length;
            }


            var factory = (IWICComponentFactory)new WICImagingFactory();

            //Decode the image with WIC
            IWICBitmapFrameDecode frame;
            var streamWrapper = factory.CreateStream();
            streamWrapper.InitializeFromMemory(data, (uint)lData);
            var decoder = factory.CreateDecoderFromStream(streamWrapper, null,
                                                          WICDecodeOptions.WICDecodeMetadataCacheOnLoad);

            try {
                //TODO: add support for &frame= and &page=0
                frame = decoder.GetFrame(0);
                try {
                    return ConversionUtils.FromWic(frame);
                } finally {
                    Marshal.FinalReleaseComObject(frame);
                }
            } finally {
                Marshal.FinalReleaseComObject(decoder);
                Marshal.FinalReleaseComObject(factory);
            }
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (Environment.OSVersion.Version.Major < 6) issues.Add(new Issue("WIC should only be used Windows 7, Server 2008, or higher.", IssueSeverity.Critical));
            return issues;
        }
    }
}
