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
namespace DatabaseSampleCSharp
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
            _connectionString = ConfigurationManager.ConnectionStrings["database"].ConnectionString;
        }
        /// <summary>
        /// Returns a stream to the 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Stream getStream(string virtualPath)
        {

            Stopwatch sw = new Stopwatch();
            sw.Start();
            // Create the resultBitmap object which contains merged bitmap, 
            // and the currentBitmap object which contains current bitmap during iteration. 
            // These object enable you to operate with layers.
            Aurigma.GraphicsMill.Bitmap resultBitmap = new Aurigma.GraphicsMill.Bitmap();
            Aurigma.GraphicsMill.Bitmap currentBitmap = new Aurigma.GraphicsMill.Bitmap();

            // Create advanced PSD reader object to read .psd files.
            Aurigma.GraphicsMill.Codecs.AdvancedPsdReader psdReader = new Aurigma.GraphicsMill.Codecs.AdvancedPsdReader(getPhysicalPath(virtualPath));


            // Load the background layer which you will put other layers on. 
            // Remember that the layer on zero position should be skiped 
            // because it contains merged bitmap.
            Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame frame;
            frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(1);
            frame.GetBitmap(resultBitmap);

            string showLayersWith = "12288";
            if (HttpContext.Current.Request.QueryString["showlayerswith"] != null) showLayersWith = HttpContext.Current.Request.QueryString["showlayerswith"];

            //This code merges the rest layers with the background layer one by one.
            for (int i = 2; i < psdReader.FrameCount; i++)
            {
                frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(i);

                // Do not forget to verify the unknown layer type.  
                if (frame.Type != Aurigma.GraphicsMill.Codecs.PsdFrameType.Unknown)
                {
                    bool showFrame =  frame.Visible; 
                    if (!showFrame){
                        if (i < 6 || frame.Name.Contains(showLayersWith)) showFrame = true;
                    }
                    if (showFrame){
                        // Extract the current image from the layer.
                        frame.GetBitmap(currentBitmap);

                        // Draw current layer on the result bitmap. 
                        // Also check out if the layer is visible or not.
                        // If the layer is invisible we skip it.
                        currentBitmap.Draw(resultBitmap, frame.Left, frame.Top, frame.Width, frame.Height, Aurigma.GraphicsMill.Transforms.CombineMode.Alpha, 1, Aurigma.GraphicsMill.Transforms.InterpolationMode.HighQuality);
                    }
                }
            }
            MemoryStream ms = new MemoryStream();
            // Save the result bitmap into file. 
            resultBitmap.Save(ms, new PngEncoderOptions());
            ms.Seek(0, SeekOrigin.Begin);
            // Clean up.
            psdReader.Close();
            sw.Stop();

            return ms;
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

        string getPhysicalPath(string virtualPath)
        {
            int ix = virtualPath.ToLowerInvariant().LastIndexOf(".psd");
            return HttpContext.Current.Request.MapPath(virtualPath.Substring(0, ix + 4));
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
                return new PsdFile(virtualPath, this);
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
    public class PsdFile : VirtualFile, fbs.ImageResizer.IVirtualFileWithModifiedDate
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

        public PsdFile(string virtualPath, PsdProvider provider)
            : base(virtualPath)
        {
            this.provider = provider;
        }

        /// <summary>
        /// Returns a stream to the database blob associated with the id
        /// </summary>
        /// <returns></returns>
        public override Stream Open(){ return provider.getStream(this.VirtualPath);}

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