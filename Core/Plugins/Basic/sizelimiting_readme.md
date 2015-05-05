Tags: plugin
Bundle: free
Edition: free
Tagline: (default)- Limit maximum resolution of photos, or the total size of all processed images.
Aliases: /plugins/sizelimiting

# SizeLimiting plugin

(Installed by default in 3.0.5+)
.
For preventing abuse and limiting photo resolution. Prevents request that would result in an image over 3200x3200 (configure with `totalWidth/totalHeight`).

You can also limit photo resolution with `imageWidth/imageHeight`. Originals will still be accessible unless you add a rewrite handler.

## Configuration

Example configuration with defaults: 

    <resizer>
      <sizelimits imageWidth="0" imageHeight="0" totalWidth="3200"
                  totalHeight="3200" totalBehavior="throwexception" />
      ...
    </resizer>



## totalWidth, totalHeight, & totalBehavior

(Defaults: 3200x3200, throwexception)

These settings can be used to prevent misuse of the image resizer. 

For example, an attacker could request a very large image to use a lot of CPU and RAM in a single request. Practically, it's insanely difficult to [DOS attack](http://en.wikipedia.org/wiki/Denial-of-service_attack) or DDOS attack the image resizer, due to GDI's memory allocation algorithm. GDI doesn't allow paging to disk, and requires consecutive chunks of memory. Under attack, memory gets fragmented, so image resizing requests get denied by GDI until the attack (or extremely high load) is over, and memory can be defragmented. 

With disk caching enabled, users may not even notice a DOS attack, since GDI calls would fail first, leaving plenty of fragmented RAM around for ASPX/HTML/CSS/Javascript files and cached images to be served, since their contiguous memory requirements are minimal.

totalWidth and totalHeight restrict the final output size of an image. They do not 'shrink' the image as `imageWidth` and `imageHeight` do. They simply cancel requests that exceed the permitted size. This prevents the memory from even being allocated for invalid requests.

**To disable these limits, set `totalBehavior="ignorelimits"`.**

When a request is canceled, a `SizeLimitException` is thrown with status code 500.

## imageWidth & imageHeight

Defaults: 0x0 (disabled)

`imageWidth` and `imageHeight` are the exact equivalents of `ImageResizerMaxWidth` and `ImageResizerMaxHeight` from version 2. When applied, they maintain aspect ratio.

They do **not** restrict the size of the output image, only the size of the *photo* within the image. I.e, padding, borders, margins, and rotation are not taken into effect.

These settings are *only* for limiting photo resolution, not preventing misuse. See totalWidth/totalSize for DOS attack limiting.

They affect the output in a similar manner as changing maxwidth and maxheight would have on an image larger than the specified dimensions. I.e, 

* Configuration: `<sizelimits imageWidth="800" imageHeight="600" />`
* Url: 'image.jpg?width=1000&height=800&paddingWidth=100
* Source image size: 1600x1200
* Result output size: 900x700
* Size of photo inside padding: 800x600


## Removing the plugin

In rare cases, it may make sense to completely remove the plugin. This can be done through code or via Web.config.

    protected void Application_Start(object sender, EventArgs e) {
      Config.Current.Plugins.LoadPlugins();
      Config.Current.Plugins.Get<SizeLimiting>().Uninstall(Config.Current) 
    }

Web.config: 

    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
      <configSections>
        <section name="resizer" type="ImageResizer.ResizerSection,ImageResizer"  requirePermission="false"  />
      </configSections>

      <resizer>
        <plugins>
          <remove name="SizeLimiting" />
        </plugins>
      </resizer>
    </configuration>





