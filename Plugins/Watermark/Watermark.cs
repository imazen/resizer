/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using System.Web;
using ImageResizer.Util;
using ImageResizer.Resizing;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Web.Caching;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Plugins.Watermark
{
    /// <summary>
    /// Provides extensibility points for drawing watermarks and even modifying resizing/image settings
    /// </summary>
    public class WatermarkPlugin : LegacyWatermarkFeatures, IPlugin, IQuerystringPlugin
    {
        public WatermarkPlugin() {
        }

        ImageLayer _otherImages = null;
        /// <summary>
        /// When a &amp;watermark command does not specify a named preset, it is assumed to be a file name. 
        /// Set OtherImages.Path to the search folder. All watermark images (except for presets) must be in the root of the search folder. 
        /// The remainder of the settings affect how each watermrak will be positioned and displayed.
        /// </summary>
        public ImageLayer OtherImages {
            get { return _otherImages; }
            set { _otherImages = value; }
        }
        protected Dictionary<string, IEnumerable<Layer>> _namedWatermarks = null;
        /// <summary>
        /// This dictionary contains watermarks keyed by name. Values are enumerations of layers - a watermark can have multiple layers.
        /// </summary>
        public Dictionary<string, IEnumerable<Layer>> NamedWatermarks {
            get { return _namedWatermarks; }
            set { _namedWatermarks = value; }
        }
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            _namedWatermarks = ParseWatermarks(c.getConfigXml().queryFirst("watermarks"), ref _otherImages);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "watermark" };
        }

        protected Dictionary<string, IEnumerable<Layer>> ParseWatermarks(Node n, ref ImageLayer otherImageDefaults) {
            Dictionary<string, IEnumerable<Layer>> dict = new Dictionary<string, IEnumerable<Layer>>(StringComparer.OrdinalIgnoreCase);
            if (n == null || n.Children == null) return dict;
            foreach (Node c in n.Children) {
                //Verify the name is specified and is unique.
                string name = c.Attrs["name"];
                if (c.Name.Equals("image", StringComparison.OrdinalIgnoreCase) 
                    || c.Name.Equals("text", StringComparison.OrdinalIgnoreCase)
                    || c.Name.Equals("group", StringComparison.OrdinalIgnoreCase)) {
                    if (string.IsNullOrEmpty(name) || dict.ContainsKey(name)) {
                        this.c.configurationSectionIssues.AcceptIssue(new Issue("WatermarkPlugin", "The name attribute for each watermark or watermark group must be specified, and must be unique.",
                        "XML: " + c.ToString(), IssueSeverity.ConfigurationError));
                        continue;
                    }
                }

                

                if (c.Name.Equals("otherimages", StringComparison.OrdinalIgnoreCase)) otherImageDefaults = new ImageLayer(c.Attrs, this.c);
                if (c.Name.Equals("image", StringComparison.OrdinalIgnoreCase)) dict.Add(name, new Layer[]{new ImageLayer(c.Attrs, this.c)});
                if (c.Name.Equals("text", StringComparison.OrdinalIgnoreCase)) dict.Add(name, new Layer[] {new TextLayer(c.Attrs) });
                if (c.Name.Equals("group", StringComparison.OrdinalIgnoreCase)) {
                    
                    List<Layer> layers = new List<Layer>();
                    if (c.Children != null) {
                        foreach (Node layer in c.Children) {
                            if (layer.Name.Equals("image", StringComparison.OrdinalIgnoreCase)) layers.Add(new ImageLayer(layer.Attrs, this.c));
                            if (layer.Name.Equals("text", StringComparison.OrdinalIgnoreCase)) layers.Add(new TextLayer(layer.Attrs));
                        }
                    }
                    dict.Add(name, layers);
                }  
            }
            return dict;
        }
        /*<resizer>
         * <watermarks>
         *  <otherimages path="~/watermarks" align="topleft" width="50%" />
         *  <group name="wmark">
         *   <image path="~/watermarks/image.png" align="topleft" width="50px" height="50px" />
         *  </group>
         */
        //top, left, bottom, right = px or percentages (relative to container)
        //relativeTo = image|imageArea|padding|border|margin|canvas
        //drawAs overlay|background
        //image
        //imagesettings
        //align = topleft|topright|bottomleft|bottomright|...



        protected RequestedAction RenderLayersForLevel(ImageState s, Layer.LayerPlacement only) {
            string watermark = s.settings["watermark"]; //from the querystring
            Graphics g = s.destGraphics;
            if (string.IsNullOrEmpty(watermark) || g == null) return RequestedAction.None;

            string[] parts = watermark.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            bool foundPart = false;

            foreach (string w in parts) {
                if (NamedWatermarks.ContainsKey(w)) {
                    IEnumerable<Layer> layers = NamedWatermarks[w];
                    foreach (Layer l in layers) {
                        if (l.DrawAs == only) {
                            l.RenderTo(s);
                        }
                    }
                    foundPart = true;
                }
            }
            if ( !foundPart && only == Layer.LayerPlacement.Overlay) {
                //Parse named watermark files
                
                if (watermark.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1 ||
                    watermark.IndexOfAny(new char[] { '\\', '/' }) > -1)
                    throw new ArgumentException("Watermark value contained invalid file name characters: " + watermark);




                if (OtherImages != null && OtherImages.Path != null) {
                    ImageLayer layer = OtherImages.Copy();

                    //Is the watermark dir a physical path?
                    char slash = layer.Path.Contains("/") ? '/' : '\\';
                    layer.Path = layer.Path.TrimEnd(slash) + slash + watermark.TrimStart(slash);


                    //Verify the file exists if we're in ASP.NET. If the watermark doesn't exist, skip watermarking.
                    if (!c.Pipeline.FileExists(watermark, layer.ImageQuery) && slash == '/') return RequestedAction.None;
                    layer.RenderTo(s);


                } else {
                    this.LegacyDrawWatermark(s); 
                }
            }
            return RequestedAction.None;

        }

        protected override RequestedAction PostRenderBackground(ImageState s) {
            return RenderLayersForLevel(s, Layer.LayerPlacement.Background);
        }

        protected override RequestedAction RenderOverlays(ImageState s) {
            return RenderLayersForLevel(s, Layer.LayerPlacement.Overlay);
        }


    }
}
