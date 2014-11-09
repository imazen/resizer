/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

// Contains classes for calculating and rendering images, as well as for building image processing plugins.
namespace ImageResizer.Resizing {
    /// <summary>
    /// What to do about remaining handlers/methods for the specified section
    /// </summary>
    public enum RequestedAction {
        /// <summary>
        /// Does nothing
        /// </summary>
        None = 0,
        /// <summary>
        /// Requests that ImageBuilder cancels the default logic of the method, and stop executing plugin calls for the method immediately.
        /// </summary>
        Cancel,
    }

    /// <summary>
    /// Not for external use. Inherit from BuilderExtension instead.
    /// Dual-purpose base class for both ImageBuilder and BuilderExtension
    ///  Extensions can inherit and override certain methods.
    /// ImageBuilder inherits this method to utilize its extension invocation code. 
    /// Each method of AbstractImageProcessor loops through all extensions and executes the same method on each. Provides a sort of multiple-inheritance mechanisim.
    /// </summary>
    public class AbstractImageProcessor {
        /// <summary>
        /// Creates a new AbstractImageProcessor with no extensions
        /// </summary>
        public AbstractImageProcessor() {
            exts = null;
        }
        /// <summary>
        /// Creates a new AbstractImageProcessor which will run the specified extensions with each method call.
        /// </summary>
        /// <param name="extensions"></param>
        public AbstractImageProcessor(IEnumerable<BuilderExtension> extensions) {
            exts = new List<BuilderExtension>(extensions != null ? extensions : new BuilderExtension[] { }); 
        }

        /// <summary>
        /// Contains the set of extensions that are called for every method. 
        /// </summary>
        [CLSCompliant(false)]
        protected volatile IEnumerable<BuilderExtension> exts;

        /// <summary>
        /// Extend this to allow additional types of source objects to be accepted by transforming them into Bitmap instances.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <param name="disposeSource"></param>
        /// <param name="settings"></param>
        protected virtual void PreLoadImage(ref object source, ref string path, ref bool disposeSource, ref ResizeSettings settings) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) p.PreLoadImage(ref source, ref path, ref disposeSource, ref settings);
        }

        /// <summary>
        /// Extend this to allow  additional types of source objects to be accepted by transforming them into Stream instances. First plugin to return a Stream wins.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        /// <param name="disposeStream"></param>
        /// <param name="path"></param>
        /// <param name="restoreStreamPosition"></param>
        /// <returns></returns>
        protected virtual Stream GetStream(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition) {
            path = null; //Init so the compiler doesn't complain
            restoreStreamPosition = false;

            if (exts != null) foreach (AbstractImageProcessor p in exts) {
                bool disposeS = disposeStream; //Copy the referenced boolean. Only allow plugins who return a stream to change its value
                Stream s = p.GetStream(source, settings, ref disposeS, out path, out restoreStreamPosition);
                if (s != null) {
                    disposeStream = disposeS;
                    return s;
                }
            }
            return null;
        }


        /// <summary>
        /// Extensions are executed until one extension returns a non-null value. 
        /// This is taken to mean that the error has been resolved.
        /// Extensions should not throw an exception unless they wish to cause subsequent extensions to not execute.
        /// If extensions throw an ArgumentException or ExternalException, it will be wrapped in an ImageCorruptedException instance.
        /// If the Bitmap class is used for decoding, read gdi-bugs.txt and make sure you set b.Tag to new BitmapTag(optionalPath,stream);
        /// </summary>
        public virtual Bitmap DecodeStreamFailed(Stream s, ResizeSettings settings, string optionalPath) {
            if (exts == null) return null;
            foreach (AbstractImageProcessor p in exts) {
                if (s.CanSeek && s.Position != 0)
                    s.Seek(0, SeekOrigin.Begin);

                Bitmap b = p.DecodeStreamFailed(s,settings, optionalPath);
                if (b != null) return b;
            }
            return null;
        }
        /// <summary>
        /// Extend this to support alternate image source formats. 
        /// If the Bitmap class is used for decoding, read gdi-bugs.txt and make sure you set b.Tag to new BitmapTag(optionalPath,stream);
        /// </summary>
        /// <param name="s"></param>
        /// <param name="settings"></param>
        /// <param name="optionalPath"></param>
        /// <returns></returns>
        public virtual Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            if (exts == null) return null;
            foreach (AbstractImageProcessor p in exts) {
                Bitmap b = p.DecodeStream(s,settings, optionalPath);
                if (b != null) return b;
            }
            return null;
        }


        /// <summary>
        /// Extend this to modify the Bitmap instance after it has been decoded by DecodeStream or DecodeStreamFailed
        /// </summary>
        protected virtual RequestedAction PostDecodeStream(ref Bitmap img, ResizeSettings settings) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.PostDecodeStream(ref img, settings) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }




        /// <summary>
        /// Extend this to allow additional types of *destination* objects to be accepted by transforming them into a stream.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        protected virtual void PreAcquireStream(ref object dest, ResizeSettings settings) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) p.PreAcquireStream(ref dest, settings);
        }

        /// <summary>
        /// The method to override if you want to replace the entire pipeline.
        /// All Build() calls call this method first. 
        /// Does nothing in ImageBuilder
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected virtual RequestedAction BuildJob(ImageResizer.ImageJob job) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.BuildJob(job) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        protected virtual RequestedAction BeforeEncode(ImageResizer.ImageJob job)
        {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.BeforeEncode(job) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        protected virtual RequestedAction EndBuildJob(ImageResizer.ImageJob job)
        {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.EndBuildJob(job) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        protected virtual RequestedAction InternalGraphicsDrawImage(ImageState state, Bitmap dest, Bitmap source, PointF[] targetArea, RectangleF sourceArea, float[][] colorMatrix) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.InternalGraphicsDrawImage(state, dest,source,targetArea,sourceArea,colorMatrix) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;

        }
     


        /// <summary>
        /// Called for Build() calls that want the result encoded. (Not for Bitmap Build(source,settings) calls.
        /// Only override this method if you need to replace the behavior of image encoding and image processing together, such as adding support
        /// for resizing multi-page TIFF files or animated GIFs.
        /// 
        /// Does NOT dispose of 'source' or 'source's underlying stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        protected virtual RequestedAction BuildJobBitmapToStream(ImageJob job, Bitmap source, Stream dest){
            if (exts != null) 
                foreach (AbstractImageProcessor p in exts)
                    if (p.BuildJobBitmapToStream(job, source, dest) == RequestedAction.Cancel) 
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
 
        /// <summary>
        /// Process.0 First step of the Process() method. Can replace the entire Process method if RequestAction.Cancel is returned.
        /// Can be used to add points to translate (for image maps), and also to modify the settings 
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction OnProcess(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.OnProcess(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.1 Switches the bitmap to the correct frame or page, and applies source flipping commands.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PrepareSourceBitmap(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.PrepareSourceBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.2 Extend this to apply any pre-processing to the source bitmap that needs to occur before Layout begins
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostPrepareSourceBitmap(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.PostPrepareSourceBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).0: This is the last point at which points to translate should be added.
        /// Only return RequestedAction.Cancel if you wish to replace the entire Layout sequence logic.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction Layout(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.Layout(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).1: This is where the points in the layout are flipped the same way the source bitmap was flipped (unless their flags specify otherwise)
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction FlipExistingPoints(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.FlipExistingPoints(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).2: Rings 'image' and 'imageArea' are added to the layout. 
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutImage(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.LayoutImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).3: Add rings here to insert them between the image area and the padding
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutImage(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.PostLayoutImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).4: Ring "padding" is added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutPadding(ImageState s) {
            if (exts != null) foreach (AbstractImageProcessor p in exts) if (p.LayoutPadding(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).5: Add rings here to insert them between the padding and the border
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutPadding(ImageState s) {
            if (exts != null) 
                foreach (AbstractImageProcessor p in exts) 
                    if (p.PostLayoutPadding(s) == RequestedAction.Cancel) 
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).6: Ring "border" is added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutBorder(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutBorder(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).7: Add rings here to insert them between the border and the effect rings
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutBorder(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutBorder(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).8: Effects such as 'shadow' are added here.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutEffects(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutEffects(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).9: Add rings here to insert them between the effects and the margin
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutEffects(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutEffects(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).10: Margins are added to the layout
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutMargin(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutMargin(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).11: Add rings here to insert them around the margin. Rings will be outermost
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutMargin(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutMargin(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs when the layout is rotated. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutRotate(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutRotate(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs after the layout is rotated. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutRotate(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutRotate(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs when the layout is normalized to 0,0. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutNormalize(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutNormalize(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs after the layout is normalized. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutNormalize(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutNormalize(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs when the layout point values are rounded to integers. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction LayoutRound(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.LayoutRound(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).anytime: Occurs after the layout point values are rounded to integers. May be called anytime during Layout()
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostLayoutRound(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostLayoutRound(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.3(Layout).12: Occurs once layout has finished. No more changes should occur to points or rings in the layout after this method. destSize is calculated here.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction EndLayout(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.EndLayout(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.4: The destination bitmap is created and sized based destSize. A graphics object is initialized for rendering.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PrepareDestinationBitmap(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PrepareDestinationBitmap(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render) Rendering. Do not return RequestedAction.Cancel unless  you want to replace the entire rendering system.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction Render(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.Render(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).1 The background color is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderBackground(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderBackground(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).2 After the background color is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostRenderBackground(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostRenderBackground(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).3 Effects (such as a drop shadow or outer glow) are rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderEffects(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderEffects(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).4 After outer effects are rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostRenderEffects(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostRenderEffects(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).5 Image padding is drawn
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderPadding(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderPadding(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).6 After image padding is drawn
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostRenderPadding(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostRenderPadding(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).7: An ImageAttributes instance is created if it doesn't already exist.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction CreateImageAttribues(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.CreateImageAttribues(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).8: The ImageAttributes instance exists and can be modified or replaced.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostCreateImageAttributes(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostCreateImageAttributes(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).9: Plugins have a chance to pre-process the source image before it gets rendered, and save it to s.preRenderBitmap
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PreRenderImage(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PreRenderImage(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).10: The image is copied to the destination parallelogram specified by ring 'image'. 
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderImage(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderImage(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).11: After the image is drawn
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostRenderImage(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostRenderImage(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).12: The border is rendered
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderBorder(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderBorder(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).13: After the border is drawn
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostRenderBorder(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostRenderBorder(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).14: Any last-minute changes before watermarking or overlays are applied
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PreRenderOverlays(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PreRenderOverlays(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).15: Watermarks can be rendered here. All image processing should be done
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction RenderOverlays(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.RenderOverlays(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
        /// <summary>
        /// Process.5(Render).16: Called before changes are flushed and the graphics object is destroyed.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PreFlushChanges(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PreFlushChanges(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).17: Changes are flushed to the bitmap here and the graphics object is destroyed.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction FlushChanges(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.FlushChanges(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.5(Render).18: Changes have been flushed to the bitmap, but the final bitmap has not been flipped yet.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction PostFlushChanges(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.PostFlushChanges(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.6: Non-rendering changes to the bitmap object occur here, such as flipping. The graphics object is unavailable.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction ProcessFinalBitmap(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.ProcessFinalBitmap(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.7: Layout and rendering are both complete.
        /// </summary>
        /// <param name="s"></param>
        protected virtual RequestedAction EndProcess(ImageState s) {
            if (exts != null)
                foreach (AbstractImageProcessor p in exts)
                    if (p.EndProcess(s) == RequestedAction.Cancel)
                        return RequestedAction.Cancel;
            return RequestedAction.None;
        }
    }
}
