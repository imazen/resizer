# Builder Extensions (deprecated)

NOTE: BuilderExtensions no longer make sense as they are not compatible with the Imageflow backend. Add features to Imageflow instead.

The ImageResizer plugin model encompases several aspects of the resizer
pipeline.  The most obvious are the ImageBuilder BuilderExtensions, which
provide resizing and image manipulation functionality.

All extensions to the resizing process derive from the `BuilderExtension` base
class, which in turn derives from `AbstractImageProcessor`.  The
`AbstractImageProcessor` class serves two purposes:

 1. It defines 53 virtual methods which individual extensions can override as
    needed.  9 of these methods are for acquiring or beginning the overall
    process, and the remaining 44 are individual steps of the resizing
    process.

 2. It provides a default implementation for each of these methods that pass
    the handling of the method on to any sub-extensions attached via a
    protected "list of `BuilderExtension`s" member, `exts`.  This is how the
    `ImageBuilder` delegates processing to the resizing plugins.

The descriptions below describe what an extension's implementation is expected
to do, if the extension overrides the method.

## Image Acquisition

>### `protected virtual void PreLoadImage(ref object source, ref string path, ref bool disposeSource, ref ResizeSettings settings)`

Called before the source image is loaded.  At present, no extensions override
this method.

>### `protected virtual Stream GetStream(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition)`

Provides the `Stream` for the given source.  The RemoteReader plugin
implements this in order to support remote URL-based source images.

>### `public virtual Bitmap DecodeStreamFailed(Stream s, ResizeSettings settings, string optionalPath)`

Called when `ImageBuilder.DecodeStream()` throws an exception.  The FreeImage,
WIC, PsdReader and WebP extensions implement this method in order to support
their respective image types. 

<span style="color:red">**_Why is the `DecodeStream()` override not
sufficient?  Why does this appear to get handled twice?_**</span>

Some parsers only tell you that they can't handle a format by throwing an exception. However, if we tell the plugins to not throw exceptions, we won't get any feedback about other categories of problems.

Thus, DecodeStream lets the first appropriate parser take a stab at the problem. The exception is later re-thrown if DecodeStreamFailed can't produce an alternate, working parser.

DecodeStreamFailed indicates that file parsers should *not* throw any exceptions - they just pass control to the next plugin parser in the line.

>### `public virtual Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath)`

Called to decode an image `Stream` into a `Bitmap`.  The FreeImage, WIC,
PsdReader and WebP extensions implement this method in order to support their
respective image types.

>### `protected virtual RequestedAction PostDecodeStream(ref Bitmap img, ResizeSettings settings)`

Called after `DecodeStream()` (and possibly `DecodeStreamFailed()`) has
converted the `Stream` into a `Bitmap`.  Implemented by the `AutoRotate`
extension to flip/rotate the image based on the Exif "Orientation" tag on the
source image.

>### `protected virtual void PreAcquireStream(ref object dest, ResizeSettings settings)`

Called prior to acquiring the _destination_ stream before the resizing process
begins.  At present, no extensions override this method.

## High-level Image Resizing

>### `protected virtual RequestedAction BuildJob(ImageResizer.ImageJob job)`

Called (by `ImageBuilder.Build()`) to start the image processing pipeline.
If overridden, replaces the default `ImageBuilder` handing of the pipeline.
FreeImage, WIC, and WPF provide override implementations that take over if the
job settings include their value for the `builder` key.

>### `protected virtual RequestedAction buildToStream(Bitmap source, Stream dest, ResizeSettings settings)`

Called after the destination `Bitmap` has been created (after
`buildToBitmap()`) if encoded output is needed.  The `ImageBuilder`
implementation finds and uses an appropriate encoder based on the settings.
The AnimatedGifs extension overrides to provide support for multi-frame
(animated) GIFs.

>### `protected virtual Bitmap buildToBitmap(Bitmap source, ResizeSettings settings, bool transparencySupported)`

Called to create the output `Bitmap` based on the source `Bitmap` and the
settings.  Only overridden by `ImageBuilder` in order to call `Process()` and
begin the default resizing pipeline.

## Low-level Image Resizing

All 44 lower-level image resizing methods have an identical signature: they
take an `ImageState` parameter which can be modified as a part of the
implementation of any of the methods, and return a `RequestedAction` which
indicates whether the overall handling should be canceled or not.

Many of the methods come in both <code>_Xxx_()</code> and
<code>Post_Xxx_()</code> forms; a couple also have a <code>Pre_Xxx_()</code>
variation.  These will be documented together, calling out only where
implementations provide specific and distinct behavior.

The resizing methods fall into two groups: layout, and render.  The layout
methods calculate values on the `ImageState`'s `ResizeSettings` object,
typically `BoxPadding` values.  After the plugins set these values, the
`ImageBuilder` implementation turns these `BoxPadding` values into "rings"
on the `ImageState`'s `LayoutBuilder` object.

>### `protected virtual RequestedAction OnProcess(ImageState s)`

Called at the very beginning of the resizing pipeline, to give extensions a
chance to modify the querystring or settings.  Not currently implemented by
any extensions.

>### `protected virtual RequestedAction PrepareSourceBitmap(ImageState s)`<br/>`protected virtual RequestedAction PostPrepareSourceBitmap(ImageState s)`

Called to perform any pre-layout work with the source `Bitmap`.  The Faces
plugin implements `PostPrepareSourceBitmap()` to add a list of `Face` data to
the `ImageState.Data` dictionary.

>### `protected virtual RequestedAction Layout(ImageState s)`

Implemented by `ImageBuilder` to call the various layout-related methods in
sequence.

>### `protected virtual RequestedAction FlipExistingPoints(ImageState s)`

Unused.

>### `protected virtual RequestedAction LayoutImage(ImageState s)`<br/>`protected virtual RequestedAction PostLayoutImage(ImageState s)`

Called to populate `ImageState`'s `copyRect` rectangle and other layout data.

The SizeLimiting plugin implements `PostLayoutImage()` to set the scaling
factor when it exceeds the size limits specified in the configuration.  The
SeamCarving plugin implements both methods in order to calculate carving data.
(The Post-version doesn't actually do anything.)  The WhitespaceTrimmer plugin
implements both methods in order to find unused pixels around the outside of
the image and update the `copyRect` value.  The WPF plugin implements
`LayoutImage()`, but only calls the base implementation.

After the plugins get a chance to handle `LayoutImage()`, `ImageBuilder`'s
implementation calculates the size needed for any cropping and resizing
specified in the settings or querystring, then adds the "image" and
"imageArea" rings.

>### `protected virtual RequestedAction LayoutPadding(ImageState s)`<br />`protected virtual RequestedAction PostLayoutPadding(ImageState s)`

Called to add padding around the image.  No plugins currently implement these
methods.

After the plugins get a chance to handle `LayoutPadding()`, `ImageBuilder`'s
implementation adds any padding requested as a "padding" ring to the
`LayoutBuilder`.

>### `protected virtual RequestedAction LayoutBorder(ImageState s)`<br />`protected virtual RequestedAction PostLayoutBorder(ImageState s)`

Called to add space for a border around the image.  No plugins currently
implement these methods.

After the plugins get a chance to handle `LayoutBorder()`, `ImageBuilder`'s
implementation adds any padding requested as a "border" ring to the
`LayoutBuilder`.

>### `protected virtual RequestedAction LayoutEffects(ImageState s)`<br />`protected virtual RequestedAction PostLayoutEffects(ImageState s)`

Called to add space for effects to the image.

The DropShadow plugin implements `LayoutEffects()` to add a "shadow" ring to
the layout.

>### `protected virtual RequestedAction LayoutMargin(ImageState s)`<br />`protected virtual RequestedAction PostLayoutMargin(ImageState s)`

Called to add space for a margin around the image.  No plugins currently
implement these methods.

After the plugins get a chance to handle `LayoutMargin()`, `ImageBuilder`'s
implementation adds any margin requested as a "margin" ring to the
`LayoutBuilder`.

>### `protected virtual RequestedAction LayoutRotate(ImageState s)`<br />`protected virtual RequestedAction PostLayoutRotate(ImageState s)`

Called to adjust the layout in order to accomodate any requested rotation.
No plugins currently implement these methods.

After the plugins get a chance to handle `LayoutRotate()`, `ImageBuilder`'s
implementation calls `LayoutBuilder.Rotate()` to rotate all of the rings.

>### `protected virtual RequestedAction LayoutNormalize(ImageState s)`<br />`protected virtual RequestedAction PostLayoutNormalize(ImageState s)`

Called to normalize the layout to (0,0).  No plugins currently implement these
methods.

After the plugins get a chance to handle `LayoutNormalize()`, `ImageBuilder`'s
implementation calls `LayoutBuilder.Normalize()` to normalize the layout.

>### `protected virtual RequestedAction LayoutRound(ImageState s)`<br />`protected virtual RequestedAction PostLayoutRound(ImageState s)`

Called to round the layout to whole integers.  No plugins currently implement
these methods.

>### `protected virtual RequestedAction EndLayout(ImageState s)`

Called to indicate the end of the layout-related methods.  Only implemented
by `ImageBuilder` to round the final size to a whole number of pixels and
ensure a minimum 1x1 size.

>### `protected virtual RequestedAction PrepareDestinationBitmap(ImageState s)`

Called in between the layout and rendering passes.

The SizeLimiting plugin implements this to validate that the final destination
size does not exceed the configured limits.

After the plugins get a chance to handle `PrepareDestinationBitmap()`,
`ImageBuilder`'s implementation creates the destination `Bitmap` and
`Graphics` objects.

>### `protected virtual RequestedAction Render(ImageState s)`

Called to start the rendering pass.

The RedEye plugin implements this to look for the locations of eyes in the
source image.  The Faces plugin implements this to detect faces in the source
image.

After the plugins get a chance to handle `Render()`, `ImageBuilder`'s
implementation calls the various render-related methods in sequence.

>### `protected virtual RequestedAction RenderBackground(ImageState s)`<br />`protected virtual RequestedAction PostRenderBackground(ImageState s)`

Called to render the background of the destination image.

The Watermark plugin implements `PostRenderBackground()` to render any
watermark intended to show over the background.

After the plugins get a chance to handle `RenderBackground()`,
`ImageBuilder`'s implementation fills the background color in case the source
image includes any transparency.

>### `protected virtual RequestedAction RenderEffects(ImageState s)`<br />`protected virtual RequestedAction PostRenderEffects(ImageState s)`

Called to render any effects.

The DropShadow plugin implements `RenderEffects()` to draw the shadow to the
destination `Graphics` object.

`ImageBuilder` does not override these methods.

>### `protected virtual RequestedAction RenderPadding(ImageState s)`<br />`protected virtual RequestedAction PostRenderPadding(ImageState s)`

Called to render padding around the source image.  No plugins currently
implement these methods.

After the plugins get a chance to handle `RenderPadding()`,
`ImageBuilder`'s implementation draws the padding polygon in the destination
`Graphics` object.

>### `protected virtual RequestedAction CreateImageAttribues(ImageState s)`<br />`protected virtual RequestedAction PostCreateImageAttributes(ImageState s)`

Called to create an `ImageAttributes` object for the destination image.  No
plugins currently implement these methods.

After the plugins get a chance to handle `CreateImageAttribues()`,
`ImageBuilder`'s implementation creates an `ImageAttributes` object if it
doesn't already exist.

>### `protected virtual RequestedAction PreRenderImage(ImageState s)`

Called to pre-process the source image before rendering.  Any changes should
be saved to the `ImageState`'s `preRenderBitmap` field.

The SimpleFilters plugin implements this to support rounded corners.  The
AdvancedFilters plugin implements `PreRenderImage()` to support feathered
edges.  The SeamCarving plugin implements this to perform seam-carving-based
resizing.  The FreeImage plugin implements this to perform FreeImage resizing.

>### `protected virtual RequestedAction RenderImage(ImageState s)`<br />`protected virtual RequestedAction PostRenderImage(ImageState s)`

Called to render the source (or `preRenderBitmap`) image into the "image" ring
in the destination image.

The SpeedOrQuality plugin implements `RenderImage()` to choose specific
settings that impact the speed when drawing the destination image.  If used,
returns `RequestedAction.Cancel` to prevent `ImageBuilder`'s default handling.

The SimpleFilters plugin implements `PostRenderImage()` in order to fill the
"image" ring with an overlay color (presumably a not-completely-opaque color).
The AdvancedFilters plugin implements `PostRenderImage()` in order to provide
a variety of effects, including blur, sharpen, oil painting, noise reduction,
sepia, edge detection, etc.  The RedEye plugin implements `PostRenderImage()`
in order to draw the red-eye reduction effects to the destination image.
<span style="color:red">_**(Seems to draw a rectangle?)**_</span>
The Faces plugin implements `PostRenderImage()` in order to show the bounds
of the calculated face rectangles if the "f.show" setting is "true".

After the plugins get a chance to handle `RenderImage()`, `ImageBuilder`'s
implementation draws either the `preRenderBitmap` (if it exists) or source
`Bitmap` into the rectangle specified by the "image" ring in the
`LayoutBuilder`.

>### `protected virtual RequestedAction RenderBorder(ImageState s)`<br />`protected virtual RequestedAction PostRenderBorder(ImageState s)`

Called to render a border around the destination image.  No plugins currently
implement these methods.

After the plugins get a chance to handle `RenderBorder()`, `ImageBuilder`'s
implementation draws the border polygon in the destination `Graphics` object.

>### `protected virtual RequestedAction PreRenderOverlays(ImageState s)`

Called to give plugins a last chance to make changes before any overlays
are rendered.  No plugins currently implement this method.

>### `protected virtual RequestedAction RenderOverlays(ImageState s)`

Called to render any overlays on the destination image.

The Watermark plugin implements this to render any watermark intended to show
on top of the image.

`ImageBuilder` does not provide a default implementation of this method.

>### `protected virtual RequestedAction PreFlushChanges(ImageState s)`

Called before the destination `Graphics` object is flushed into the
destination `Bitmap`.

The Trial plugin implements this to write "Unlicensed" across the image.

`ImageBuilder` does not provide a default implementation of this method.

>### `protected virtual RequestedAction FlushChanges(ImageState s)`<br />`protected virtual RequestedAction PostFlushChanges(ImageState s)`

Called to flush the destination `Graphics` object into the destination
`Bitmap`.  No plugins currently implement these methods.

After the plugins get a chance to handle `FlushChanges()`, `ImageBuilder`'s
implementation flushes the destination `Graphics` object and calls
`Dispose()` on it.

>### `protected virtual RequestedAction ProcessFinalBitmap(ImageState s)`

Called for any non-rendering changes to the destination `Bitmap`.

The CopyMetadataPlugin extension implements this to copy metadata `PropertyItem`s
from the source bitmap to the destination bitmap.

After the plugins get a chance to handle `ProcessFinalBitmap()`,
`ImageBuilder`'s implementation may perform a final rotation/flip.

>### `protected virtual RequestedAction EndProcess(ImageState s)`

Called at the very end of the processing pipeline.  No plugins currently
implement this method.

`ImageBuilder` does not provide a default implementation of this method.
