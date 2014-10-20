using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration.Issues;
using ImageResizer.Util;
using ImageResizer.Plugins.Basic;
using System.IO;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using System.Drawing;
using ImageResizer.Plugins.Wic.InteropServices;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Wic;
using System.Web.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ImageResizer.Plugins.WicEncoder;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.WicBuilder {
    public class WicBuilderPlugin : BuilderExtension, IPlugin, IIssueProvider, IFileExtensionPlugin {

        public WicBuilderPlugin() {
        }

        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


        /// <summary>
        /// Adds alternate pipeline based on WIC. Invoked by &builder=wic. 
        /// This method doesn't handle job.DisposeSource or job.DesposeDest or settings filtering, that's handled by ImageBuilder.
        /// Handles all the work for turning 'source' into a byte[]/long pair.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected override RequestedAction BuildJob(ImageJob job) {
            if (!"wic".Equals(job.Settings["builder"])) return RequestedAction.None;

            //Convert the source stream to a byte[] array and length.
            byte[] data = null;
            long lData = 0;


            //This step gets a Stream instance, copies it to a MemoryStream, then accesses the underlying buffer to get the byte[] and length we need.
            Stream s = null;
            bool disposeStream = !(job.Source is Stream);
            long originalPosition = 0;
            bool restoreStreamPosition = false;
            try {
                //Get a Stream instance for the job
                string path;
                s = c.CurrentImageBuilder.GetStreamFromSource(job.Source, job.Settings, ref disposeStream, out path, out restoreStreamPosition);
                if (s == null) return RequestedAction.None; //We don't support the source object!
                if (job.ResetSourceStream) restoreStreamPosition = true;
                job.SourcePathData = path;

                //Save the original stream positione
                originalPosition = (restoreStreamPosition) ? s.Position : -1;

                data = s.CopyOrReturnBuffer( out lData,false, 0x1000);
            } finally {
                if (s != null && restoreStreamPosition && s.CanSeek) s.Seek(originalPosition, SeekOrigin.Begin);
                if (disposeStream && s != null) s.Dispose();
            }

            //Ok, now we have our byte[] and length. 

            //Let's find out if transparency is supported.
            IEncoder managedEncoder = c.Plugins.GetEncoder(job.Settings, job.SourcePathData);

            bool supportsTransparency = managedEncoder.SupportsTransparency;

            RequestedAction result = BuildJobWic(data, lData, job, supportsTransparency);
            GC.KeepAlive(data);
            return result;
        }

        /// <summary>
        /// Decodes the image in byte[] data, performs the image proccessing, and encodes it to job.Dest
        /// </summary>
        /// <param name="data">The buffer containing the encoded image file</param>
        /// <param name="lData">The number of bytes to read</param>
        /// <param name="job"></param>
        /// <param name="supportsTransparency"></param>
        /// <returns></returns>
        protected virtual RequestedAction BuildJobWic(byte[] data, long lData, ImageJob job, bool supportsTransparency) {

            ResizeSettings settings = job.Settings; ResizeSettings q = settings;
            string path = job.SourcePathData;

            //A list of COM objects to destroy
            List<object> com = new List<object>();
            try {
                //Create the factory
                IWICComponentFactory factory  = (IWICComponentFactory)new WICImagingFactory();
                com.Add(factory);

                //Wrap the byte[] with a IWICStream instance
                var streamWrapper = factory.CreateStream();
                streamWrapper.InitializeFromMemory(data, (uint)lData);
                com.Add(streamWrapper);

                var decoder = factory.CreateDecoderFromStream(streamWrapper, null,
                                                              WICDecodeOptions.WICDecodeMetadataCacheOnLoad);
                com.Add(decoder);

                //Figure out which frame to work with
                int frameIndex = 0;
                if (!string.IsNullOrEmpty(q["page"]) && !int.TryParse(q["page"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out frameIndex))
                    if (!string.IsNullOrEmpty(q["frame"]) && !int.TryParse(q["frame"], NumberStyles.Number, NumberFormatInfo.InvariantInfo, out frameIndex))
                        frameIndex = 0;

                //So users can use 1-based numbers
                frameIndex--;

                if (frameIndex > 0) {
                    int frameCount = (int)decoder.GetFrameCount(); //Don't let the user go past the end.
                    if (frameIndex >= frameCount) frameIndex = frameCount - 1;
                }

                IWICBitmapFrameDecode frame = decoder.GetFrame((uint)Math.Max(0,frameIndex));
                com.Add(frame);

                

                WICBitmapInterpolationMode interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeFant;
                if ("nearest".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeNearestNeighbor;
                if ("bicubic".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeCubic;
                if ("linear".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeLinear;
                if ("nearestneighbor".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeLinear;
                
                //Find the original image size
                uint origWidth, origHeight;
                frame.GetSize(out origWidth,out origHeight);
                Size orig = new Size((int)origWidth,(int)origHeight);

                Guid pixelFormat;
                frame.GetPixelFormat(out pixelFormat);
                //Calculate the new size of the image and the canvas.
                ImageState state = new ImageState(settings, orig, true);
                state.Job = job;
                c.CurrentImageBuilder.Process(state);


                Rectangle imageDest = PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(state.layout["image"]));

              
                IWICBitmapSource imageData = frame; 
                //Are we cropping? then daisy-chain a clipper
                if (state.copyRect.Left != 0 || state.copyRect.Top != 0 || state.copyRect.Width != state.originalSize.Width || state.copyRect.Height != state.originalSize.Height) {

                    //Cropping is absurdly slow... 4x slower than resizing!
                    //Cropping after resizing (unintuitively) is faster.
                    if (imageDest.Width != state.originalSize.Width || imageDest.Height != state.originalSize.Height) {
                        double sx = (double)imageDest.Width / (double)state.copyRect.Width;
                        double sy = (double)imageDest.Height / (double)state.copyRect.Height;
                        uint uncroppedDestWidth = (uint)Math.Round(sx * state.originalSize.Width);
                        uint uncroppedDestHeight = (uint)Math.Round(sy * state.originalSize.Height);
                        
                        var scaler = factory.CreateBitmapScaler();
                        scaler.Initialize(imageData, uncroppedDestWidth, uncroppedDestHeight, interpolationMode);
                        com.Add(scaler);

                        //TODO: cropping is not consistent with GDI.
                        var clipper = factory.CreateBitmapClipper();
                        clipper.Initialize(scaler, new WICRect { 
                            X = (int)Math.Floor((double)state.copyRect.X * sx),
                            Y = (int)Math.Floor((double)state.copyRect.Y * sy), 
                            Width = imageDest.Width, 
                            Height = imageDest.Height
                        });
                        com.Add(clipper);
                        imageData = clipper;

                    } else {
                        var clipper = factory.CreateBitmapClipper();
                        clipper.Initialize(imageData, new WICRect { X = (int)state.copyRect.X, Y = (int)state.copyRect.Y, Width = (int)state.copyRect.Width, Height = (int)state.copyRect.Height });
                        com.Add(clipper);
                        imageData = clipper;
                    }
                    //If we're scaling but not cropping.
                }else if (imageDest.Width != state.originalSize.Width || imageDest.Height != state.originalSize.Height) {
                    var scaler = factory.CreateBitmapScaler();
                    scaler.Initialize(imageData, (uint)imageDest.Width, (uint)imageDest.Height, interpolationMode);
                    com.Add(scaler);
                    imageData = scaler;
                }

                

                //Are we padding? Then we have to do an intermediate write.
                if (state.destSize.Width != imageDest.Width || state.destSize.Height != imageDest.Height){
                    byte[] bgcolor = ConversionUtils.ConvertColor(job.Settings.BackgroundColor, pixelFormat);

                    for (int i = 0; i < bgcolor.Length; i++) bgcolor[i] = 255; //White

                    var padder = new WicBitmapPadder(imageData, imageDest.X, imageDest.Y, state.destSize.Width - (imageDest.X + imageDest.Width), state.destSize.Height - (imageDest.Y + imageDest.Height), bgcolor, null);
                    imageData = padder;
                }

                //Now encode imageData and be done with it...
                return Encode(factory, imageData, imageDest.Size, job);
            } finally {
                //Manually cleanup all the com reference counts, aggressively
                while (com.Count > 0) {
                    Marshal.ReleaseComObject(com[com.Count - 1]); //In reverse order, so no item is ever deleted out from under another.
                    com.RemoveAt(com.Count - 1);
                }
            }
        }

        protected virtual RequestedAction Encode(IWICComponentFactory factory, IWICBitmapSource data, Size imageSize, ImageJob job) {
            WicEncoderPlugin encoder = new WicEncoderPlugin(job.Settings, job.SourcePathData); 
 
            //Create the IStream/MemoryStream
            var outputStream = new MemoryIStream();


            encoder.EncodeToStream(factory, data, imageSize, outputStream);

            object dest = job.Dest;
            // Try to save the bitmap
            if (dest is string) {
                //Make physical and resolve variable references all at the same time.
                job.FinalPath = job.ResolveTemplatedPath(job.Dest as string,
                    delegate(string var) {
                        if ("ext".Equals(var, StringComparison.OrdinalIgnoreCase)) return encoder.Extension;
                        if ("width".Equals(var, StringComparison.OrdinalIgnoreCase)) return imageSize.Width.ToString();
                        if ("height".Equals(var, StringComparison.OrdinalIgnoreCase)) return imageSize.Height.ToString();
                        return null;
                    });
                //If requested, auto-create the parent directory(ies)
                if (job.CreateParentDirectory) {
                    string dirName = Path.GetDirectoryName(job.FinalPath);
                    if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
                }

                using (FileStream fs = new FileStream(job.FinalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                    outputStream.WriteTo(fs);
                }
            } else if (dest is Stream) {
                outputStream.WriteTo((Stream)dest);
            } else return RequestedAction.None;

            return RequestedAction.Cancel;
        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (Environment.OSVersion.Version.Major < 6) issues.Add(new Issue("WIC should only be used Windows 7, Server 2008, or higher to prevent stability issues.", IssueSeverity.Critical));
            return issues;
        }

        public IEnumerable<string> GetSupportedFileExtensions() {
            return new string[]{"hdp","jxr","wdp"};//Plus the ones already listed by ImageBuilder
            //We can enumerate available codecs through factory.CreateComponentEnumerator and factory.CreateComponentInfo...
            //But those codecs only give us Author, CLSID, FriendlyName, SpecVersion, VendorGUID, and Version. No list of supported file extensions.
            //So maybe it's best to make that user-specified?
        }
    }
}
