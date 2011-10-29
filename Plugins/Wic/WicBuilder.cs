using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration.Issues;
using ImageResizer.Util;
using ImageResizer.Plugins.Basic;
using System.IO;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using System.Drawing;
using WicResize.InteropServices;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.WicBuilder {
    public class WicBuilderPlugin : ImageBuilder, IPlugin, IIssueProvider {

        public WicBuilderPlugin() {
        }
        /// <summary>
        /// Creates a new FreeImageBuilder instance with no extensions.
        /// </summary>
        public WicBuilderPlugin(IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider)
            : base(encoderProvider, virtualFileProvider) {
        }

        /// <summary>
        /// Create a new instance of FreeImageBuilder using the specified extensions and encoder provider. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        public WicBuilderPlugin(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider)
            : base(extensions, encoderProvider, virtualFileProvider) {
        }


        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public override ImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer, IVirtualImageProvider virtualFileProvider) {
            return new WicBuilderPlugin(extensions, writer, virtualFileProvider);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public override ImageBuilder Copy() {
            return new WicBuilderPlugin(this.exts, this.EncoderProvider, this.VirtualFileProvider);
        }

        public IPlugin Install(Configuration.Config c) {
            c.UpgradeImageBuilder(new WicBuilderPlugin(c.CurrentImageBuilder.EncoderProvider, VirtualFileProvider));
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            return false; // We can't uninstall this.
        }


        public override string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            if (!(source is Image) && (dest is String || dest is Stream) && !addFileExtension) {
                if (BuildWic(source, dest, settings, disposeSource)) return dest as string;
            }
            return base.Build(source, dest, settings, disposeSource, addFileExtension);
        }

        protected virtual bool BuildWic(object source, object dest, ResizeSettings settings, bool disposeSource) {
            if (!"wic".Equals(settings["builder"])) return false;

            string path = null;
            bool disposeSourceDuringLoad = !(source is Stream);
            bool restoreStreamPosition = false;

            //Convert the source object to a stream
            Stream s = this.GetStreamFromSource(ref source, ref path, ref disposeSourceDuringLoad, ref restoreStreamPosition, ref settings);
            //Make it a memory stream
            if (!(s is MemoryStream)) {
                s = StreamUtils.CopyStream((Stream)s);
            }
            if (disposeSourceDuringLoad && source is IDisposable) ((IDisposable)source).Dispose();

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
            frame = decoder.GetFrame(0); //TODO add support for page and frame

            try {
                //What is our destination format
                IEncoder managedEncoder = EncoderProvider.GetEncoder(settings, path); //Use the existing pipeline to parse the querystring 

                //Load the WIC the container encoder
                Guid guidEncoder = Consts.GUID_ContainerFormatJpeg;
                if (managedEncoder.MimeType.Equals("image/jpeg")) guidEncoder = Consts.GUID_ContainerFormatJpeg;
                if (managedEncoder.MimeType.Equals("image/png")) guidEncoder = Consts.GUID_ContainerFormatPng;
                if (managedEncoder.MimeType.Equals("image/gif")) guidEncoder = Consts.GUID_ContainerFormatGif;
                var encoder = factory.CreateEncoder(guidEncoder, null);

                //Configure it, prepare output stream
                var outputStream = new MemoryIStream();
                encoder.Initialize(outputStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
                // Prepare output frame
                IWICBitmapFrameEncode outputFrame;
                var arg = new IPropertyBag2[1];
                encoder.CreateNewFrame(out outputFrame, arg);
                var propBag = arg[0];
                var propertyBagOption = new PROPBAG2[1];
                propertyBagOption[0].pstrName = "ImageQuality";
                propBag.Write(1, propertyBagOption, new object[] { ((float)settings.Quality) / 100 });
                outputFrame.Initialize(propBag);



                //Find the original image size
                uint origWidth, origHeight;
                frame.GetSize(out origWidth,out origHeight);
                Size orig = new Size((int)origWidth,(int)origHeight);

                //Calculate the new size of the image and the canvas.
                ImageState state = new ImageState(settings, orig, true);
                Layout(state);

                bool supportsTransparency = true;

                RectangleF imageDest = PolygonMath.GetBoundingBox(state.layout["image"]);

                //Set destination frame size
                outputFrame.SetSize((uint)state.destSize.Width, (uint)state.destSize.Height);

                IWICBitmapSource imageData = frame; 
                //Are we cropping? then daisy-chain a clipper
                if (state.copyRect.Left != 0 || state.copyRect.Top != 0 || state.copyRect.Width != state.originalSize.Width || state.copyRect.Height != state.originalSize.Height) {
                    var clipper = factory.CreateBitmapClipper();
                    clipper.Initialize(imageData, new WICRect { X = (int)state.copyRect.X, Y = (int)state.copyRect.Y, Width = (int)state.copyRect.Width, Height = (int)state.copyRect.Height });
                    imageData = clipper;
                }

                //Are we scaling? Then daisy-chain a scaler
                if (imageDest.Width != state.originalSize.Width || imageDest.Height != state.originalSize.Height) {
                    WICBitmapInterpolationMode interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeFant;
                    if ("nearest".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeNearestNeighbor;
                    if ("bicubic".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeCubic;
                    if ("linear".Equals(settings["w.filter"], StringComparison.OrdinalIgnoreCase)) interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeLinear;
                    var scaler = factory.CreateBitmapScaler();
                    scaler.Initialize(imageData, (uint)imageDest.Width, (uint)imageDest.Height, interpolationMode);
                    imageData = scaler;
                }

                //Are we padding? Then we have to do an intermediate write.
                if (state.destSize.Width != imageDest.Width || state.destSize.Height != imageDest.Height){
                    //IWICBitmap canvas = factory.CreateBitmap((uint)state.destSize.Width, (uint)state.destSize.Height, Consts.GUID_WICPixelFormat32bppRGBA, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad);
                    //TODO: Write unsafe code to draw padding, etc.
                }
                //TODO: no support for background color or padding
               
                // Write the data to the output frame
                outputFrame.WriteSource(imageData, new WICRect { X = 0, Y = 0, Width = (int)imageDest.Width, Height = (int)imageDest.Height });
                outputFrame.Commit();
                encoder.Commit();


                // Try to save the bitmap
                if (dest is string || dest is Stream) {
                    if (dest is string) {
                        using (FileStream fs = new FileStream((string)dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)){
                            outputStream.WriteTo(fs);
                        }
                    } else if (dest is Stream) {
                        outputStream.WriteTo((Stream)dest);
                    }
                } 
                


            } finally {
                //Nothing to do really...
            }
            return true;
        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (Environment.OSVersion.Version.Major < 6) issues.Add(new Issue("WIC should only be used Windows 7, Server 2008, or higher.", IssueSeverity.Critical));
            return issues;
        }
    }
}
