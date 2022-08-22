Tags: plugin
Edition: creative
Tagline: Render multiple image & text overlays and background layers with incredible flexibility and great performance. 
Aliases: /plugins/watermark


# Watermark plugin

The Watermark plugin in is fully XML configurable, and supports named watermark configurations, 
multiple image and text layers, layer groups, and background (as well as overlay) layers. 
The positioning system is per-layer, permits flexible pixel and percent layout, anchoring, and container selection.

*Imageflow and future versions of ImageResizer are unlikely to support text layers. For best forwards compatibility, stick to image layers.*

## Installation

Either run `Install-Package ImageResizer.Plugins.Watermark` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.Watermark.dll to your project
2. Add `<add name="Watermark" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

## Configuration

The `<watermarks/>` element allows you to define image layers, text layers, and layer groups. 

By default all layers are overlays, but can be rendered as a background layer using `drawAs="background"`. This is effective when combined with padding or transparent source images.

The name attribute defines the ID that will be used from the querystring to enable the watermark.

Both image and text layers have a very flexible positioning system. You can use any combination of Left, Top, Right, Bottom, Width, and Height to get the layout desired. In rare instances where you still need to specify alignment, you can use the `align` [attribute to set it](http://msdn.microsoft.com/en-us/library/system.drawing.contentalignment.aspx). All values can be pixels (default) or percentages. Percentages are relative to the container width/height. `Right` and `Bottom` are relative to the right and bottom sides of the container.  The container is specified by `relativeTo`. Valid container names include 'image', 'imageArea', and 'canvas'. `image` is the photo itself, `imageArea` includes padding added to the image to keep aspect ratio, and `canvas` is the final dimensions of the file, including any borders, padding, or margins. 

Image layers can pre-process the watermark itself using the `imageQuery` attribute.

Text layers can print values passed in from the querystring if you use the #{key} notation in the `text` attribute, where `key` is a querystring key.

Example configuration:

```xml
  <resizer>
    ...
    <watermarks>
      <otherimages path="~/watermarks" right="20" bottom="20" width="20" height="20" />
      <image name="test1" path="~/watermarks/Sun_64.png" imageQuery="filter=alpha(0.5)&amp;rotate=45" top="0" left="0" />
      <text name="test2" text="Hello #{name}!" vertical="true" align="topright" />
      <image name="gradientbg" path="~/gradient.png" drawAs="background" imageQuery="color1=black&amp;color2=white&amp;angle=45" top="0" right="0" left="0" bottom="0" />
      <group name="storyBG">
        <image path="~/watermarks/Sun_64.png" align="topleft" width="100%" height="100%" relativeTo="canvas" drawAs="Background"></image >
      </group>
    </watermarks>
  </resizer>
```

Like the original plugin, it is possible to specify the watermark filename (but not path) in the querystring. When used in this way, it will first look for the `<otherimages />` element to determine the watermarks directory (from the 'path' attribute), and if `<otherimages path="">` is not present, it will fall back to the legacy configuration values and layout system for compatibility.


## Usage

You can specify multiple watermark layers and groups at the same time:

```
  image.jpg?watermark=test1,test2,storyBG
```

Like the old version, you can also specify watermark images by name, which will all use the configuration specified in the `<otherimages/>` element.

```
  image.jpg?watermark=image.png
```

## Common errors

* Make sure that the production server has all the fonts you are using installed. Servers, by default, don't have very many fonts installed.
  
## Layer attribute reference

* **name** - The key to use in the URL to invoke the application of the layer or group.
* **align** - How to align the layer content within its bounds when there is an ambiguity [List of possible values](http://msdn.microsoft.com/en-us/library/system.drawing.contentalignment.aspx).
* **Left** - Pixel or percent value relative to the left of the container. Ex. 10px, 3%. Percentages relative to container width.
* **Top** - Pixel or percent value relative to the left of the container. Ex. 10px, 3%. Percentages relative to container height.
* **Right** - Pixel or percent value relative to the right of the container. "10px" would be 10 pixels leftwards from the right side of the container. Percentages relative to container width.
* **Bottom** - Pixel or percent value relative to the bottom of the container. "10px" would be 10 pixels upwards from the bottom side of the container. 3%. Percentages relative to container height.
* **Width** - Pixel or percent value for the width of the layer.
* **Height** - Pixel or percent value for the height of the layer.
* **relativeTo** - Name of the container, such as 'image', 'imageArea', or 'canvas'. `image` is the photo itself, `imageArea` includes padding added to the image to keep aspect ratio, and `canvas` is the final dimensions of the file, including any borders, padding, or margins. 
* **fill** - True/false (defaults to false). If true, the contents of the layer will attempt to fill the layer, even if they are normally smaller. Maintains aspect ratio, but may upscale text and images, potentially causing blurriness. If no width/height values are specified, this will also make the layer attempt to fill the container bounds.
* **drawAs** - 'background' or 'overlay'. If 'background', the layer will be rendered over the background color, but underneath everything else. Combine with padding, alpha adjustment, or transparency on the original image to let it show through.

## ImageLayer specific attributes


* **path** - A virtual path (i.e, ~/folder/image or /app/folder/image) to the watermark image. 
* **imageQuery** - Allows pre-processing of the image. For example, use "stretch=fill" to distort the watermark to fit the aspect ratio of the layer. Multiple commands can be separated with ;

## TextLayer specific attributes

**Imageflow and future versions of ImageResizer are unlikely to support text layers. For best forwards compatibility, avoid their use.**

* **text** - The text to display. May contain querystring-specified values, referenced like this: "Hello #{name}" - `image.jpg?watermark=hi&name=Nathanael`.
* **vertical** - True to display the text vertically. May also be achieved with Angle, but rendering may be slightly better.
* **color** - A 3, 6, or 8-digit hex color reference, or a named color.
* **font** - like "Verdana". The font family.
* **style** - like "Regular", "Bold", "Italic", "Underline", "Strikeout", or comma-delimited combinations like "Bold,Italic". The default is "Bold".
* **angle** - the degrees clockwise to rotate the text.
* **fontSize** the height in pixels of the text. May not be very accurate, GDI isn't great at font heights. Will definitely vary with your font choice.
* **outlineColor** - The color for the outline of the text. Only rendered if outlineWidth >0
* **outlineWidth** - How many pixels wide to draw the outline. As the outline is under the text, start with 3 for a 1px outline, and increase by adding 2.
* **glowColor** - The color for the glow effect.
* **glowWidth** - The width of the glow effect.  As the glow is under the text, start with 3 for a 1px outline, and increase by adding 2.
* **rendering** - (3.1.5+) The rendering algorithm for the text. Valid values are SystemDefault, SingleBitPerPixelGridFit, SingleBitPerPixel, AntiAliasGridFit, AntiAlias, and ClearTypeGridFit. The default before 3.1.5 was ClearTypeGridFit. Afterwards, the default was AntiAliasGridFit.

## Managed API

Referencing a watermark already configured in XML is easy: 

```
  ImageBuilder.Current.Build(source,dest,new ResizeSettings("watermark=name"));
```

If you want to build a custom watermark from code, that's also easy, but needs to happen during Application\_Start if you're modifying Config.Current. 
Installing plugins on a shared Config instance on different threads can result in warnings on /resizer.debug, although the threading is handled correctly.

```
  //You can have multiple configurations. Config.Current contains web.config settings, but you can use new Config(); to get a clean slate.
  Config c = Config.Current; 
  
  //Get a reference to the instance we added previously
  WatermarkPlugin wp = c.Plugins.Get<WatermarkPlugin>();
  if (wp == null) { //Install it if it's missing
    wp = new WatermarkPlugin();
    wp.Install(c);
  }
  //Re-query in case another thread beat us to installation.
  wp = c.Plugins.Get<WatermarkPlugin>();
  
  //Let's make some layers
  TextLayer t = new TextLayer();
  t.Text = "Hello #{name}";
  t.Fill = true; //Fill the image with the text
  ImageLayer i = new ImageLayer(c); //ImageLayer needs a Config instance so it knows where to locate images
  i.Path = "~/image.png";
  
  //Let's register them with the watermark plugin. Note a layer name can have multiple layers
  wp.NamedWatermarks["img"] = new Layer[]{ i };
  wp.NamedWatermarks["text"] = new Layer[] { t };

  
  //Let's build an image
  c.CurrentImageBuilder.Build(source,dest,new ResizeSettings("watermark=text,img;name=John Doe"));
```

# Original watermark plugin (Versions 3.0.13 and older)

The original version of the Watermark plugin did not support XML configuration, and only allowed one set of layout directions, which were applied to all watermark files. 
All watermark files were required to be located in the same folder. The &watermark=file.png command would select a file from that directory for use.

To install and configure the plugin, you must place some code in the Application_Start event of Global.asax.cs.

Here is an example Global.asax.cs file

``` 
  using System;
  using System.Collections.Generic;
  using System.Web;
  using ImageResizer;
  using System.Drawing;
  using ImageResizer.Configuration;
  using ImageResizer.Plugins.RemoteReader;
  
  namespace ComplexWebApplication {
    public class Global : System.Web.HttpApplication {
      protected void Application_Start(object sender, EventArgs e) {
      
        // Code that runs on application startup
        ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
        w.align = System.Drawing.ContentAlignment.BottomLeft; //Where to align the watermark within the bounds specified by bottomRightPadding and topLeftPadding
        w.hideIfTooSmall = true;
        w.keepAspectRatio = true; //Maintains the aspect ratio of the watermark itself.
        w.valuesPercentages = false; //When true .bottomRightPadding, .topLeftPadding, and .watermarkSize are all percentages of the primary image size. Percentages are 0..1, not 0..100.
        w.watermarkDir = "~/watermarks/"; //Where the watermark plugin looks for the image specified in the querystring ?watermark=file.png
        w.bottomRightPadding = new System.Drawing.SizeF(20, 20); //Padding between the bottom and right edges of the watermark and the primary image
        w.topLeftPadding = new System.Drawing.SizeF(20, 20); //Padding between the top  and left edges of the watermark and the primary image
        w.watermarkSize = new System.Drawing.SizeF(30, 30); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
        //Install the plugin
        w.Install(Config.Current);
      }
    }
  }

```