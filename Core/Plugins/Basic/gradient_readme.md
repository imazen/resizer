Tags: plugin
Bundle: free
Edition: free
Tagline: "Create gradients from css, js, or html: /gradient.png?color1=FFFFFFAA&color2=BBBBBB99&width=10&width=10&rotate=90."
Aliases: /plugins/gradient

# Gradient plugin

Generates gradients on the fly. Very useful for rapid prototyping and design - but safe for production use!

## Installation

1. Add `<add name="Gradient" />` to the `<plugins />` section.

## Syntax

`gradient.png?width=10&height=10&color1=EEAAFFDD&color2=AAABB3300&angle=90`

* width/height: The size of the PNG to create
* color1: can be a named color, or a hex color. Accepts 6 and 8-digit hex values (last two digits of 8-digit hex values are for transparency)
* color2: the second color in the gradient.
* angle: the gradient angle in degrees.
* Can be combined with all other standard image resizing commands.


## Examples


* `/gradient.png?width=200&height=10&color1=00dd0099&color2=0000ee991`: ![gradient](http://img.imageresizing.net/gradient.png;width=200;height=10;color1=00dd0099;color2=0000ee99)

* `/gradient.png?width=200&height=10&color1=0066a1&color2=black`: ![gradient](http://img.imageresizing.net/gradient.png;width=200;height=10;color1=0066a1;color2=black)

* `/gradient.png?width=200&height=10&color1=0066a122&color2=00000044&angle=90`: ![gradient](http://img.imageresizing.net/gradient.png;width=100;height=10;color1=0066a122;color2=00000044;angle=10)


## Source code to plugin

The Gradient plugin is an example of a simple yet very useful plugin. It implements IQuerystringPlugin to inform the ImageResizer to 'look' for image URLs with specific querystring keys for processing, and implements IVirtualImageProvider so it can provide gradient images to the ImageResizer as if they existed on disk. This allows those gradients to be post-processed like any image, even included as a watermark over another image. 

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Collections.Specialized;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using ImageResizer.Util;
    using System.IO;
    using System.Drawing.Imaging;

    namespace ImageResizer.Plugins.Basic {
        /// <summary>
        /// Allows gradients to be dynamically generated like so:
      /// /gradient.png?color1=white&amp;color2=black&amp;angle=40&amp;width=20&amp;height=100
        /// </summary>
        public class Gradient: IPlugin, IQuerystringPlugin, IVirtualImageProvider {
          
            public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
                return (virtualPath.EndsWith("/gradient.png", StringComparison.OrdinalIgnoreCase));
            }

            public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
                return new GradientVirtualFile(queryString);
            }

            public IEnumerable<string> GetSupportedQuerystringKeys() {
                return new string[] {"color1","color2", "angle", "width", "height" };
            }

            public IPlugin Install(Configuration.Config c) {
                c.Plugins.add_plugin(this);
                return this;
            }

            public bool Uninstall(Configuration.Config c) {
                c.Plugins.remove_plugin(this);
                return true;
            }


            public class GradientVirtualFile : IVirtualFile, IVirtualBitmapFile {
                public GradientVirtualFile(NameValueCollection query) { this.query = new ResizeSettings(query); }
                public string VirtualPath {
                    get { return "gradient.png"; }
                }

                protected ResizeSettings query;

              public System.IO.Stream Open() {
                  MemoryStream ms = new MemoryStream();
                  using (Bitmap b = GetBitmap()) {
                      b.Save(ms, ImageFormat.Png);
                  }
                  ms.Seek(0, SeekOrigin.Begin);
                  return ms;
              }

              public System.Drawing.Bitmap GetBitmap() {
                  Bitmap b = null;
                  try {
                      int w = query.Width > 0 ? query.Width : (query.MaxWidth > 0 ? query.MaxWidth : 8);
                      int h = query.Height > 0 ? query.Height : (query.MaxHeight > 0 ? query.MaxHeight : 8);
                      float angle = Util.Utils.getFloat(query,"angle",0);
                      Color c1 = Utils.parseColor(query["color1"],Color.White);
                      Color c2 = Utils.parseColor(query["color2"],Color.Black);
                      b = new Bitmap(w, h);

                      using (Graphics g = Graphics.FromImage(b)) 
                      using (Brush brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,w,h),c1,c2,angle)){
                          g.FillRectangle(brush, 0, 0, w, h);
                      }
                  } catch {
                      if (b != null) b.Dispose();
                      throw;
                  }
                  return b;
              }
          }
      }
  }
