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
    /// Provides methods for generating resizied images, and for reading and writing them to disk.
    /// Use ImageManager.Create to get the default instance, or use ImageManager.Instance.Create() to control which extensions are used.
    /// </summary>
    public class ImageBuilder : AbstractImageProcessor, IQuerystringPlugin
    {
        /// <summary>
        /// Handles the encoder selection and provision proccess.
        /// </summary>
        protected IEncoderProvider encoderProvider = null;
        /// <summary>
        /// Handles the encoder selection and provision proccess.
        /// </summary>
        public IEncoderProvider EncoderProvider { get { return encoderProvider; } }

        /// <summary>
        /// Returns a shared instance of ImageBuilder or a subclass, equivalent to  Config.Current.CurrentImageBuilder
        /// </summary>
        /// <returns></returns>
        public static ImageBuilder Current {get{ return Config.Current.CurrentImageBuilder; }}
        /// <summary>
        /// Creates a new ImageBuilder instance with no extensions.
        /// </summary>
        public ImageBuilder(IEncoderProvider encoderProvider): base() {
                this.encoderProvider = encoderProvider;
        }

        /// <summary>
        /// Create a new instance of ImageBuilder using the specified extensions and encoder provider. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        public ImageBuilder(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider):base(extensions){
            this.encoderProvider = encoderProvider;
        }

        
        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public virtual ImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer) {
            return new ImageBuilder(extensions,writer);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public virtual ImageBuilder Copy(){
            return new ImageBuilder(this.exts,this.encoderProvider);
        }


        /// <summary>
        /// Allows Bitmap Build(object) to wrap void Build(object,object) easily.
        /// </summary>
        protected class BitmapHolder {
            public BitmapHolder() { }
            public Bitmap bitmap;
        }
        /// <summary>
        /// Loads a Bitmap from the specified source. If a filename is available, it will be attached to bitmap.Tag. The Bitmap.tag may be virtual, relative, UNC, windows, or unix path. 
        /// Accepts physical paths and application relative paths. (C:\... and ~/path) 
        /// </summary>
        /// <param name="source">May  be an instance of string, VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream</param>
        /// <param name="settings">Will ignore ICC profile if ?ignoreicc=true.</param>
        /// <returns></returns>
        public virtual Bitmap LoadImage(object source, ResizeSettings settings) {

            //Fire PreLoadImage(source,settings)
            this.PreLoadImage(ref source, settings);

            System.Drawing.Bitmap b = null;

            string loadFailureReasons = "File may be corrupted, empty, or may contain a PNG image with a single dimension greater than 65,535 pixels.";

            bool useICM = true;
            if (settings != null && "true".Equals(settings["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;

            //Bitmap
            if (source is Bitmap) return source as Bitmap;
            //Image
            if (source is System.Drawing.Image) return new Bitmap((System.Drawing.Image)source);
            //IVirtualBitmapFile
            if (source is IVirtualBitmapFile) {
                b = ((IVirtualBitmapFile)source).GetBitmap();
                b.Tag = ((IVirtualBitmapFile)source).VirtualPath;
                return b;
            }
            
            //String, physical path
            if (source is string) {
                string path = source as string;
                //Convert app-relative paths
                if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase)) path = HostingEnvironment.MapPath(path);
                try {
                    try {
                        b = new System.Drawing.Bitmap(path, useICM);
                    //Pass FileNotFound Exceptions along
                    } catch (FileNotFoundException notFound){
                        throw notFound;
                    } catch (Exception e) {
                        b = LoadImageFailed(e, path, useICM);
                        if (b == null) throw e; //If none of the extensions loaded the image, throw the exception anyhow.
                    }
                } catch (ArgumentException ae) {
                    ae.Data.Add("path", path);
                    throw new ImageCorruptedException(loadFailureReasons, ae);
                } catch (ExternalException ee) {
                    ee.Data.Add("path", path);
                    throw new ImageCorruptedException(loadFailureReasons, ee);
                }
                b.Tag = path;
                return b;
            }
            //VirtualFile
            //HttpPostedFile
            //Stream
            if (source is VirtualFile || source is HttpPostedFile || source is Stream) {
                Stream s = null;
                string path = null;
                if (source is Stream) s = (Stream)source;
                else if (source is HttpPostedFile) {
                    path = ((HttpPostedFile)source).FileName;
                    s = ((HttpPostedFile)source).InputStream;
                }
                if (source is VirtualFile) {
                    path = ((VirtualFile)source).VirtualPath;
                    s = ((VirtualFile)source).Open();
                }

                using (s) {
                    try {
                        try {
                            b = new System.Drawing.Bitmap(s, useICM);
                        } catch (Exception e) {
                            b = LoadImageFailed(e, s, useICM);
                            if (b == null) throw e; //If none of the extensions loaded the image, throw the exception anyhow.
                        }
                    } catch (ArgumentException ae) {
                        ae.Data.Add("path", path);
                        throw new ImageCorruptedException(loadFailureReasons, ae);
                    } catch (ExternalException ee) {
                        ee.Data.Add("path", path);
                        throw new ImageCorruptedException(loadFailureReasons, ee);
                    }
                    b.Tag = path;
                    return b;
                }
            }
            throw new ArgumentException("Paramater source may only be an instance of string, VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.", "source");
        }



        /// <summary>
        /// Resizes and processes the specified source image and returns a bitmap of the result.
        /// This method assumes that transparency will be supported in the final output format, and therefore does not apply a matte color. Use &amp;bgcolor to specify a background color
        /// if you use this method with a non-transparent format such as Jpeg.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        public virtual Bitmap Build(object source, ResizeSettings settings) {
            BitmapHolder bh = new BitmapHolder();
            Build(source, bh, settings);
            return bh.bitmap;
        }

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        public virtual void Build(object source, object dest, ResizeSettings settings) {
            ResizeSettings s = new ResizeSettings(settings);

            Bitmap b = null; 
            try {
                
                //Load image
                b = LoadImage(source,settings);

                
                //Fire PreAcquireStream(ref dest, settings)
                this.PreAcquireStream(ref dest, settings);
                
                //Write to Physical file
                if (dest is string) {
                    
                    System.IO.FileStream fs = new FileStream((string)dest, FileMode.Create, FileAccess.Write);
                    using (fs) {
                        buildToStream(b, fs, s);
                    }
                //Write to Unknown stream
                } else if (dest is Stream) {
                    buildToStream(b, (Stream)dest, s);
                //Write to BitmapHolder
                } else if (dest is BitmapHolder) {
                    ((BitmapHolder)dest).bitmap = buildToBitmap(b, s, true);
                    b = null; //So the b.Dispose doesn't happen
                } else throw new ArgumentException("Paramater dest may only be a string, Stream, or BitmapHolder", "dest");

            } finally {
                if (b != null) b.Dispose();
            }

        }
        /// <summary>
        /// Override this when you need to override the behavior of image encoding and/or Bitmap processing
        /// Not for external use.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        protected virtual void buildToStream(Bitmap source, Stream dest, ResizeSettings settings) {
            IEncoder e = Config.Current.Plugins.GetEncoder(source, settings);
            if (e == null) throw new ImageProcessingException("No image encoder was found for this request.");
            using (Bitmap b = buildToBitmap(source, settings,e.SupportsTransparency)) {//Determines output format, includes code for saving in a variety of formats.
                //Save to stream
                e.Write(b, dest);
            }
        }

        /// <summary>
        /// Override this when you need to override the behavior of Bitmap processing. 
        /// Not for external use.
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
                if (!s.supportsTransparency && !PolygonMath.GetBoundingBox(s.layout["image"]).Equals(s.layout.GetBoundingBox()))
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

            if (s.settings.Border.All <= 0) throw new NotImplementedException("Separate border widths have not yet been implemented");

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
            targetSize.Width = Math.Max(1, targetSize.Width);
            targetSize.Height = Math.Max(1, targetSize.Height);
            
            
            
            //Autocrop
            if (s.settings.CropMode == CropMode.Auto && s.settings.Stretch == StretchMode.Proportionally) {
                //Determine the size of the area we are copying
                SizeF sourceSize = PolygonMath.ScaleInside(areaSize, s.originalSize);
                //Center the portion we are copying within the original bitmap
                s.copyRect = new RectangleF((s.originalSize.Width - sourceSize.Width) / 2, (s.originalSize.Height - sourceSize.Height) / 2, sourceSize.Width, sourceSize.Height);
                //Restore targetSize to match areaSize //Warning - crop always forces scale=both.
                targetSize = areaSize;
            }

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

        public virtual IEnumerable<string> GetSupportedFileExtensions() {
            return _supportedFileExtensions;
        }
       
        private readonly string[] _supportedQuerystringKeys = new string[]{
                    "format", "thumbnail", "maxwidth", "maxheight",
                "width", "height",
                "scale", "stretch", "crop", "page", "bgcolor",
                "rotate", "flip", "sourceFlip", "borderWidth",
                "borderColor", "paddingWidth", "paddingColor", "ignoreicc", "frame", "useresizingpipeline"};
    
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return _supportedQuerystringKeys;
        }
    }
}
