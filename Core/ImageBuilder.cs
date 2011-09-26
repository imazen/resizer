/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Drawing;
using System.IO;
using System.Web.Hosting;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Reflection;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Util;
using ImageResizer.Configuration;
using ImageResizer.Plugins;

namespace ImageResizer
{
    /// <summary>
    /// Provides methods for generating resized images, and for reading and writing them to disk.
    /// Use ImageBuilder.Current to get the current instance (as configured in the application configuration), or use ImageBuilder.Current.Create() to control which extensions are used.
    /// </summary>
    public class ImageBuilder : AbstractImageProcessor, IQuerystringPlugin, IFileExtensionPlugin
    {
        protected ImageBuilder() { }
        /// <summary>
        /// Handles the encoder selection and provision proccess.
        /// </summary>
        protected IEncoderProvider _encoderProvider = null;
        /// <summary>
        /// Handles the encoder selection and provision proccess.
        /// </summary>
        public IEncoderProvider EncoderProvider { get { return _encoderProvider; } }

        /// <summary>
        /// Returns a shared instance of ImageBuilder or a subclass, equivalent to  Config.Current.CurrentImageBuilder
        /// </summary>
        /// <returns></returns>
        public static ImageBuilder Current {get{ return Config.Current.CurrentImageBuilder; }}
        /// <summary>
        /// Creates a new ImageBuilder instance with no extensions.
        /// </summary>
        public ImageBuilder(IEncoderProvider encoderProvider): base() {
                this._encoderProvider = encoderProvider;
        }

        /// <summary>
        /// Create a new instance of ImageBuilder using the specified extensions and encoder provider. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        public ImageBuilder(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider):base(extensions){
            this._encoderProvider = encoderProvider;
        }

        
        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public virtual ImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer) {
            return new ImageBuilder(extensions,writer);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public virtual ImageBuilder Copy(){
            return new ImageBuilder(this.exts,this._encoderProvider);
        }


        /// <summary>
        /// Allows Bitmap Build(object) to wrap void Build(object,object) easily.
        /// </summary>
        protected class BitmapHolder {
            public BitmapHolder() { }
            public Bitmap bitmap;
        }

        /// <summary>
        /// Loads a Bitmap from the specified source. If a filename is available, it will be attached to bitmap.Tag in a BitmapTag instance. The Bitmap.Tag.Path value may be a virtual, relative, UNC, windows, or unix path. 
        /// Does not dispose 'source' if it is a Stream or Image instance - that's the responsibility of the calling code.
        /// </summary>
        /// <param name="source">May  be an instance of string, VirtualFile, IVirtualFile IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.  If passed an Image instance, the image will be cloned, which will cause metadata, indexed state, and any additional frames to be lost. Accepts physical paths and application relative paths. (C:\... and ~/path) </param>
        /// <param name="settings">Will ignore ICC profile if ?ignoreicc=true.</param>
        /// <returns>A Bitmap. The .Tag property will include a BitmapTag instance. If .Tag.Source is not null, remember to dispose it when you dispose the Bitmap.</returns>
        public virtual Bitmap LoadImage(object source, ResizeSettings settings) {

            bool disposeStream = !(source is Stream);
            string path = null;

            //Fire PreLoadImage(source,settings)
            this.PreLoadImage(ref source, ref path, ref disposeStream,  ref settings);

            System.Drawing.Bitmap b = null;
            string loadFailureReasons = "File may be corrupted, empty, or may contain a PNG image with a single dimension greater than 65,535 pixels.";
            
 
            //App-relative path - converted to virtual path
            if (source is string) {
                path = source as string;
                //Convert app-relative paths to VirtualFile instances
                if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase)) {
                    //TODO: add support for Ivirtual files
                    source = HostingEnvironment.VirtualPathProvider.GetFile(PathUtils.ResolveAppRelative(path));
                }
            }

            //Bitmap
            if (source is Bitmap) return source as Bitmap;
            //Image
            if (source is System.Drawing.Image) 
                return new Bitmap((System.Drawing.Image)source); //Note, this clones just the raw bitmap data - doesn't copy attributes, bit depth, or anything.
            //IVirtualBitmapFile
            if (source is IVirtualBitmapFile) {
                b = ((IVirtualBitmapFile)source).GetBitmap();
                if (b.Tag == null) b.Tag = new BitmapTag(((IVirtualBitmapFile)source).VirtualPath);
                return b;
            }



            bool restoreStreamPosition = false;
            Stream s = null;
            path = null;
            //Stream
            if (source is Stream) s = (Stream)source;
            //VirtualFile
             else if (source is VirtualFile) {
                path = ((VirtualFile)source).VirtualPath;
                s = ((VirtualFile)source).Open();
            //IVirtualFile
            } else if (source is IVirtualFile) {
                path = ((IVirtualFile)source).VirtualPath;
                s = ((IVirtualFile)source).Open();
            //PhysicalPath
            } else if (source is string) {
                path = (string)source;
                s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            } else if (source is byte[]){
                s = new MemoryStream((byte[])source, false);
            } else {
                //For HttpPostedFile and HttpPostedFileBase - we must use reflection to support .NET 3.5 without losing 2.0 compat.
                PropertyInfo pname = source.GetType().GetProperty("FileName",typeof(string));
                PropertyInfo pstream = source.GetType().GetProperty("InputStream");

                if (pname != null && pstream != null) {
                    path = pname.GetValue(source, null) as string;
                    s = pstream.GetValue(source, null) as Stream;
                    disposeStream = false; //We never want to dispose the HttpPostedFile or HttpPostedFileBase streams..
                    restoreStreamPosition = true;
                }
                
                if (s == null) 
                    throw new ArgumentException("Paramater source may only be an instance of string, VirtualFile, IVirtualBitmapFile, HttpPostedFile, HttpPostedFileBase, Bitmap, Image, or Stream.", "source");
            }
            //Save the original stream position if it's an HttpPostedFile
            long originalPosition = (restoreStreamPosition) ? s.Position : - 1;

            try {
                try {
                    //First try DecodeStream
                    b = this.DecodeStream(s, settings, path);
                    //Let the fallbacks work. (Only happens when a plugin overrides DecodeStream and retuns null)
                    if (b == null) throw new ImageCorruptedException("Failed to decode image. Plugin made DecodeStream return null.", null);
                } catch (Exception e) {
                    //Start over - on error.
                    if (s.CanSeek && s.Position != 0)
                        s.Seek(0, SeekOrigin.Begin);
                    //If we can't seek back to the beginning of the stream, we can't hope to decode it.
                    else if (!s.CanSeek)
                        throw new ImageCorruptedException("Cannot attempt fallback decoding path on a non-seekable stream", e);

                    b = DecodeStreamFailed(s, settings, path);
                    if (b == null) throw e; //If none of the extensions loaded the image, throw the exception anyhow.
                }
            } catch (ArgumentException ae) {
                ae.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ae);
            } catch (ExternalException ee) {
                ee.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ee);
            } finally {
                //Now, we can't dispose the stream if Bitmap is still using it. 
                if (b != null && b.Tag != null && b.Tag is BitmapTag && ((BitmapTag)b.Tag).Source == s) {
                    //And, it looks like Bitmap is still using it.
                    s = null;
                }
                //Dispose the stream if we opened it. If someone passed it to us, they're responsible.
                if (s != null && disposeStream) { s.Dispose(); s = null; }

                //Restore the stream position if we were given an HttpPostedFile instance
                if (originalPosition > -1 && s != null && s.CanSeek) s.Position = originalPosition;

                //Make sure the bitmap is tagged with its path. DecodeStream usually handles this, only relevant for extension decoders.
                if (b != null && b.Tag == null && path != null) b.Tag = new BitmapTag(path);

            }
            return b;            
        }

        /// <summary>
        /// Decodes the stream into a bitmap instance. As of 3.0.7, now ensures the stream can safely be closed after the method returns.
        /// May copy the stream. The copied stream will be in b.Tag.Source. Does not close or dispose any streams.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="settings"></param>
        /// <param name="optionalPath"></param>
        /// <returns></returns>
        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            Bitmap b = base.DecodeStream(s, settings, optionalPath);
            if (b != null && b.Tag == null) b.Tag = new BitmapTag(optionalPath); //Assume Bitmap wasn't used.
            if (b != null) return b;

            bool useICM = true;
            if (settings != null && "true".Equals(settings["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;

            //NDJ - May 24, 2011 - Copying stream into memory so the original can be closed safely.
            MemoryStream ms = StreamUtils.CopyStream(s);
            b = new Bitmap(ms, useICM); 
            b.Tag = new BitmapTag(optionalPath, ms); //May 25, 2011: Storing a ref to the MemorySteam so it won't accidentally be garbage collected.
            return b;
        }


        /// <summary>
        /// Resizes and processes the specified source image and returns a bitmap of the result.
        /// Note! 
        /// This method assumes that transparency will be supported in the final output format, and therefore does not apply a matte color. Use &amp;bgcolor to specify a background color
        /// if you use this method with a non-transparent format such as Jpeg.
        /// If passed a source Stream, Bitmap, or Image instance, it will be disposed after use. Use disposeSource=False to disable that behavior. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        public virtual Bitmap Build(object source, ResizeSettings settings) {
            return Build(source, settings, true);
        }
        /// <summary>
        /// Resizes and processes the specified source image and returns a bitmap of the result.
        /// Note! 
        /// This method assumes that transparency will be supported in the final output format, and therefore does not apply a matte color. Use &amp;bgcolor to specify a background color
        /// if you use this method with a non-transparent format such as Jpeg.
        /// 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        /// <param name="disposeSource">If false, 'source' will not be disposed. </param>
       
        public virtual Bitmap Build(object source, ResizeSettings settings, bool disposeSource) {
            BitmapHolder bh = new BitmapHolder();
            Build(source, bh, settings, disposeSource);
            return bh.bitmap;
        }

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination.
        /// If passed a source Stream, Bitmap, or Image instance, it will be disposed after use. Use disposeSource=False to disable that behavior. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        public virtual void Build(object source, object dest, ResizeSettings settings) {
            Build(source, dest, settings, true);
        }


        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination. 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        /// <param name="disposeSource">True to dispose 'source' after use. False to leave intact.</param>
        public virtual void Build(object source, object dest, ResizeSettings settings, bool disposeSource) {
            Build(source, dest, settings, disposeSource, false);
        }

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination. 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// If passed a path destination, the physical path of the written file will be returned.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        /// <param name="disposeSource">True to dispose 'source' after use. False to leave intact.</param>
        /// <param name="addFileExtension">If true, will add the correct file extension to 'dest' if it is a string. </param>
        public virtual string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            ResizeSettings s = new ResizeSettings(settings);
            Bitmap b = null; 
            try {
                //Load image
                b = LoadImage(source, settings);

                
                //Fire PreAcquireStream(ref dest, settings)
                this.PreAcquireStream(ref dest, settings);
                
                //Write to Physical file
                if (dest is string) {
                    string destPath = dest as string;
                    //Convert app-relative paths
                    if (destPath.StartsWith("~", StringComparison.OrdinalIgnoreCase)) destPath = HostingEnvironment.MapPath(destPath);

                    //Add the file extension if specified.
                    if (addFileExtension) {
                        IEncoder e = this.EncoderProvider.GetEncoder(settings, b);
                        if (e != null) destPath += "." + e.Extension;
                    }

                    System.IO.FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                    using (fs) {
                        buildToStream(b, fs, s);
                    }
                    return destPath;
                //Write to Unknown stream
                } else if (dest is Stream) {
                    buildToStream(b, (Stream)dest, s);
                //Write to BitmapHolder
                } else if (dest is BitmapHolder) {
                    ((BitmapHolder)dest).bitmap = buildToBitmap(b, s, true);
                } else throw new ArgumentException("Paramater dest may only be a string, Stream, or BitmapHolder", "dest");

            } finally {
                //Get the source bitmap's underlying stream (may differ from 'source')
                Stream underlyingStream = null;
                if (b != null && b.Tag != null && b.Tag is BitmapTag) underlyingStream = ((BitmapTag)b.Tag).Source;

                //Close the source bitamp's underlying stream unless it is the same stream (EDIT: or bitmap) we were passed.
                if (b != source && underlyingStream != source && underlyingStream != null) underlyingStream.Dispose();

                //Dispose the bitmap unless we were passed it. We check for 'null' in case an ImageCorruptedException occured. 
                if (b != null && b != source) b.Dispose();

                //Follow the disposeSource boolean.
                if (disposeSource && source is IDisposable) ((IDisposable)source).Dispose();

            }
            return null;
        }

        /// <summary>
        /// Override this when you need to override the behavior of image encoding and/or Bitmap processing
        /// Not for external use. Does NOT dispose of 'source' or 'source's underlying stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        protected override RequestedAction buildToStream(Bitmap source, Stream dest, ResizeSettings settings) {
            if (base.buildToStream(source, dest, settings) == RequestedAction.Cancel) return RequestedAction.None;

            IEncoder e = this.EncoderProvider.GetEncoder(settings,source);
            if (e == null) throw new ImageProcessingException("No image encoder was found for this request.");
            using (Bitmap b = buildToBitmap(source, settings,e.SupportsTransparency)) {//Determines output format, includes code for saving in a variety of formats.
                //Save to stream
                e.Write(b, dest);
            }
            return RequestedAction.None;
        }

        /// <summary>
        /// Override this when you need to override the behavior of Bitmap processing. 
        /// Not for external use. Does NOT dispose of 'source' or 'source's underlying stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        /// <param name="transparencySupported">True if the output method will support transparency. If false, the image should be provided a matte color</param>
        /// <returns></returns>
        protected override Bitmap buildToBitmap(Bitmap source, ResizeSettings settings, bool transparencySupported) {
            Bitmap b = base.buildToBitmap(source,settings,transparencySupported);
            if (b != null) return b; //Allow extensions to replace the method wholesale.

            using (ImageState state = new ImageState(settings, source.Size, transparencySupported)) {
                state.sourceBitmap = source;

                //Generic processing of ImageState instances.
                Process(state);

                //Save a reference to return
                b = state.destBitmap;
                state.destBitmap = null; //So it doesn't get disposed yet

                //Don't dispose the source bitmap either, just the graphics object.
                state.sourceBitmap = null;
            }
            return b;
        }

        /// <summary>
        /// Processes an ImageState instance. Used by Build, GetFinalSize, and TranslatePoint. 
        /// Can be overriden by a plugin with the OnProcess method
        /// </summary>
        /// <param name="s"></param>
        public virtual void Process(ImageState s){
            if (OnProcess(s) == RequestedAction.Cancel) return;
            PrepareSourceBitmap(s);  // We select the page/frame and flip the source bitmap here
            PostPrepareSourceBitmap(s);
            Layout(s); //Layout everything
            PrepareDestinationBitmap(s); //Create a bitmap and graphics object based on s.destSize
            Render(s); //Render using the graphics object
            ProcessFinalBitmap(s); //Perform the final flipping of the bitmap.
            EndProcess(s);
        }
        /// <summary>
        /// Process.3: Handles the layout phase of Processing
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction Layout(ImageState s) {
            if (base.Layout(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            FlipExistingPoints(s);
            LayoutImage(s);
            PostLayoutImage(s);
            LayoutPadding(s);
            PostLayoutPadding(s);
            LayoutBorder(s);
            PostLayoutBorder(s);
            LayoutEffects(s);
            PostLayoutEffects(s);
            LayoutMargin(s);
            PostLayoutMargin(s);
            LayoutRotate(s);
            PostLayoutRotate(s);
            LayoutNormalize(s);
            PostLayoutNormalize(s);
            LayoutRound(s);
            PostLayoutRound(s);
            EndLayout(s);
            return RequestedAction.None;
        }
        /// <summary>
        /// Handles the rendering phase of processing
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction Render(ImageState s) {
            if (base.Render(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            RenderBackground(s);
            PostRenderBackground(s);
            RenderEffects(s);
            PostRenderEffects(s);
            RenderPadding(s);
            PostRenderPadding(s);
            CreateImageAttribues(s);
            PostCreateImageAttributes(s);
            RenderImage(s);
            PostRenderImage(s);
            RenderBorder(s);
            PostRenderBorder(s);
            PreRenderOverlays(s);
            RenderOverlays(s);
            PreFlushChanges(s);
            FlushChanges(s);
            PostFlushChanges(s);
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.1 Switches the bitmap to the correct frame or page, and applies source flipping commands
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected override RequestedAction PrepareSourceBitmap(ImageState s)
        {
            if (base.PrepareSourceBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel ; //Call extensions

            if (s.sourceBitmap == null) return RequestedAction.None ; //Nothing to do if there is no bitmap

            Bitmap src = s.sourceBitmap;
            ResizeSettings q = s.settings;

            int page = 0;
            if (!string.IsNullOrEmpty(q["page"]) && !int.TryParse(q["page"], out page))
                page = 0;

            int frame = 0;
            if (!string.IsNullOrEmpty(q["frame"]) && !int.TryParse(q["frame"], out frame))
                frame = 0;

            //So users can use 1-based numbers
            page--; frame--;

            //Support page selection in a .tiff document.
            try {
                //Stay on the last page/frame if out of bounds
                if (page > 0 && page >= src.GetFrameCount(FrameDimension.Page)) page = src.GetFrameCount(FrameDimension.Page) - 1;
                if (frame > 0 && frame >= src.GetFrameCount(FrameDimension.Time)) frame = src.GetFrameCount(FrameDimension.Time) - 1;

                //Select the right page/frame if specified
                if (page > 0) src.SelectActiveFrame(FrameDimension.Page, page);
                if (frame > 0) src.SelectActiveFrame(FrameDimension.Time, frame);
            } catch (ExternalException) { } //When somebody tries &frame or &page on a single-frame image
            


            //Flipping has to be done on the original - it can't be done as part of the DrawImage or later, after the borders are drawn.

            if (s.sourceBitmap != null && s.settings.SourceFlip != RotateFlipType.RotateNoneFlipNone) {
                s.sourceBitmap.RotateFlip(s.settings.SourceFlip);
                s.originalSize = s.sourceBitmap.Size;
            }
            return RequestedAction.None;
        }


        protected override RequestedAction LayoutPadding(ImageState s) {
            if (base.LayoutPadding(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //We need to add padding
            if (!s.settings.Padding.IsEmpty) {
                s.layout.AddRing("padding",  s.settings.Padding);
            }
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutMargin(ImageState s) {
            if (base.LayoutMargin(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //We need to add padding
            if (!s.settings.Margin.IsEmpty) {
                s.layout.AddRing("margin", s.settings.Margin);
            }
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutBorder(ImageState s) {
            if (base.LayoutBorder(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //And borders
            if (!s.settings.Border.IsEmpty) {
                s.layout.AddRing("border", s.settings.Border);
            }
            return RequestedAction.None;
        }

        

        protected override RequestedAction LayoutRound(ImageState s)
        {
            if (base.LayoutRound(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Todo, round points here.
            //s.layout.Round();
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutRotate(ImageState s)
        {
            if (base.LayoutRotate(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Now, rotate all rings.
            s.layout.Rotate(s.settings.Rotate, new PointF(0, 0));
            return RequestedAction.None;
        }
        protected override RequestedAction LayoutNormalize(ImageState s)
        {
            if (base.LayoutNormalize(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Normalize all the rings
            s.layout.Normalize(new PointF(0, 0));
            return RequestedAction.None;
        }


        protected override RequestedAction EndLayout(ImageState s)
        {
            if (base.EndLayout(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Calculates a bounding box around all the rings in the layout, then rounds that size. Creates a 1x1 pixel destSize value at minimum.
            s.destSize = PolygonMath.RoundPoints(s.layout.GetBoundingBox().Size);
            s.destSize = new Size((int)Math.Max(1, s.destSize.Width), (int)Math.Max(1, s.destSize.Height));
            return RequestedAction.None;
        }

        /// <summary>
        /// Creates a bitmap of s.destSize dimensions, intializes a graphics object for it, and configures all the default settings.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction  PrepareDestinationBitmap(ImageState s)
        {
            if (base.PrepareDestinationBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel;

             if (s.sourceBitmap == null) return RequestedAction.None;

            //Create new bitmap using calculated size. 
            s.destBitmap = new Bitmap(s.destSize.Width,s.destSize.Height, PixelFormat.Format32bppArgb);
            

            //Create graphics handle
            Graphics g = s.destGraphics = Graphics.FromImage(s.destBitmap);

            //High quality everthing
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.CompositingMode = CompositingMode.SourceOver;
            return RequestedAction.None;
        }

        /// <summary>
        /// Sets the background color if needed or requested
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction RenderBackground(ImageState s) {
            if (base.RenderBackground(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            Graphics g = s.destGraphics;

            //If the image doesn't support transparency, we need to fill the background color now.
            Color background = s.settings.BackgroundColor;

            if (background == Color.Transparent)
                //Only set the bgcolor if the image isn't taking the whole area.
                if (!s.supportsTransparency)// && (!PolygonMath.GetBoundingBox(s.layout["image"]).Equals(s.layout.GetBoundingBox()))
                    background = Color.White;
            
            //Fill background
            if (background != Color.Transparent) //This causes increased aliasing at the edges - i.e., a faint white border that is even more pronounced than usual.
                g.Clear(background); //Does this work for Color.Transparent? -- 
            return RequestedAction.None;
        }


        protected override RequestedAction RenderPadding(ImageState s) {
            if (base.RenderPadding(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            if (!s.layout.ContainsRing("padding")) return RequestedAction.None;

            Color paddingColor = s.settings.PaddingColor;
            //Inherit color
            if (paddingColor.Equals(Color.Transparent)) paddingColor = s.settings.BackgroundColor;
            //Draw padding around image if needed.
            if (!paddingColor.Equals(s.settings.BackgroundColor) && paddingColor != Color.Transparent)
                s.destGraphics.FillPolygon(new SolidBrush(paddingColor), s.layout["padding"]);

            return RequestedAction.None;
        }

        protected override RequestedAction CreateImageAttribues(ImageState s) {
            if (base.CreateImageAttribues(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            if (s.copyAttibutes == null) s.copyAttibutes = new ImageAttributes();
            return RequestedAction.None;
        }

        protected override RequestedAction RenderImage(ImageState s) {
            if (base.RenderImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            s.copyAttibutes.SetWrapMode(WrapMode.TileFlipXY);
            s.destGraphics.DrawImage(s.sourceBitmap, PolygonMath.getParallelogram(s.layout["image"]), s.copyRect, GraphicsUnit.Pixel, s.copyAttibutes);

            return RequestedAction.None;
        }

        protected override RequestedAction RenderBorder(ImageState s) {
            if (base.RenderBorder(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            //Draw border
            if (s.settings.Border.IsEmpty) return RequestedAction.None;

            if (s.settings.Border.All == double.NaN) throw new NotImplementedException("Separate border widths have not yet been implemented");

            Pen p = new Pen(s.settings.BorderColor, (float)s.settings.Border.All);
            p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center; //PenAlignment.Center is the only supported mode.
            p.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            s.destGraphics.DrawPolygon(p, PolygonMath.InflatePoly(s.layout["border"], (float)(s.settings.Border.All / -2.0))); //I hope GDI rounds the same way as .NET.. Otherwise there may be an off-by-one error..

            return RequestedAction.None;
        }


        protected override RequestedAction FlushChanges(ImageState s) {
            if (base.FlushChanges(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            if (s.destGraphics == null) return RequestedAction.None;
            //Commit changes.
            s.destGraphics.Flush(FlushIntention.Flush);
            s.destGraphics.Dispose();
            s.destGraphics = null;
            return RequestedAction.None;
        }

        protected override RequestedAction ProcessFinalBitmap(ImageState s) {
            if (base.ProcessFinalBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //The default if we are doing a simulation

            s.finalSize = s.destSize;
            //Skip this when we are doing simulations
            if (s.destBitmap == null) return RequestedAction.None;

            //The last flipping.
            if (s.settings.Flip != RotateFlipType.RotateNoneFlipNone)
                s.destBitmap.RotateFlip(s.settings.Flip);

            s.finalSize = s.destBitmap.Size;
            return RequestedAction.None;
        }
       

        /// <summary>
        /// Doesn't support flipping. Translate a point on the original bitmap to a point on the new bitmap. If the original point no longer exists, returns Empty
        /// </summary>
        /// <returns></returns>
        public virtual PointF[] TranslatePoints(PointF[] sourcePoints, Size originalSize, ResizeSettings q) {
            ImageState s = new ImageState(q, originalSize, true);
            s.layout.AddInvisiblePolygon("points", sourcePoints);
            Process(s);
            return s.layout["points"];
        }
         


        /// <summary>
        /// Gets the final size of an image
        /// </summary>
        /// <returns></returns>
        public virtual Size GetFinalSize(Size originalSize, ResizeSettings q)
        {
            ImageState s = new ImageState(q, originalSize, true);
            Process(s);
            return s.finalSize;
        }

  




        /// <summary>
        /// Populates copyRect, as well as Rings image and imageArea. Translates and scales any existing rings as if they existed on the original bitmap.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction LayoutImage(ImageState s) {
            if (base.LayoutImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Use the crop size if present.
            s.copyRect = new RectangleF(new PointF(0,0),s.originalSize);
            if (s.settings.CropMode == CropMode.Custom) {
                s.copyRect = s.settings.getCustomCropSourceRect(s.originalSize);
                if (s.copyRect.Size.IsEmpty) throw new Exception("You must specify a custom crop rectange if crop=custom");
            }

            //Aspect ratio of the image
            double imageRatio = s.copySize.Width / s.copySize.Height;

            //Was any dimension specified?
            bool dimensionSpecified = s.settings.Width != -1 || s.settings.Height != -1 || s.settings.MaxHeight != -1 || s.settings.MaxWidth != -1;

            //The target size for the image 
            SizeF targetSize = new SizeF(-1, -1);
            SizeF areaSize = new SizeF(-1, -1);
            if (!dimensionSpecified) {
                areaSize = targetSize = s.copySize; //No dimension - use original size if possible - within web.config bounds.
            } else {
                //A dimension was specified. 
                //We first calculate the largest size the image can be under the width/height/maxwidth/maxheight restrictions.
                //- pretending stretch=fill and scale=both

                //Temp vars - results stored in targetSize and areaSize
                double width = s.settings.Width;
                double height = s.settings.Height;
                double maxwidth = s.settings.MaxWidth;
                double maxheight = s.settings.MaxHeight;

                //Eliminate cases where both a value and a max value are specified: use the smaller value for the width/height 
                if (maxwidth > 0 && width > 0) {
                    width = Math.Min(maxwidth, width);
                    maxwidth = -1;
                }
                if (maxheight > 0 && height > 0) {
                    height = Math.Min(maxheight, height);
                    maxheight = -1;
                }
                //Do sizing logic

                if (width > 0 || height > 0) //In this case, either (or both) width and height were specified 
                {
                    //If only one is specified, calculate the other from 
                    if (width > 0) {
                        if (height < 0) height = (width / imageRatio);
                        if (maxheight > 0 && height > maxheight) height = maxheight; //Crop to maxheight value

                    } else if (height > 0) {
                        if (width < 0) width = (height * imageRatio);
                        if (maxheight > 0 && height > maxheight) height = maxheight; //Crop to maxheight value
                    }
                    //Store result
                    targetSize = new SizeF((float)width, (float)height);
                } else //In this case, only maxwidth and/or maxheight were specified.
                {
                    //Calculate the missing max bounds (if one *is* missing), using aspect ratio of the image
                    if (maxheight > 0 && maxwidth <= 0)
                        maxwidth = maxheight * imageRatio;
                    else if (maxwidth > 0 && maxheight <= 0)
                        maxheight = maxwidth / imageRatio;

                    //Scale image coords to fit.
                    targetSize = PolygonMath.ScaleInside(s.copySize, new SizeF((float)maxwidth, (float)maxheight));

                }

                //We now have targetSize. targetSize will only be a different aspect ratio if both 'width' and 'height' are specified.

                //This will be the area size also
                areaSize = targetSize;

                //Now do scale=proportionally check. Set targetSize=imageSize and make it fit within areaSize using ScaleInside.
                if (s.settings.Stretch == StretchMode.Proportionally) {
                    targetSize = PolygonMath.ScaleInside(s.copySize, areaSize);
                }

                //Now do upscale/downscale checks. If they take effect, set targetSize to imageSize
                if (s.settings.Scale == ScaleMode.DownscaleOnly) {
                    if (PolygonMath.FitsInside(s.copySize, targetSize)) {
                        //The image is smaller or equal to its target polygon. Use original image coordinates instead.
                        areaSize = targetSize = s.copySize;
                    }
                } else if (s.settings.Scale == ScaleMode.UpscaleOnly) {
                    if (!PolygonMath.FitsInside(s.copySize, targetSize)) {
                        //The image is larger than its target. Use original image coordintes instead
                        areaSize = targetSize = s.copySize;

                    }
                } else if (s.settings.Scale == ScaleMode.UpscaleCanvas) {
                    //Same as downscaleonly, except areaSize isn't changed.
                    if (PolygonMath.FitsInside(s.copySize, targetSize)) {
                        //The image is smaller or equal to its target polygon. Use original image coordinates instead.

                        targetSize = s.copySize;
                    }

                }

            }

            //June 3: Ensure no dimension of targetSize or areaSize is less than 1px;
            areaSize.Width = Math.Max(1, areaSize.Width);
            areaSize.Height = Math.Max(1, areaSize.Height);
  
            
            //Autocrop
            if (s.settings.CropMode == CropMode.Auto && s.settings.Stretch == StretchMode.Proportionally) {
                //Determine the size of the area we are copying
                SizeF sourceSize = PolygonMath.ScaleInside(areaSize, s.originalSize);
                //Center the portion we are copying within the original bitmap
                s.copyRect = new RectangleF((s.originalSize.Width - sourceSize.Width) / 2, (s.originalSize.Height - sourceSize.Height) / 2, sourceSize.Width, sourceSize.Height);
                //Restore targetSize to match areaSize //Warning - crop always forces scale=both.
                targetSize = areaSize;
            }

            //May 12: require max dimension and round values to minimize rounding differences later.
            areaSize.Width = Math.Max(1, (float)Math.Round(areaSize.Width));
            areaSize.Height = Math.Max(1, (float)Math.Round(areaSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));
            

            //Translate and scale all existing rings
            s.layout.Shift(new RectangleF(0, 0, s.originalSize.Width, s.originalSize.Height), new RectangleF(new Point(0, 0), targetSize));

            s.layout.AddRing("image", PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), targetSize)));

            s.layout.AddRing("imageArea",PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), areaSize)));

            //Center imageArea around 'image'
            s.layout["imageArea"] = PolygonMath.CenterInside(s.layout["imageArea"], s.layout["image"]);

            return RequestedAction.None;
        }



        private readonly string[] _supportedFileExtensions = new string[]
             {"bmp","gif","exif","png","tif","tiff","tff","jpg","jpeg", "jpe","jif","jfif","jfi"};

        /// <summary>
        /// Returns a list of the file extensions ImageBuilder can load by default. Plugins can implement IFileExtensionPlugin to add new ones.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedFileExtensions() {
            return _supportedFileExtensions;
        }
       
        private readonly string[] _supportedQuerystringKeys = new string[]{
                    "format", "thumbnail", "maxwidth", "maxheight",
                "width", "height",
                "scale", "stretch", "crop", "page", "bgcolor",
                "rotate", "flip", "sourceFlip", "borderWidth",
                "borderColor", "paddingWidth", "paddingColor",
                "ignoreicc", "frame", "useresizingpipeline", 
                "cache", "process", "margin"};

        /// <summary>
        /// Returns a list of the querystring commands ImageBuilder can parse by default. Plugins can implement IQuerystringPlugin to add new ones.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return _supportedQuerystringKeys;
        }
    }
}
