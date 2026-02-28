Tags: plugin
Bundle: free
Edition: free
Tagline: Use the Exif rotation data from the camera to auto-rotate your images.
Aliases: /plugins/autorotate

# AutoRotate (built into core since v4)

Automatically rotates images based on the Exif Orientation flag embedded by the camera. This corrects the common problem of portrait photos from phones appearing rotated 90 degrees.

Since ImageResizer v4, autorotation is built into the core (`ImageResizer.dll`). You do **not** need to install a separate plugin. The `AutoRotate` plugin class still exists for backwards compatibility but is marked `[Obsolete]` and does nothing.

## Enabling autorotation

Autorotation is **off by default**. You must opt in.

### For all images (recommended)

Add this to your `<resizer>` section in Web.config:

```xml
<pipeline defaultCommands="autorotate.default=true" />
```

### Per-request

Append `&autorotate=true` to the image URL. This overrides the default.

`&autorotate=false` can be used to disable autorotation for a specific request when the default is enabled.

## Common issues

* **Images appear rotated 90 degrees** — You need autorotation enabled. Most phone cameras embed an Exif Orientation flag instead of physically rotating the pixel data.
* **PNG files are not rotated** — PNG does not support Exif metadata. If you convert from JPEG to PNG, apply `&autorotate=true` during the conversion so the rotation is baked into the pixels.
* **`<add name="AutoRotate" />` in plugins section** — This is harmless but unnecessary in v4. The plugin does nothing; remove it to avoid confusion.
