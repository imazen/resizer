/* Copyright (c) 2014 Imazen See license.txt for your rights. */
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
        /// <summary>
        /// Creates a new instance of the watermark plugin.
        /// </summary>
        public WatermarkPlugin() {
        }


        private ResizeSettings _defaultImageQuery = new ResizeSettings("scache=true");
        /// <summary>
        /// Default querystring parameters for all image watermarks.
        /// If not specified in the watermark configuration, defaults to
        /// "scache=true".
        /// </summary>
        public ResizeSettings DefaultImageQuery
        {
            get { return _defaultImageQuery; }
            set { _defaultImageQuery = value; }
        }

        ImageLayer _otherImages = new ImageLayer(null);
        /// <summary>
        /// When a &amp;watermark command does not specify a named preset, it is assumed to be a file name. 
        /// Set OtherImages.Path to the search folder. All watermark images (except for presets) must be in the root of the search folder. 
        /// The remainder of the settings affect how each watermark will be positioned and displayed.
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
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            this.OtherImages.ConfigInstance = c;
            _namedWatermarks = ParseWatermarks(c.getConfigXml().queryFirst("watermarks"), ref _defaultImageQuery, ref _otherImages);
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;
            return this;
        }

      /// <summary>
      /// Removes the plugin from the given configuration container
      /// </summary>
      /// <param name="c"></param>
      /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "watermark" };
        }

        protected Dictionary<string, IEnumerable<Layer>> ParseWatermarks(Node n, ref ResizeSettings defaultImageQuery, ref ImageLayer otherImageDefaults) {
            // Grab the defaultImageQuery value (if it exists) from the watermarks
            // node, so that we can apply them to any subsequent image watermarks.
            if (n != null && !string.IsNullOrEmpty(n.Attrs["defaultImageQuery"])) {
                defaultImageQuery = new ResizeSettings(n.Attrs["defaultImageQuery"]);
            }

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

                
                if (c.Name.Equals("otherimages", StringComparison.OrdinalIgnoreCase)) otherImageDefaults = new ImageLayer(c.Attrs, defaultImageQuery, this.c);
                if (c.Name.Equals("image", StringComparison.OrdinalIgnoreCase)) dict.Add(name, new Layer[] { new ImageLayer(c.Attrs, defaultImageQuery, this.c) });
                if (c.Name.Equals("text", StringComparison.OrdinalIgnoreCase)) dict.Add(name, new Layer[] {new TextLayer(c.Attrs) });
                if (c.Name.Equals("group", StringComparison.OrdinalIgnoreCase)) {
                    
                    List<Layer> layers = new List<Layer>();
                    if (c.Children != null) {
                        foreach (Node layer in c.Children) {
                            if (layer.Name.Equals("image", StringComparison.OrdinalIgnoreCase)) layers.Add(new ImageLayer(layer.Attrs, defaultImageQuery, this.c));
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

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            //Cache breaking
            string[] parts = WatermarksToUse(e.QueryString);
            if (parts == null) return;

            foreach (string w in parts)
            {
                if (NamedWatermarks.ContainsKey(w))
                {
                    IEnumerable<Layer> layers = NamedWatermarks[w];
                    foreach (Layer l in layers)
                    {
                        e.QueryString["watermark-cachebreak"] = (e.QueryString["watermark-cachebreak"] ?? "") + "_" + l.GetDataHash().ToString();
                    }
                }
            }
        }

        protected RequestedAction RenderLayersForLevel(ImageState s, Layer.LayerPlacement only) {
            string watermark;
            string[] parts = WatermarksToUse(s.settings, out watermark);
            Graphics g = s.destGraphics;
            if (parts == null || g == null) return RequestedAction.None;

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
                ImageLayer layer = this.CreateLayerFromOtherImages(watermark);

                if (layer != null) {
                    layer.RenderTo(s);
                } else {
                    this.LegacyDrawWatermark(s);
                }
            }

            return RequestedAction.None;
        }

        /// <summary>
        /// Creates an ImageLayer for the watermark based on OtherImages, if
        /// it exists.  If OtherImages does not exist, the watermark should be
        /// treated as a legacy one.
        /// </summary>
        /// <param name="watermarkPath">The path to the watermark image.</param>
        /// <returns>Returns a copy of OtherImages, modified to represent the
        /// watermark passed in <c>watermarkPath</c>, or <c>null</c> if
        /// OtherImages doesn't exist.</returns>
        private ImageLayer CreateLayerFromOtherImages(string watermarkPath)
        {
            if (watermarkPath.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1 ||
                watermarkPath.IndexOfAny(new char[] { '\\', '/' }) > -1) {
                throw new ArgumentException("Watermark value contained invalid file name characters: " + watermarkPath);
            }

            if (OtherImages != null && OtherImages.Path != null) {
                ImageLayer layer = OtherImages.Copy();

                //Is the watermark dir a physical path?
                char slash = layer.Path.Contains("/") ? '/' : '\\';
                layer.Path = layer.Path.TrimEnd(slash) + slash + watermarkPath.TrimStart(slash);

                //If it's a forward-slash, and we're in asp.net,  verify the file exists
                if (slash == '/' &&
                    HttpContext.Current != null &&
                    !c.Pipeline.FileExists(layer.Path, layer.ImageQuery)) {
                    return null;
                }

                return layer;
            }

            return null;
        }

        // Watermark images are loaded prior to the main image for performance
        // reasons.  The logic is thus:
        //
        //   1. Overlays tend to be smaller than primary images
        //
        //   2. Network/disk latency can cause image processing to 'pause' during
        //      a render stage, which is catastrophic to RAM use, as 200MB block
        //      of RAM may be held open for 1-10s instead of 200ms.
        //
        //   3. If we have a MemoryStream of overlay images before the primary
        //      image is even decoded, we theoretically limit the render phase
        //      time to 20% overhead (overlay jpeg decoding). Locking on a cached
        //      Bitmap instance is OK, as all DrawImage calls are serialized
        //      anyway. It doesn't address initial latency problems though, which
        //      can be bad during startup for concurrent similar requests.
        protected override void PreLoadImage(ref object source, ref string path, ref bool disposeSource, ref ResizeSettings settings) {
            // We can only cache in ASP.NET.
            if (HttpContext.Current == null) return;

            // We don't actually touch source, path, or disposeSource, since
            // we're not actually doing anything about the source image itself.
            string watermark;
            string[] parts = WatermarksToUse(settings, out watermark);
            if (parts == null) return;

            bool foundPart = false;

            foreach (string w in parts) {
                if (NamedWatermarks.ContainsKey(w)) {
                    IEnumerable<Layer> layers = NamedWatermarks[w];
                    foreach (Layer l in layers) {
                        ImageLayer il = l as ImageLayer;
                        if (il != null) {
                            il.PreFetchImage();
                        }
                        foundPart = true;
                    }
                }
            }

            if (!foundPart) {
                ImageLayer layer = this.CreateLayerFromOtherImages(watermark);

                if (layer != null) {
                    layer.PreFetchImage();
                } else {
                    this.LegacyPreFetchWatermark(settings);
                }
            }
        }

        protected override RequestedAction PostRenderBackground(ImageState s) {
            return RenderLayersForLevel(s, Layer.LayerPlacement.Background);
        }

        protected override RequestedAction RenderOverlays(ImageState s) {
            return RenderLayersForLevel(s, Layer.LayerPlacement.Overlay);
        }


        private static string[] WatermarksToUse(NameValueCollection nvc) {
            string watermark;
            return WatermarksToUse(nvc, out watermark);
        }

        private static string[] WatermarksToUse(NameValueCollection nvc, out string watermark) {
            watermark = nvc["watermark"];
            if (string.IsNullOrEmpty(watermark)) return null;

            string[] parts = watermark.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }
    }
}
