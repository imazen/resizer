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
using Aurigma.GraphicsMill.Codecs;
using System.Diagnostics;
using fbs.ImageResizer;
using fbs;
using System.Collections.Specialized;
namespace PsdRenderer
{
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class PsdProvider : VirtualPathProvider
    {

        string _pathPrefix = "~/databaseimages/";
        string _connectionString = null;
        string _binaryQueryString = 
            "SELECT Content FROM Images WHERE ImageID=@id";
        string _modifiedDateQuery = 
            "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
        string _existsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";

        private System.Data.SqlDbType idType = System.Data.SqlDbType.Int;

        public PsdProvider()
            : base()
        {
            //Override connection string here
            //_connectionString = ConfigurationManager.ConnectionStrings["database"].ConnectionString;
        }
        /// <summary>
        /// Returns the renderer object selected in the querystring
        /// </summary>
        /// <returns></returns>
        public static IPsdRenderer GetSelectedRenderer(NameValueCollection queryString)
        {
            //Renderer object
            IPsdRenderer renderer = null;
            //The querystring-specified renderer name
            string sRenderer = null;
            if (queryString["renderer"] != null) sRenderer = queryString["renderer"].ToLowerInvariant();
            //Build the correct renderer
            if (("graphicsmill").Equals(sRenderer))
                renderer = new GraphicsMillRenderer();
            else
                renderer = new PsdPluginRenderer();
            return renderer;
        }

        /// <summary>
        /// Creates a callback that can be used to filter layer visibility.
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        private static RenderLayerDelegate BuildLayerCallback(NameValueCollection queryString)
        {
            //Which layers do we show?
            string showLayersWith = "1";
            if (queryString["showlayerswith"] != null) showLayersWith = queryString["showlayerswith"];

            return delegate(int index, string name, bool visibleNow)
            {
                if (visibleNow) return true;
                return (index < 6 || name.Contains(showLayersWith));
            };
        }

        public Stream getStream(string virtualPath)
        {
            return getStream(virtualPath, HttpContext.Current.Request.QueryString);
        }
        /// <summary>
        /// Returns a stream to the 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Stream getStream(string virtualPath, NameValueCollection queryString)
        {

            Stopwatch sw = new Stopwatch();
            sw.Start();

            System.Drawing.Bitmap b = getBitmap(virtualPath, queryString);
            //Memory stream for encoding the file
            MemoryStream ms = new MemoryStream();
            //Encode image to memory stream, then seek the stream to byte 0
            using (b)
            {
                //Use whatever settings appear in the URL 
                ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromExtension(System.IO.Path.GetExtension(virtualPath)),queryString);
                ios.SaveImage(ms, b);
                ms.Seek(0, SeekOrigin.Begin); //Reset stream for reading
            }

            sw.Stop();
            trace("Total time, including encoding: " + sw.ElapsedMilliseconds.ToString() + "ms");

            return ms;
        }
        public System.Drawing.Bitmap getBitmap(string virtualPath){
            return getBitmap(virtualPath,HttpContext.Current.Request.QueryString);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap getBitmap(string virtualPath, NameValueCollection queryString)
        {
            //Renderer object
            IPsdRenderer renderer = GetSelectedRenderer(queryString);

            //Bitmap we will render to
            System.Drawing.Bitmap b = null;

            MemCachedFile file = MemCachedFile.GetCachedFile(getPhysicalPath(virtualPath));
            using (Stream s = file.GetStream())
            {
                //Time just the parsing/rendering
                Stopwatch swRender = new Stopwatch();
                swRender.Start();

                IList<ITextLayer> textLayers = null;
                //Use the selected renderer to parse the file and compose the layers, using this delegate callback to determine which layers to show.
                b = renderer.Render(s, out textLayers, BuildLayerCallback(queryString));

                //Save text layers for later use
                file.setSubkey("textlayers_" + renderer.ToString(), textLayers);

                //How fast?
                swRender.Stop();
                trace("Using encoder " + renderer.ToString() + ", rendering stream to a composed Bitmap instance took " + swRender.ElapsedMilliseconds.ToString() + "ms");
            }
            return b;
        }


        private static void trace(string msg)
        {
            if (HttpContext.Current == null)
                System.Diagnostics.Debug.Write(msg);
            else
                HttpContext.Current.Trace.Write(msg);
        }

        public static IList<ITextLayer> getVisibleTextLayers(string virtualPath, NameValueCollection queryString)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //Renderer object
            IPsdRenderer renderer = GetSelectedRenderer(queryString);
            //File
            MemCachedFile file = MemCachedFile.GetCachedFile(getPhysicalPath(virtualPath));
            //key
            string dataKey = "textlayers_" + renderer.ToString();
           
            //Try getting from the cache first
            IList<ITextLayer> layers =file.getSubkey(dataKey) as IList<ITextLayer>;
            if (layers == null){
                //Time just the parsing
                Stopwatch swRender = new Stopwatch();
                swRender.Start();

                layers = renderer.GetTextLayers(file.GetStream());
                //Save to cache for later
                file.setSubkey(dataKey,layers);

                //How fast?
                swRender.Stop();
                trace("Using decoder " + renderer.ToString() + ",parsing file and enumerating layers took " + swRender.ElapsedMilliseconds.ToString() + "ms");
            }


            //Now, time to filter layers to those that would be showing on the image right now.
            IList<ITextLayer> filtered = new List<ITextLayer>();
            
            //Generate a callback just like the one used in the renderer for filtering
            RenderLayerDelegate callback = BuildLayerCallback(queryString);

            for (int i = 0; i < layers.Count; i++){
                if (callback(layers[i].Index, layers[i].Name, layers[i].Visible))
                {
                    filtered.Add(layers[i]);
                }
            }
            
            sw.Stop();
            trace("Total time for enumerating, including file reading: " + sw.ElapsedMilliseconds.ToString() + "ms");
            return filtered;

        }

        /// <summary>
        /// Returns DateTime.MinValue if there are no rows, or no values on the row.
        /// Executes _modifiedDateQuery, then returns the first non-null datetime value on the first row.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DateTime getDateModifiedUtc(string virtualPath){
            return System.IO.File.GetLastWriteTimeUtc(getPhysicalPath(virtualPath));
        }

        public SqlConnection GetConnectionObj(){
            return new SqlConnection(_connectionString);
        }

        protected override void Initialize()
        {

        }


        /// <summary>
        ///   Determines whether a specified virtual path is within
        ///   the virtual file system.
        /// </summary>
        /// <param name="virtualPath">An absolute virtual path.</param>
        /// <returns>
        ///   true if the virtual path is within the 
        ///   virtual file sytem; otherwise, false.
        /// </returns>
        bool IsPathVirtual(string virtualPath)
        {
            return (System.IO.Path.GetFileName(virtualPath).ToLowerInvariant().Contains(".psd."));
        }

        static string  getPhysicalPath(string virtualPath)
        {
            int ix = virtualPath.ToLowerInvariant().LastIndexOf(".psd");
            string str = virtualPath.Substring(0, ix + 4);
            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.MapPath(str);
            }
            else
            {
                return str.TrimStart('~','/').Replace('/','\\');
            }
        }


        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return System.IO.File.Exists(getPhysicalPath(virtualPath));
            }
            else
                return Previous.FileExists(virtualPath);
        }

        bool PSDExists(string virtualPath)
        {
            return IsPathVirtual(virtualPath) && System.IO.File.Exists(getPhysicalPath(virtualPath));
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (PSDExists(virtualPath))
                return new PsdVirtualFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart)
        {
            //Maybe the database is also involved? 
            return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }
    }


    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class PsdVirtualFile : VirtualFile, IVirtualFileWithModifiedDate, IVirtualBitmapFile
    {
  
        private PsdProvider provider;

        private Nullable<bool> _exists = null;
        private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Returns true if the row exists. 
        /// </summary>
        public bool Exists
        {
            get {
                if (_exists == null) _exists = provider.FileExists(this.VirtualPath);
                return _exists.Value;
            }
        }

        public PsdVirtualFile(string virtualPath, PsdProvider provider)
            : base(virtualPath)
        {
            this.provider = provider;
        }

        /// <summary>
        /// Returns a stream of the encoded file bitmap using the current request querystring.
        /// </summary>
        /// <returns></returns>
        public override Stream Open(){ return provider.getStream(this.VirtualPath);}
        /// <summary>
        /// Returns a composed bitmap of the file using request querystring paramaters.
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap GetBitmap() { return provider.getBitmap(this.VirtualPath); }

        /// <summary>
        /// Returns the last modified date of the row. Cached for performance.
        /// </summary>
        public DateTime ModifiedDateUTC{
            get{
                if (_fileModifiedDate == null) _fileModifiedDate = provider.getDateModifiedUtc(this.VirtualPath);
                return _fileModifiedDate.Value;
            }
        }
      
    }
}