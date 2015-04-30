Aliases: /docs/extending-imagebuilder /docs/plugins/extending-imagebuilder /docs/extend/extending-imagebuilder 

# Extending ImageBuilder


ImageBuilder can be extended in two ways.

1. By writing a subclass of ImageBuilderExtension, and registering it with Config.ImageBuilderExtensions (It is suggested you implement IPlugin also)
2. By implementing IEncoder and registering it with Config.ImageEncoders


# Public methods

The primary method. Accepts VirtualFiles, physical paths, Bitmap objects, etc. Extremely flexible. Uses LoadImage to turn 'source' into a bitmap.
`void Build(object source, object dest, ResizeSettingsCollection settings);`

## Lifecycle of Build()

1. Calls LoadImage(source) to acquire a source bitmap
2. Calls PreAcquireStream
3. If destination is a string or stream, acquires an output stream from `dest`
4. If dest is a stream, calls buildToStream. Otherwise, calls buildToBitmap.

Convenience method for getting a Bitmap result (Wraps above method):
`Bitmap Build(object source, ResizeSettingsCollection settings);`


`Bitmap LoadImage(object source, bool useICM);`

1. Calls PreLoadImage(ref object source, ref bool useICM)
2. Turns a string, VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream into a Bitmap and returns it. Attaches any path (if present) to Bitmap.Tag


# Protected methods

Calls buildToBitmap, then encodes the result into the specified stream
`protected virtual void buildToStream(Bitmap source, Stream dest, ResizeSettingsCollection settings)`

Everything channels through here. This is where the science happens.
`protected virtual Bitmap buildToBitmap(Bitmap source, ResizeSettingsCollection settings)`



# Lifecycle of buildToBitmap

1. Prepare ImageState instance. All operations occur on an ImageState instance. Populates with sourceBitmap, settings, maxSize, and originalSize
2. Runs Process(ImageState)
3. Disposes unneeded objects in ImageState and returns the resulting bitmap.







