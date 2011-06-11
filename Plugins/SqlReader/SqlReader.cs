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
namespace ImageResizer.Plugins.SqlReader
{
    /// <summary>
    /// Specialized VirtualPathProvider that allows accessing database images as if they are on disk.
    /// </summary>
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class SqlReaderPlugin : VirtualPathProvider, IPlugin, IIssueProvider
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

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            HostingEnvironment.RegisterVirtualPathProvider(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            return false;
        }


        /// <summary>
        /// Called before any database op. Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public virtual void authorize(string id){
            s.FireBeforeAccess(id);
        }
        /// <summary>
        /// Returns a stream to the 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Stream getStream(string id)
        {
            authorize(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ImageBlobQuery, conn);
                sc.Parameters.Add(getIdParameter(id));
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
        public SqlParameter getIdParameter(string id)
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
        /// Returns true if the image ID is a string type
        /// </summary>
        public bool IsStringKey {
            get {
                return s.IsStringType(s.ImageIdType);
            }
        }

        public bool IsIntKey {
            get {
                return s.IsIntType(s.ImageIdType);
            }
        }


        /// <summary>
        /// Executes _existsQuery, and returns true if the value is greater than 0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool rowExists(string id){
            authorize(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ImageExistsQuery, conn);
                sc.Parameters.Add(getIdParameter(id));
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
        public DateTime getDateModifiedUtc(string id){
            authorize(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(s.ModifiedDateQuery, conn);
                sc.Parameters.Add(getIdParameter(id));
                SqlDataReader sdr = sc.ExecuteReader();
                using (sdr)
                {
                    if (!sdr.Read()) return DateTime.MinValue; //0 rows

                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        if (!sdr.IsDBNull(i) && sdr.GetValue(i) is DateTime)
                        {
                            return (DateTime)sdr.GetValue(i); //Return the first non-null datetime instance in the row. Regardless of value.
                        }
                    }
                }
            }
            return DateTime.MinValue;
        }

        public SqlConnection GetConnectionObj(){
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (s.ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                string key = s.ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] != null)
                    return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString);
                else
                    throw new ImageResizer.ImageProcessingException("SqlReader: Failed to locate the named connection string '" + key + "' in web.config");

            }
            return new SqlConnection(s.ConnectionString);
        }

        public SqlCommand GetCacheDependencyQuery(string id){
            SqlCommand sc = new SqlCommand(s.ModifiedDateQuery, GetConnectionObj());
            sc.Parameters.Add(getIdParameter(id));
            return sc;
        }

        protected override void Initialize()
        {

        }

        /// <summary>
        /// Supports int, string, and GUID IDs. Override this to modify ID parsing if you can't do it with rewrite rules.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public virtual string getIdFromPath(string virtualPath)
        {
            String checkPath = VirtualPathUtility.ToAppRelative(virtualPath);
            //Check for prefix
            if (!checkPath.StartsWith(s.PathPrefix, StringComparison.InvariantCultureIgnoreCase)) return null;
            string id = checkPath.Substring(s.PathPrefix.Length); //Strip prefix
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
            return getIdFromPath(virtualPath) != null;
        }

        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return rowExists(getIdFromPath(virtualPath));
            }
            else
                return Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))

                return new DatabaseFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart)
        {
            if (IsPathVirtual(virtualPath))
                return new SqlCacheDependency(GetCacheDependencyQuery(getIdFromPath(virtualPath)));
            else
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();

            //1) Verify named connection strings exist
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (s != null && s.ConnectionString != null && s.ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                string key = s.ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] == null)
                    issues.Add(new Issue("SqlReader: Failed to locate the named connection string '" + key + "' in web.config", IssueSeverity.ConfigurationError));

            }

            return issues;
        }
    }


    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class DatabaseFile : VirtualFile, ImageResizer.Plugins.IVirtualFileWithModifiedDate
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
                if (_exists == null) _exists =  provider.rowExists(id);
                return _exists.Value;
            }
        }

        public DatabaseFile(string virtualPath, SqlReaderPlugin provider)
            : base(virtualPath)
        {
            this.provider = provider;
            this.id = provider.getIdFromPath(virtualPath);

        }

        /// <summary>
        /// Returns a stream to the database blob associated with the id. Throws a FileNotFound exception if the row is missing. Allows Image404 to work properly.
        /// </summary>
        /// <returns></returns>
        public override Stream Open(){ return provider.getStream(id);}

        /// <summary>
        /// Returns the last modified date of the row. Cached for performance.
        /// </summary>
        public DateTime ModifiedDateUTC{
            get{
                if (_fileModifiedDate == null) _fileModifiedDate = provider.getDateModifiedUtc(id);
                return _fileModifiedDate.Value;
            }
        }
      
    }
}