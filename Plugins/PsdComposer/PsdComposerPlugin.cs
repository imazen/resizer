using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Hosting;
using System.Security.Permissions;
using System.Web.Caching;
using System.IO;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using PhotoshopFile;
using PhotoshopFile.Text;
using ImageResizer.Plugins;
using ImageResizer.Encoding;
using ImageResizer.Configuration;
using System.Globalization;
namespace ImageResizer.Plugins.PsdComposer
{
    /// <summary>
    /// Allows you to edit PSD files (hide/show layers, change text layer contents, apply certain effects), and render them to jpeg, gif, or png dynamically. Works as an IVirtualImageProvider, so you can post-process the composed result with any of the other plugins or commands.
    /// </summary>
    public class PsdComposerPlugin : IPlugin, IVirtualImageProvider, IQuerystringPlugin, IFileExtensionPlugin
    {
        /// <summary>
        /// Creates a new instance of PsdComposer
        /// </summary>
        public PsdComposerPlugin(): base()  { }

        internal Config c;
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            this.c = c;
            this.c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Returns true if the specified file and querystring indicate a PSD composition request
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString) {
            return IsPathPSDToCompose(virtualPath,queryString) && c.Pipeline.FileExists(StripFakeExtension(virtualPath),new NameValueCollection());
        }
        
        /// <summary>
        /// Returns a virtual file instance for the specified specified file and querystring, if they indicate a PSD composition request. 
        /// Otherwise, null is returned.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString) {
            if (IsPathPSDToCompose(virtualPath, queryString) && c.Pipeline.FileExists(StripFakeExtension(virtualPath), new NameValueCollection()))
                return new PsdVirtualFile(virtualPath, queryString, this);
            else
                return null;
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return PsdCommandBuilder.GetSupportedQuerystringKeys();
        }
        /// <summary>
        /// Additional file types this plugin adds support for decoding.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedFileExtensions() {
            return new string[] { "psd" };
        }


        protected bool isStrictMode()
        {
            return c.get("psdcomposer.strictMode", true);
        }

        /// <summary>
        /// Returns the renderer object selected in the querystring
        /// </summary>
        /// <returns></returns>
        protected IPsdRenderer GetSelectedRenderer(NameValueCollection queryString)
        {
            return new PsdPluginRenderer(); //There is only ONE renderer now - The Agurigma one was horrible
        }

 
        /// <summary>
        /// Returns a stream to the composed file, encoded in the format requested by the querystring or fake extension
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public Stream ComposeStream(string virtualPath, NameValueCollection queryString) {

            Stopwatch sw = new Stopwatch();
            sw.Start();

        
            System.Drawing.Bitmap b = ComposeBitmap(virtualPath, queryString);
            //Memory stream for encoding the file
            MemoryStream ms = new MemoryStream();
            //Encode image to memory stream, then seek the stream to byte 0
            using (b) {
                //Use whatever settings appear in the URL 
                IEncoder encoder = c.Plugins.GetEncoder(new ImageResizer.ResizeSettings(queryString), virtualPath);
                encoder.Write(b, ms);
                ms.Seek(0, SeekOrigin.Begin); //Reset stream for reading
            }


            sw.Stop();
            trace("Total time, including encoding: " + sw.ElapsedMilliseconds.ToString() + "ms");

            return ms;
        }
 
        /// <summary>
        /// Returns a Bitmap instance of the composed result
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap ComposeBitmap(string virtualPath, NameValueCollection queryString) {
            //Renderer object
            IPsdRenderer renderer = GetSelectedRenderer(queryString);

            //Bitmap we will render to
            System.Drawing.Bitmap b = null;

            MemCachedFile file = MemCachedFile.GetCachedVirtualFile(StripFakeExtension(virtualPath),c.Pipeline, new NameValueCollection());
            using (Stream s = file.GetStream()) {
                //Time just the parsing/rendering
                Stopwatch swRender = new Stopwatch();
                swRender.Start();

                IList<IPsdLayer> layers = null;
                Size size = Size.Empty;
                //Use the selected renderer to parse the file and compose the layers, using this delegate callback to determine which layers to show.
                b = renderer.Render(s, out layers, out size, BuildLayerCallback(queryString), BuildModifyLayerCallback(queryString));

                //Save layers & size for later use
                file.SetSubkey("layers_" + renderer.ToString(), layers);
                file.SetSubkey("size_" + renderer.ToString(), size);

                //How fast?
                swRender.Stop();
                trace("Using encoder " + renderer.ToString() + ", rendering stream to a composed Bitmap instance took " + swRender.ElapsedMilliseconds.ToString() + "ms");
            }


            return b;
        }

        /// <summary>
        /// Returns the size of the PSD 
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public Size GetPsdDimensions(string virtualPath, NameValueCollection queryString) {
            return GetFileMetadata(virtualPath, queryString).Key;
        }

        /// <summary>
        /// Returns a collection of all the layers for the specified file (memcached)
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IList<IPsdLayer> GetAllLayers(string virtualPath, NameValueCollection queryString) {
            return GetFileMetadata(virtualPath, queryString).Value;
        }
        /// <summary>
        /// Returns a collection of all the layers for the specified file and the size of the file (memcached)
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        protected KeyValuePair<Size,IList<IPsdLayer>> GetFileMetadata(string virtualPath, NameValueCollection queryString) {
            Stopwatch sw = new Stopwatch();
            sw.Start();


            //Renderer object
            IPsdRenderer renderer = GetSelectedRenderer(queryString);
            //File
            MemCachedFile file = MemCachedFile.GetCachedVirtualFile(StripFakeExtension(virtualPath), c.Pipeline, new NameValueCollection());
            //key
            string layersKey = "layers_" + renderer.ToString();
            string sizeKey = "size_" + renderer.ToString();

            //Try getting from the cache first
            IList<IPsdLayer> layers = file.GetSubkey(layersKey) as IList<IPsdLayer>;
            Size size = file.GetSubkey(sizeKey) is Size ? (Size)file.GetSubkey(sizeKey) : Size.Empty;
            if (layers == null) {
                //Time just the parsing
                Stopwatch swRender = new Stopwatch();
                swRender.Start();

                layers = renderer.GetLayersAndSize(file.GetStream(), out size);

                //Save to cache for later
                file.SetSubkey(layersKey, layers);
                file.SetSubkey(sizeKey, size);
                //How fast?
                swRender.Stop();
                trace("Using decoder " + renderer.ToString() + ",parsing file and enumerating layers took " + swRender.ElapsedMilliseconds.ToString() + "ms");
            }


            sw.Stop();
            trace("Total time for enumerating, including file reading: " + sw.ElapsedMilliseconds.ToString(NumberFormatInfo.InvariantInfo) + "ms");
            return new KeyValuePair<Size,IList<IPsdLayer>>(size,layers);
        }

        /// <summary>
        /// Returns a collection of all visible text layers for the file (memcached). Useful for building image maps
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IList<IPsdLayer> GetVisibleTextLayers(string virtualPath, NameValueCollection queryString) {

            //Get all layers
            IList<IPsdLayer> layers = GetAllLayers(virtualPath, queryString);


            Stopwatch sw = new Stopwatch();
            sw.Start();
            //Now, time to filter layers to those that would be showing on the image right now.
            IList<IPsdLayer> filtered = new List<IPsdLayer>();

            //Generate a callback just like the one used in the renderer for filtering
            ShowLayerDelegate callback = BuildLayerCallback(queryString);

            for (int i = 0; i < layers.Count; i++) {
                if (layers[i].IsTextLayer && callback(layers[i].Index, layers[i].Name, layers[i].Visible)) {
                    filtered.Add(layers[i]);
                }
            }

            sw.Stop();
            trace("Time for filtering layers: " + sw.ElapsedMilliseconds.ToString() + "ms");
            return filtered;

        }


        /// <summary>
        /// Creates a callback that can be used to filter layer visibility.
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        private ShowLayerDelegate BuildLayerCallback(NameValueCollection queryString)
        {
            //Which layers do we show?
            PsdCommandSearcher searcher = new PsdCommandSearcher(new PsdCommandBuilder(queryString));

            return delegate(int index, string name, bool visibleNow)
            {
                //Exclude Layer Group layers, 		Name	"</Layer group>"	string
                if ("</Layer group>".Equals(name, StringComparison.OrdinalIgnoreCase)) return false;
                if (name.IndexOf('<') > -1) return false;
                Nullable<bool> show = (searcher.getVisibility(name));
                if (show == null) return visibleNow;
                else return show.Value;
            };
        }
        /// <summary>
        /// Mixes the specified color (using the included alpha value as a weight) into the bitmap, leaving the bitmap's original alpha values untouched.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="c"></param>
        private void ColorBitmap(Bitmap b, Color c)
        {
            double weightedR = c.R * c.A;
            double weightedG = c.G * c.A;
            double weightedB = c.B * c.A;
            double originalWeight = 255 - c.A;

            int width = b.Width; int height = b.Height;
            BitmapData bd = b.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, b.PixelFormat);
            unsafe
            {
                byte* pCurrRowPixel = (byte*)bd.Scan0.ToPointer();
                for (int y = 0; y < height; y++)
                {
                    int rowIndex = y * width;
                    PhotoshopFile.ImageDecoder.PixelData* pCurrPixel = (PhotoshopFile.ImageDecoder.PixelData*)pCurrRowPixel;
                    for (int x = 0; x < width; x++)
                    {
                        pCurrPixel->Red = (byte)(((double)pCurrPixel->Red * originalWeight + weightedR) / 255);
                        pCurrPixel->Green = (byte)(((double)pCurrPixel->Green * originalWeight + weightedG) / 255);
                        pCurrPixel->Blue = (byte)(((double)pCurrPixel->Blue * originalWeight + weightedB) / 255);
                        pCurrPixel += 1;
                    }
                    pCurrRowPixel += bd.Stride;
                }
            }

            b.UnlockBits(bd);
        }

        private ComposeLayerDelegate BuildModifyLayerCallback(NameValueCollection queryString)
        {
            PsdCommandSearcher searcher = new PsdCommandSearcher(new PsdCommandBuilder(queryString));

            return delegate(Graphics g, Bitmap b, object layer)
            {
                PhotoshopFile.Layer l = (PhotoshopFile.Layer)layer;
                //See if this layer is supposed to be re-colored.
                Nullable<Color> color = searcher.getColor(l.Name);

                if (b == null && l.Rect.X == 0 && l.Rect.Y == 0 && l.Rect.Width == 0 && l.Rect.Height == 0)
                {
                    return; //This layer has no size, it is probably a layer group.
                }

                //See if we need to re-draw this text layer
                Nullable<bool> redraw = searcher.getRedraw(l.Name);
                if (redraw != null && redraw == true)
                {
                    //Verify it has text layer information:
                    bool hasText = false;
                    foreach (PhotoshopFile.Layer.AdjustmentLayerInfo lInfo in  l.AdjustmentInfo)
                        if (lInfo.Key.Equals("TySh", StringComparison.Ordinal)){ hasText=true; break;}


                    if (hasText) {
                        //Re-draw the text directly, ignoring the bitmap
                        var tlr = new TextLayerRenderer(l);
                        tlr.IgnoreMissingFonts = !isStrictMode();
                        tlr.Render(g, color, searcher.getReplacementText(l.Name));
                        return;
                    }
                }
                
                    
                    if (b == null && !isStrictMode()) return; //Skip drawing layers that have no bitmap data.
                    if (b == null) throw new Exception("No bitmap data found for layer " + l.Name);
                    //Draw the existing bitmap
                    //Blend color into bitmap
                    if (color != null) ColorBitmap(b, color.Value);
                    //Draw image
                    g.DrawImage(b, l.Rect);
                
            };
        }


        private static void trace(string msg)
        {
            if (HttpContext.Current == null)
                System.Diagnostics.Debug.Write(msg);
            else
                HttpContext.Current.Trace.Write(msg);
        }


   
        /// <summary>
        /// True if the file is a .psd.jpeg, .psd.png, etc file.
        /// </summary>
        protected bool IsPathPSDToCompose(string virtualPath,  NameValueCollection queryString = null)
        {
            string fileName = System.IO.Path.GetFileName(virtualPath); //Exclude the folders, just looking at the filename here.
            int psd = fileName.IndexOf(".psd",StringComparison.OrdinalIgnoreCase);
            if (psd > -1){
                //We always take the .psd. syntax
                if (fileName.IndexOf(".psd.",StringComparison.OrdinalIgnoreCase) > -1) return true;

                if (queryString == null) queryString = c.Pipeline.ModifiedQueryString;
                //But we only grab the .psd syntax if we detect our commands
                foreach(string s in PsdCommandBuilder.GetSupportedQuerystringKeys()){
                    if (!string.IsNullOrEmpty(queryString[s])) return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Strips the .psd.jpg to .psd, converts to physical physicalPath
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        internal static string StripFakeExtension(string virtualPath) {
            //Trim everything after .psd
            int ix = virtualPath.ToLowerInvariant().LastIndexOf(".psd");
            if (ix < 0) return virtualPath;
            return  virtualPath.Substring(0, ix + 4);
        }


        internal static NameValueCollection QueryStringMinusTriggers(NameValueCollection qs) {
            var nvc = new NameValueCollection(qs);
            foreach (string s in PsdCommandBuilder.GetSupportedQuerystringKeys())
                nvc.Remove(s);
            return nvc;
        }

  
    }


    public class PsdVirtualFile : IVirtualFile, IVirtualBitmapFile
    {
  
        private PsdComposerPlugin provider;

        private Nullable<bool> _exists = null;
        //private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Returns true if the row exists. 
        /// </summary>
        public bool Exists
        {
            get {
                if (_exists == null) _exists = provider.FileExists(this.VirtualPath, this.Query);
                return _exists.Value;
            }
        }

        public PsdVirtualFile(string virtualPath, NameValueCollection query, PsdComposerPlugin provider)
        {
            this.provider = provider;
            this._virtualPath = virtualPath;
            this._query = query;

        }

        private string _virtualPath = null;

        public string VirtualPath {
            get { return _virtualPath; }
        }
        private NameValueCollection _query;

        public NameValueCollection Query {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Returns a stream of the encoded file bitmap using the current request querystring.
        /// </summary>
        /// <returns></returns>
        public  Stream Open() { return provider.ComposeStream(this.VirtualPath, this.Query); }
        /// <summary>
        /// Returns a composed bitmap of the file using request querystring paramaters.
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap GetBitmap() { return provider.ComposeBitmap(this.VirtualPath, this.Query); }

    }
}