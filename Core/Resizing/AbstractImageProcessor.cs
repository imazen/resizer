using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace fbs.ImageResizer.Resizing {
    /// <summary>
    /// Provides a dual-purpose base class. Extensions can inherit and override certain methods.
    /// ImageBuilder inherits this method to utilize its extension invocation code. 
    /// Each method of AbstractImageProcessor loops through all extensions and executes the same method on each. Provides a sort of multiple-inheritance mechanisim.
    /// </summary>
    public class AbstractImageProcessor {

        public AbstractImageProcessor() {
            exts = new List<ImageBuilderExtension>();
        }
        /// <summary>
        /// Creates a new AbstractImageProcessor which will run the specified extensions with each method call.
        /// </summary>
        /// <param name="extensions"></param>
        public AbstractImageProcessor(IEnumerable<ImageBuilderExtension> extensions) {
            exts = new List<ImageBuilderExtension>(extensions != null ? extensions : new ImageBuilderExtension[] { }); 
        }

        /// <summary>
        /// It is best to only call this method on the same thread that created the instance, and only prior to other threads being able to access the instance.
        /// </summary>
        /// <param name="extension"></param>
        public virtual void AddExtension(ImageBuilderExtension extension) {
            lock (extsLock) {
                //Should clone extensions, then add to the new copy, then assign back the the value. 
                //Must be thread safe
                List<ImageBuilderExtension> newList = new List<ImageBuilderExtension>(exts);
                newList.Add(extension);
                exts = newList;
            }
        }


        /// <summary>
        /// Contains the set of extensions that are called for every method. 
        /// </summary>
        protected IEnumerable<ImageBuilderExtension> exts;
        private object extsLock = new object();

        /// <summary>
        /// Extend this to allow additional types of source objects to be accepted by transforming them into accepted types. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        protected virtual void PreLoadImage(ref object source, ResizeSettings settings) {
            foreach (AbstractImageProcessor p in exts) p.PreLoadImage(ref source, settings);
        }

        /// <summary>
        /// Extensions are executed until one extension returns a non-null value. 
        /// This is taken to mean that the error has been resolved.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="useICM"></param>
        protected virtual Bitmap LoadImageFailed(Exception e, string path, bool useICM) {
            foreach (AbstractImageProcessor p in exts) {
                Bitmap b = p.LoadImageFailed(e, path, useICM);
                if (b != null) return b;
            }
            return null;
        }

        /// <summary>
        /// Extensions are executed until one extension returns a non-null value. 
        /// This is taken to mean that the error has been resolved.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="useICM"></param>
        protected virtual Bitmap LoadImageFailed(Exception e, Stream s, bool useICM) {
            foreach (AbstractImageProcessor p in exts) {
                Bitmap b = p.LoadImageFailed(e, s, useICM);
                if (b != null) return b;
            }
            return null;
        }

        /// <summary>
        /// Extend this to allow additional types of destination objects to be accepted by transforming them into either a bitmap or a stream
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        protected virtual void PreAcquireStream(ref object dest, ResizeSettings settings) {
            foreach (AbstractImageProcessor p in exts) p.PreAcquireStream(ref dest, settings);
        }

        /// <summary>
        /// 1) Occurs before Proccessing begins. Can be used to add points to translate (for image maps), and also to modify the settings 
        /// </summary>
        /// <param name="s"></param>
        protected virtual void BeginProcess(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.BeginProcess(s);
        }

        /// <summary>
        /// 2) Switches the bitmap to the correct frame or page, and applies source flipping commands.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PrepareSourceBitmap(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PrepareSourceBitmap(s);
        }
        /// <summary>
        /// 3) Extend this to apply any pre-processing to the source bitmap that needs to occur before Layout begins
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostPrepareSourceBitmap(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostPrepareSourceBitmap(s);
        }

        /// <summary>
        /// 4) Layout is beginnging. This is the last point at which points to translate should be added.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void BeginLayout(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.BeginLayout(s);
        }

        /// <summary>
        /// 5) This is where the points in the layout are flipped the same way the source bitmap was flipped (unless their flags specify otherwise)
        /// </summary>
        /// <param name="s"></param>
        protected virtual void FlipExistingPoints(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.FlipExistingPoints(s);
        }

        /// <summary>
        /// 6) Rings image and imageArea are added to layout. 
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutImage(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutImage(s);
        }

        /// <summary>
        /// 7) Add rings here to insert them between the image area and the padding
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutImage(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutImage(s);
        }

        /// <summary>
        /// 8) Ring "padding" is added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutPadding(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutPadding(s);
        }

        /// <summary>
        /// 9) Add rings here to insert them between the padding and the border
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutPadding(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutPadding(s);
        }

        /// <summary>
        /// 10) Ring "border" is added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutBorder(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutBorder(s);
        }

        /// <summary>
        /// 11) Add rings here to insert them between the border and the effect rings
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutBorder(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutBorder(s);
        }

        /// <summary>
        /// 12) Effects such as 'shadow' are added here.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutEffects(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutEffects(s);
        }

        /// <summary>
        /// 13) Add rings here to insert them between the effects and the margin
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutEffects(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutEffects(s);
        }

        /// <summary>
        /// 14) Margins are added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutMargin(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutMargin(s);
        }

        /// <summary>
        /// 15) Add rings here to insert them around the margin. Rings will be outermost
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutMargin(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutMargin(s);
        }

        /// <summary>
        /// Occurs when the layout is rotated. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutRotate(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutRotate(s);
        }

        /// <summary>
        /// Occurs after the layout is rotated. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutRotate(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutRotate(s);
        }

        /// <summary>
        /// Occurs when the layout is normalized to 0,0. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutNormalize(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutNormalize(s);
        }

        /// <summary>
        /// Occurs after the layout is normalized. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutNormalize(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutNormalize(s);
        }

        /// <summary>
        /// Occurs when the layout point values are rounded to integers. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void LayoutRound(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.LayoutRound(s);
        }

        /// <summary>
        /// Occurs after the layout point values are rounded to integers. May occur anywhere betweeen BeginLayout and EndLayotu
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostLayoutRound(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostLayoutRound(s);
        }

        /// <summary>
        /// 16) Occurs once layout has finished. No more changes should occur to points or rings in the layout after this method. destSize is calculated here.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void EndLayout(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.EndLayout(s);
        }
        /// <summary>
        /// 17) The destination bitmap is created and sized based destSize. A graphics object is initialized for rendering.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PrepareDestinationBitmap(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PrepareDestinationBitmap(s);
        }
        /// <summary>
        /// 18) Rendering is ready to start
        /// </summary>
        /// <param name="s"></param>
        protected virtual void BeginRender(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.BeginRender(s);
        }
        /// <summary>
        /// 19) The background color is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderBackground(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderBackground(s);
        }
        
        protected virtual void PostRenderBackground(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderBackground(s);
        }

        /// <summary>
        /// 21) Effects, such as shadow, are rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderEffects(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderEffects(s);
        }

        protected virtual void PostRenderEffects(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderEffects(s);
        }
        /// <summary>
        /// 23) The padding is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderPadding(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderPadding(s);
        }

        protected virtual void PostRenderPadding(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderPadding(s);
        }
        /// <summary>
        /// 25) An ImageAttributes instance is created if it doesn't already exist.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void CreateImageAttribues(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.CreateImageAttribues(s);
        }
        /// <summary>
        /// 26) The ImageAttributes instance exists and can be modified or replaced.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void PostCreateImageAttributes(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostCreateImageAttributes(s);
        }


        /// <summary>
        /// 27) The image is copied to the destination parallelogram specified by ring 'image'
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderImage(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderImage(s);
        }
        protected virtual void PostRenderImage(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderImage(s);
        }
        /// <summary>
        /// 29) The border is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderBorder(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderBorder(s);
        }

        protected virtual void PostRenderBorder(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderBorder(s);
        }

        /// <summary>
        /// 31) watermarks can be rendered here.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderOverlays(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderOverlays(s);
        }

        protected virtual void PostRenderOverlays(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.PostRenderOverlays(s);
        }

        /// <summary>
        /// 33) Changes are flushed to the bitmap here and the graphics object is destroyed.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void EndRender(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.EndRender(s);
        }

        /// <summary>
        /// 34) Changes have been flushed to the bitmap, but the final bitmap has not been flipped yet.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void RenderComplete(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.RenderComplete(s);
        }

        /// <summary>
        /// 35) Non-rendering changes to the bitmap object occur here, such as flipping. The graphics object is unavailable.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void ProcessFinalBitmap(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.ProcessFinalBitmap(s);
        }

        /// <summary>
        /// 36) Layout and rendering are both complete.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void EndProcess(ImageState s) {
            foreach (AbstractImageProcessor p in exts) p.EndProcess(s);
        }
    }
}
