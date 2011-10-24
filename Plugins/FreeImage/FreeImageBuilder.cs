using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration.Issues;
using FreeImageAPI;
using System.Drawing;
using ImageResizer.Util;
using System.Drawing.Imaging;
using ImageResizer.Plugins.Basic;
using System.IO;
using ImageResizer.Plugins.FreeImageEncoder;
using ImageResizer.Plugins.FreeImageScaling;

namespace ImageResizer.Plugins.FreeImageBuilder {
    public class FreeImageBuilderPlugin :ImageBuilder, IPlugin, IIssueProvider {

        public FreeImageBuilderPlugin(){
        }
         /// <summary>
        /// Creates a new FreeImageBuilder instance with no extensions.
        /// </summary>
        public FreeImageBuilderPlugin(IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider)
            : base(encoderProvider,virtualFileProvider) {
        }

        /// <summary>
        /// Create a new instance of FreeImageBuilder using the specified extensions and encoder provider. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        public FreeImageBuilderPlugin(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider)
            : base(extensions, encoderProvider,virtualFileProvider) {
        }

        
        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public override ImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer, IVirtualImageProvider virtualFileProvider) {
            return new FreeImageBuilderPlugin(extensions, writer,virtualFileProvider);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public override ImageBuilder Copy() {
            return new FreeImageBuilderPlugin(this.exts, this.EncoderProvider, this.VirtualFileProvider);
        }

        public IPlugin Install(Configuration.Config c) {
            c.UpgradeImageBuilder(new FreeImageBuilderPlugin(c.CurrentImageBuilder.EncoderProvider,VirtualFileProvider));
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            return false; // We can't uninstall this.
        }


        public override string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            if ((source is String || source is Stream) && (dest is String || dest is Stream || dest is BitmapHolder) && !addFileExtension) {
                if (BuildUnmanaged(source, dest, settings)) return dest as string;
            }
            return base.Build(source, dest, settings, disposeSource, addFileExtension);
        }

        protected virtual bool BuildUnmanaged(object source, object dest, ResizeSettings settings) {
            if (!FreeImageAPI.FreeImage.IsAvailable()) return false;

            if (!"freeimage".Equals(settings["builder"])) return false;

            // Load the example bitmap.
            FIBITMAP original = FIBITMAP.Zero;
            FIBITMAP final = FIBITMAP.Zero;

            if (source is String)
                original = FreeImage.LoadEx((String)source); //Supports all kinds of input formats.
            else if (source is Stream)
                original = FreeImage.LoadFromStream((Stream)source);
            if (original.IsNull) return false;
            try{
                //What is our destination format
                IEncoder managedEncoder = EncoderProvider.GetEncoder(settings, source); //Use the existing pipeline to parse the querystring
                FREE_IMAGE_FORMAT destFormat = FreeImage.GetFIFFromMime(managedEncoder.MimeType); //Use the resulting mime-type to determine the output format.
                //This prevents us from supporting output formats that don't already have registered encoders. Good, right?
                
                

                //Find the image size
                Size orig = new Size( (int)FreeImage.GetWidth(original),  (int)FreeImage.GetHeight(original));

                //Calculate the new size of the image and the canvas.
                ImageState state = new ImageState(settings, orig, true);
                Layout(state);

                bool supportsTransparency = true;

                RectangleF imageDest = PolygonMath.GetBoundingBox(state.layout["image"]);

                if (imageDest.Width != orig.Width || imageDest.Height != orig.Height) {
                    //Rescale
                    bool temp;
                    final = FreeImage.Rescale(original, (int)imageDest.Width, (int)imageDest.Height, FreeImageScalingPlugin.ParseResizeAlgorithm(settings["fi.scale"], FREE_IMAGE_FILTER.FILTER_BOX, out temp));
                    FreeImage.UnloadEx(ref original);
                    if (final.IsNull) return false;
                } else {
                    final = original;
                }

                RGBQUAD bgcolor = default(RGBQUAD);
                bgcolor.Color = settings.BackgroundColor;
                if (settings.BackgroundColor == Color.Transparent && !supportsTransparency)
                    bgcolor.Color = Color.White;

                //If we need to leave padding, do so.
                BoxPadding outsideImage = new BoxPadding(imageDest.Left, imageDest.Top, state.destSize.Width - imageDest.Right, state.destSize.Height - imageDest.Bottom);

                if (outsideImage.All != 0) {
                    original = final;
                    //Extend canvas
                    final = FreeImage.EnlargeCanvas<RGBQUAD>(original,
                                (int)outsideImage.Left, (int)outsideImage.Top, (int)outsideImage.Right, (int)outsideImage.Bottom, 
                                bgcolor.Color != Color.Transparent ? new Nullable<RGBQUAD>(bgcolor) : null,
                                FREE_IMAGE_COLOR_OPTIONS.FICO_RGBA);
 
                    FreeImage.UnloadEx(ref original);
                    if (final.IsNull) return false;
                }

                // Try to save the bitmap
                if (dest is string || dest is Stream){
                    FreeImageEncoderPlugin e = new FreeImageEncoderPlugin(settings, source);
                    if (dest is string){
                        if (!FreeImage.Save(e.Format, final, (string)dest, e.EncodingOptions)) return false;
                    } else if (dest is Stream) {
                        if (!FreeImage.SaveToStream(final, (Stream)dest, e.Format,e.EncodingOptions)) return false;
                    }
                } else if (dest is BitmapHolder){
                    ((BitmapHolder)dest).bitmap = FreeImage.GetBitmap(final);
                }

            
            }finally{
                if (!original.IsNull) FreeImage.UnloadEx(ref original);
                if (!original.IsNull) FreeImage.UnloadEx(ref final);
            }
            return true;
        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }
    }
}
