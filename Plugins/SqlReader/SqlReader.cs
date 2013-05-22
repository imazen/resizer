/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
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
using System.Collections.Specialized;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Configuration.Xml;
namespace ImageResizer.Plugins.SqlReader
{
    /// <summary>
    /// Specialized VirtualPathProvider that allows accessing database images as if they are on disk.
    /// </summary>
    public class SqlReaderPlugin : VirtualPathProvider, IPlugin, IIssueProvider, IVirtualImageProvider, IMultiInstancePlugin, IRedactDiagnostics
    {

        SqlReaderSettings s = null;
        public SqlReaderPlugin(NameValueCollection args) :base(){
            this.s = new SqlReaderSettings(args);
        }
        public SqlReaderPlugin(SqlReaderSettings s)
            : base()
        {
            this.s = s;
        }
        /// <summary>
        /// Provides all the configuration options for the plugin.
        /// </summary>
        public SqlReaderSettings Settings { get { return s; } }


        private bool _failedToRegisterVpp = false;
        /// <summary>
        /// True if the provider attempted to register itself as a VirtualPathProvider and failed due to limited security clearance.
        /// False if it did not attempt, or if it succeeded.
        /// </summary>
        public bool FailedToRegisterVpp {
            get { return _failedToRegisterVpp; }
        }

        /// <summary>
        /// Installes the plugin into the specified configuration. Once installed, it cannot be uninstalled.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            
            c.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender2, HttpContext context) {
                    //Only work with database images
                    //This allows us to resize database images without putting ".jpg" after the ID in the path.
                    if ((!s.RequireImageExtension || s.UntrustedData) && c.Pipeline.PreRewritePath.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase))
                        c.Pipeline.SkipFileTypeCheck = true; //Skip the file extension check. FakeExtensions will still be stripped.
            };
            

            c.Pipeline.RewriteDefaults += delegate(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
                //Only work with database images
                //Non-images will be served as-is
                //Cache all file types, whether they are processed or not.
                if (s.CacheUnmodifiedFiles && e.VirtualPath.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase))
                    e.QueryString["cache"] = ServerCacheMode.Always.ToString();


            };
            c.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
                //Only work with database images
                //If the data is untrusted, always re-encode each file.
                if (s.UntrustedData && e.VirtualPath.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase))
                    e.QueryString["process"] = ImageResizer.ProcessWhen.Always.ToString();

            };
            if (s.RegisterAsVirtualPathProvider) {
                try {
                    HostingEnvironment.RegisterVirtualPathProvider(this);
                } catch (SecurityException) {
                    this._failedToRegisterVpp = true;
                }
            }

            return this;
        }

        /// <summary>
        /// This plugin cannot be uninstalled as ASP.NET does not provide a 'undo' function for RegisterVirtualPathProvider
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            return false;
        }


        /// <summary>
        /// Called before any database op. Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public virtual void FireAuthorizeEvent(string id){
            s.FireBeforeAccess(id);
        }
        /// <summary>
        /// Returns a stream to the 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Stream GetStream(string id)
        {
            FireAuthorizeEvent(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ImageBlobQuery, conn);
                sc.CommandType = s.QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                sc.Parameters.Add(CreateIdParameter(id));
                SqlDataReader sdr = sc.ExecuteReader();
                using (sdr)
                {
                    if (!sdr.Read()) throw new FileNotFoundException("Failed to find the specified image " + id + " in the database"); //0 rows

                    return sdr.GetSqlBytes(0).Stream; //No connection required for the stream, all cloned in memory.
                }
            }
        }

        /// <summary>
        /// Creates a SQL paramater of the correct type for the row id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SqlParameter CreateIdParameter(string id)
        {
            SqlParameter sp = new SqlParameter("id", s.ImageIdType);
            if (IsIntKey) {
                sp.Size = 4;
                sp.Value = long.Parse(id);
            } else if (s.ImageIdType == System.Data.SqlDbType.UniqueIdentifier)
                sp.Value = new Guid(id);
            else if (IsStringKey)
                sp.Value = id;
            
            return sp;
        }
        /// <summary>
        /// Returns true if Settings.ImageIdType is a string type
        /// </summary>
        public bool IsStringKey {
            get {
                return s.IsStringType(s.ImageIdType);
            }
        }
        /// <summary>
        /// Returns true if Settings.ImageIdType  is an integer type
        /// </summary>
        public bool IsIntKey {
            get {
                return s.IsIntType(s.ImageIdType);
            }
        }


        /// <summary>
        /// Executes existsQuery, and returns true if the value is greater than 0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RowExists(string id){
            FireAuthorizeEvent(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ImageExistsQuery, conn);
                sc.CommandType = s.QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                sc.Parameters.Add(CreateIdParameter(id));
                int count = (int)sc.ExecuteScalar();
                if (count > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns DateTime.MinValue if there are no rows, or no values on the row.
        /// Executes ModifiedDateQuery, then returns the first non-null datetime value on the first row.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DateTime GetDateModifiedUtc(string id){
            FireAuthorizeEvent(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ModifiedDateQuery, conn);
                sc.Parameters.Add(CreateIdParameter(id));
                sc.CommandType = s.QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                SqlDataReader sdr = sc.ExecuteReader();
                using (sdr)
                {
                    if (!sdr.Read()) return DateTime.MinValue; //0 rows

                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        if (!sdr.IsDBNull(i) && sdr.GetValue(i) is DateTime)
                        {
                            DateTime val = (DateTime)sdr.GetValue(i);
                            if (val.Kind == DateTimeKind.Unspecified) val = DateTime.SpecifyKind(val, DateTimeKind.Utc);
                            return val; //Return the first non-null datetime instance in the row. Regardless of value.
                        }
                    }
                }
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Creates and returns a SqlConnection object for the database based on the configuration.
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetConnectionObj(){

            //First, try the connection string as a connection string key.
            if (System.Configuration.ConfigurationManager.ConnectionStrings[s.ConnectionString] != null)
                return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[s.ConnectionString].ConnectionString);

            //Second, try the .NET syntax
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (s.ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                string key = s.ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] != null)
                    return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString);
                else
                    throw new ImageResizer.ImageProcessingException("SqlReader: Failed to locate the named connection string '" + key + "' in web.config");

            }

            //Third, try it as an actual connection string
            return new SqlConnection(s.ConnectionString);
        }

        /// <summary>
        /// Returns a SqlCommand cache dependency using the modifiedQuery.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SqlCommand GetCacheDependencyQuery(string id){
            SqlCommand sc = new SqlCommand(s.ModifiedDateQuery, GetConnectionObj());
            sc.CommandType = s.QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
            sc.Parameters.Add(CreateIdParameter(id));
            return sc;
        }

        /// <summary>
        /// No initialization needed for this VPP
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        /// Supports int, string, and GUID IDs. Override this to modify ID parsing if you can't do it with rewrite rules.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public virtual string ParseIdFromVirtualPath(string virtualPath)
        {
            String checkPath = ImageResizer.Util.PathUtils.ResolveAppRelative(virtualPath);
            //Check for prefix
            if (!checkPath.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase)) return null;
            string id = checkPath.Substring(s.VirtualPathPrefix.Length); //Strip prefix
            //Strip slashes at beginning 
            id = id.TrimStart(new char[] { '/', '\\' });
            //Strip extension if not a string 
            if (!IsStringKey || s.StripFileExtension) {
                int length = id.LastIndexOf('.');
                if (length > -1) id = id.Substring(0, length);
            }
            //Can't be empty.
            if (id.Length < 1) return null;
            //Verify only valid characters present
            if (IsIntKey)
            {
                long val =0;
                if (!long.TryParse(id, out val)) return null; // not a valid integer
            }
            else if (s.ImageIdType == System.Data.SqlDbType.UniqueIdentifier)
            {
                try
                {
                    Guid test = new Guid(id);
                }
                catch
                {
                    return null; //Not a valid guid.
                }

            } else if (IsStringKey) {
                return id;
            }else {
                throw new ImageProcessingException("Only Integer, String, and GUID identifiers are suported by SqlReader");
            }
 
            return id;
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
        private bool IsPathVirtual(string virtualPath)
        {
            return ParseIdFromVirtualPath(virtualPath) != null;
        }
        /// <summary>
        /// VPP method - not for external use
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return !s.CheckForModifiedFiles || RowExists(ParseIdFromVirtualPath(virtualPath));
            }
            else
                return Previous.FileExists(virtualPath);
        }

        /// <summary>
        /// VPP method, not for external use
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))

                return new DatabaseFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }
        /// <summary>
        /// Returns true if the specified virtual file should be provided by this reader and it exists. False if this provider cannot/should not provide it, or it doesn't exist.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString) {
            return IsPathVirtual(virtualPath) && (!s.CheckForModifiedFiles || RowExists(ParseIdFromVirtualPath(virtualPath)));
        }

        /// <summary>
        /// Returns an IVirtualFile instance if the file exists in this provider.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString) {
            return IsPathVirtual(virtualPath) ? new DatabaseFile(virtualPath, this) : null;
        }

        /// <summary>
        /// VPP method, not for external use
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="virtualPathDependencies"></param>
        /// <param name="utcStart"></param>
        /// <returns></returns>
        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart)
        {
            if (IsPathVirtual(virtualPath))
                return new SqlCacheDependency(GetCacheDependencyQuery(ParseIdFromVirtualPath(virtualPath)));
            else
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        /// <summary>
        /// Provides the diagnostics system with a list of configuration issues
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();

            //1) Verify named connection strings exist
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (s != null && s.ConnectionString != null && s.ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                string key = s.ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] == null)
                    issues.Add(new Issue("SqlReader", "Failed to locate the named connection string '" + key + "' in web.config","", IssueSeverity.ConfigurationError));

            }

            if (FailedToRegisterVpp)
                issues.Add(new Issue("SqlReader", "Failed to register as VirtualPathProvider.",
                    "Only the image resizer will be able to access files located in SQL - other systems will not be able to.", IssueSeverity.Error));
            

            return issues;
        }



        public Configuration.Xml.Node RedactFrom(Node resizer) {
            foreach (Node n in resizer.queryUncached("plugins.add")) {
                if (n.Attrs["connectionString"] != null) n.Attrs.Set("connectionString", "[redacted]");
            }
            return resizer;
        }

    }

    /// <summary>
    /// Represents a blob stored in the database. Provides methods for verifying existence, opening a stream, and checking the modified date.
    /// Modified date and existence values are cached after the first query.
    /// </summary>
    public class DatabaseFile : VirtualFile, ImageResizer.Plugins.IVirtualFileWithModifiedDate, IVirtualFile
    {
        private string id;
        private SqlReaderPlugin provider;

        private Nullable<bool> _exists = null;
        private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Returns true if the row exists. 
        /// </summary>
        public bool Exists
        {
            get {
                if (_exists == null && !provider.Settings.CheckForModifiedFiles) return true;
                if (_exists == null) _exists =  provider.RowExists(id);
                return _exists.Value;
            }
        }

        public DatabaseFile(string virtualPath, SqlReaderPlugin provider)
            : base(virtualPath)
        {
            this.provider = provider;
            this.id = provider.ParseIdFromVirtualPath(virtualPath);

        }

        /// <summary>
        /// Returns a stream to the database blob associated with the id. Throws a FileNotFound exception if the row is missing. Allows Image404 to work properly.
        /// </summary>
        /// <returns></returns>
        public override Stream Open(){ return provider.GetStream(id);}

        /// <summary>
        /// Returns the last modified date of the row. Cached for performance.
        /// </summary>
        public DateTime ModifiedDateUTC{
            get{
                if (_fileModifiedDate == null && !provider.Settings.CheckForModifiedFiles) return DateTime.MinValue;
                if (_fileModifiedDate == null) _fileModifiedDate = provider.GetDateModifiedUtc(id);
                return _fileModifiedDate.Value;
            }
        }
      
    }
}