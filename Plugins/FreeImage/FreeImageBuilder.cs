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
using ImageResizer.Configuration;
using System.Web.Hosting;

namespace ImageResizer.Plugins.FreeImageBuilder {
    /// <summary>
    /// Provides an alternate resizing pipeline that never touches GDI. Only supports width/maxwidth/height/maxheight/scale/marginWidth/paddingWidth/fi.scale settings. Only operates on requests specifying builder=freeimage
    /// </summary>
    public class FreeImageBuilderPlugin :BuilderExtension, IPlugin, IIssueProvider {

        /// <summary>
        /// Creates a new instance of FreeImageBuilderPlugin
        /// </summary>
        public FreeImageBuilderPlugin(){
        }

        Config c;
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
            
        }

        /// <summary>
        /// Adds alternate pipeline based on FreeImage. Invoked by &amp;builder=freeimage. 
        /// This method doesn't handle job.DisposeSource or job.DesposeDest or settings filtering, that's handled by ImageBuilder.
        /// All the bitmap processing is handled by buildFiBitmap, this method handles all the I/O
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected override RequestedAction BuildJob(ImageJob job) {
            if (!"freeimage".Equals(job.Settings["builder"])) return RequestedAction.None;
            if (!FreeImageAPI.FreeImage.IsAvailable()) return RequestedAction.None;

            //StringBuilder log = new StringBuilder();

            //FreeImageAPI.FreeImageEngine.Message += (delegate(FREE_IMAGE_FORMAT fmt, string msg) {
            //    log.AppendLine(msg);
            //});

            // Variables
            Stream s = null;
            bool disposeStream = !(job.Source is Stream);
            long originalPosition = 0;
            bool restoreStreamPosition = false;

            //Get a Stream instance for the job
            string path;
            s = c.CurrentImageBuilder.GetStreamFromSource(job.Source, job.Settings, ref disposeStream, out path, out restoreStreamPosition);
            if (s == null) return RequestedAction.None;
            if (job.ResetSourceStream) restoreStreamPosition = true;
            job.SourcePathData = path;

            //Save the original stream positione
            originalPosition = (restoreStreamPosition) ? s.Position : -1;
            try {
                //What is our destination format
                IEncoder managedEncoder = c.Plugins.GetEncoder(job.Settings, job.SourcePathData); //Use the existing pipeline to parse the querystring
                //FREE_IMAGE_FORMAT destFormat = FreeImage.GetFIFFromMime(managedEncoder.MimeType); //Use the resulting mime-type to determine the output format.
                //This prevents us from supporting output formats that don't already have registered encoders. Good, right?

                bool supportsTransparency = managedEncoder.SupportsTransparency;


                return (RequestedAction)FreeImageDecoder.FreeImageDecoderPlugin.DecodeAndCall(s, job.Settings, delegate(ref FIBITMAP original, bool mayUnloadOriginal) {
                    FIBITMAP b = FIBITMAP.Zero;
                    try {
                        //Do all the bitmap stuff in another method
                        b = buildFiBitmap(ref original, job, supportsTransparency, mayUnloadOriginal);
                        if (b.IsNull) return RequestedAction.None;

                        // Try to save the bitmap
                        if (job.Dest is string || job.Dest is Stream) {
                            FreeImageEncoderPlugin e = new FreeImageEncoderPlugin(job.Settings, path);
                            if (job.Dest is string) {
                                //Make physical and resolve variable references all at the same time.
                                job.FinalPath = job.ResolveTemplatedPath(job.Dest as string,
                                    delegate(string var) {
                                        if ("width".Equals(var, StringComparison.OrdinalIgnoreCase)) return FreeImage.GetWidth(b).ToString();
                                        if ("height".Equals(var, StringComparison.OrdinalIgnoreCase)) return FreeImage.GetHeight(b).ToString();
                                        if ("ext".Equals(var, StringComparison.OrdinalIgnoreCase)) return e.Extension;
                                        return null;
                                    });
                                //If requested, auto-create the parent directory(ies)
                                if (job.CreateParentDirectory) {
                                    string dirName = Path.GetDirectoryName(job.FinalPath);
                                    if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
                                }
                                if (!FreeImage.Save(e.Format, b, job.FinalPath, e.EncodingOptions)) return RequestedAction.None;
                            } else if (job.Dest is Stream) {
                                if (!FreeImage.SaveToStream(b, (Stream)job.Dest, e.Format, e.EncodingOptions)) return RequestedAction.None;
                            }
                        } else if (job.Dest == typeof(Bitmap)) {
                            job.Result = FreeImage.GetBitmap(b);
                        } else return RequestedAction.None;
                        return RequestedAction.Cancel;
                    } finally {
                        if (!b.IsNull && b != original) FreeImage.UnloadEx(ref b);
                    }

                });
            } finally {
                if (s != null && restoreStreamPosition && s.CanSeek) s.Seek(originalPosition, SeekOrigin.Begin);
                if (disposeStream) s.Dispose();
            }

        }
   

        /// <summary>
        /// Builds an FIBitmap from the stream and job.Settings 
        /// </summary>
        /// <param name="original"></param>
        /// <param name="supportsTransparency"></param>
        /// <param name="mayUnloadOriginal"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        protected FIBITMAP buildFiBitmap(ref FIBITMAP original, ImageJob job, bool supportsTransparency, bool mayUnloadOriginal) {

            ResizeSettings settings = job.Settings;
            if (original.IsNull) return FIBITMAP.Zero;
            FIBITMAP final = FIBITMAP.Zero;

            //Find the image size
            Size orig = new Size((int)FreeImage.GetWidth(original), (int)FreeImage.GetHeight(original));

            //Calculate the new size of the image and the canvas.
            ImageState state = new ImageState(settings, orig, true);
            state.Job = job;
            c.CurrentImageBuilder.Process(state);
            RectangleF imageDest = PolygonMath.GetBoundingBox(state.layout["image"]);

            if (imageDest.Width != orig.Width || imageDest.Height != orig.Height) {
                //Rescale
                bool temp;
                final = FreeImage.Rescale(original, (int)imageDest.Width, (int)imageDest.Height, FreeImageScalingPlugin.ParseResizeAlgorithm(settings["fi.scale"], FREE_IMAGE_FILTER.FILTER_BOX, out temp));
                if (mayUnloadOriginal) FreeImage.UnloadEx(ref original);
                if (final.IsNull) return FIBITMAP.Zero;
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
                var old = final;
                //Extend canvas
                final = FreeImage.EnlargeCanvas<RGBQUAD>(old,
                            (int)outsideImage.Left, (int)outsideImage.Top, (int)outsideImage.Right, (int)outsideImage.Bottom,
                            bgcolor.Color != Color.Transparent ? new Nullable<RGBQUAD>(bgcolor) : null,
                            FREE_IMAGE_COLOR_OPTIONS.FICO_RGBA);
                if (old == original) {
                    if (mayUnloadOriginal) {
                        FreeImage.UnloadEx(ref original);
                        old = original;
                    }
                } else {
                    FreeImage.UnloadEx(ref old); //'old' has the original value of 'final', which we allocated.
                }
                if (final.IsNull) return FIBITMAP.Zero;
            }

            return final;

        }


        /// <summary>
        /// Returns the issue "The FreeImage library is not available! All FreeImage plugins will be disabled" if the FreeImage library is not available.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }
    }
}
