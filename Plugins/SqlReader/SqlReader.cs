/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 * Disclaimer of warranty and limitation of liability continued at http://nathanaeljones.com/11151_Image_Resizer_License
 **/

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
namespace fbs.ImageResizer.Plugins.SqlReader
{
    /// <summary>
    /// Specialized VirtualPathProvider that allows accessing database images as if they are on disk.
    /// </summary>
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class SqlReader : VirtualPathProvider
    {

        SqlReaderSettings s = null;
        public SqlReader(SqlReaderSettings s)
            : base()
        {
            this.s = s;
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
                    if (!sdr.Read()) return null; //0 rows

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
            SqlParameter sp = new SqlParameter("id", s.ImageIdType, 4);
            if (s.ImageIdType == System.Data.SqlDbType.Int || s.ImageIdType == System.Data.SqlDbType.TinyInt || s.ImageIdType == System.Data.SqlDbType.SmallInt ||
                s.ImageIdType == System.Data.SqlDbType.BigInt)
            {
                sp.Value = long.Parse(id);
            } else if (s.ImageIdType == System.Data.SqlDbType.UniqueIdentifier)
            {
                sp.Value = new Guid(id);
            }
            return sp;
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
        /// Supports int and GUID IDs. Override this to modify ID parsing if you can't do it with rewrite rules.
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
            //Strip extension.
            int length = id.LastIndexOf('.');
            if (length > -1) id = id.Substring(0, length);
            //Can't be empty.
            if (id.Length < 1) return null;
            //Verify only valid characters present
            if (s.ImageIdType == System.Data.SqlDbType.Int || s.ImageIdType == System.Data.SqlDbType.TinyInt || s.ImageIdType == System.Data.SqlDbType.SmallInt || 
                s.ImageIdType == System.Data.SqlDbType.BigInt)
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

            }
            else
            {
                throw new Exception("Only Integer and GUID identifiers are suported by the DatabaseVirtualPathProvider class");
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
    }


    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class DatabaseFile : VirtualFile, fbs.ImageResizer.IVirtualFileWithModifiedDate
    {
        private string id;
        private SqlReader provider;

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

        public DatabaseFile(string virtualPath, SqlReader provider)
            : base(virtualPath)
        {
            this.provider = provider;
            this.id = provider.getIdFromPath(virtualPath);

        }

        /// <summary>
        /// Returns a stream to the database blob associated with the id
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