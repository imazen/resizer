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
namespace DatabaseSampleCSharp
{
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class DatabaseVirtualPathProvider : VirtualPathProvider
    {

        string _pathPrefix = "~/databaseimages/";
        string _connectionString = null;
        string _binaryQueryString = 
            "SELECT Content FROM Images WHERE ImageID=@id";
        string _modifiedDateQuery = 
            "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
        string _existsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";

        private System.Data.SqlDbType idType = System.Data.SqlDbType.Int;

        public DatabaseVirtualPathProvider()
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
        public Stream getStream(string id)
        {
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(_binaryQueryString, conn);
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
            SqlParameter sp = new SqlParameter("id", idType, 4);
            if (idType == System.Data.SqlDbType.Int)
            {
                sp.Value = int.Parse(id);
            }
            else if (idType == System.Data.SqlDbType.UniqueIdentifier)
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
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(_existsQuery, conn);
                sc.Parameters.Add(getIdParameter(id));
                int count = (int)sc.ExecuteScalar();
                if (count > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns DateTime.MinValue if there are no rows, or no values on the row.
        /// Executes _modifiedDateQuery, then returns the first non-null datetime value on the first row.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DateTime getDateModifiedUtc(string id){
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(_modifiedDateQuery, conn);
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
            return new SqlConnection(_connectionString);
        }

        public SqlCommand GetCacheDependencyQuery(string id){
            SqlCommand sc = new SqlCommand(_modifiedDateQuery, GetConnectionObj());
            sc.Parameters.Add(getIdParameter(id));
            return sc;
        }

        protected override void Initialize()
        {

        }

        /// <summary>
        /// Supports int and GUID IDs.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public string getIdFromPath(string virtualPath)
        {
            String checkPath = VirtualPathUtility.ToAppRelative(virtualPath);
            //Check for prefix
            if (!checkPath.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase)) return null;
            string id = checkPath.Substring(_pathPrefix.Length); //Strip prefix
            //Strip slashes at beginning 
            id = id.TrimStart(new char[] { '/', '\\' });
            //Strip extension.
            int length = id.LastIndexOf('.');
            if (length > -1) id = id.Substring(0, length);
            //Can't be empty.
            if (id.Length < 1) return null;
            //Verify only valid characters present
            if (idType == System.Data.SqlDbType.Int)
            {
                int val =0;
                if (!int.TryParse(id, out val)) return null; // not a valid integer
            }
            else if (idType == System.Data.SqlDbType.UniqueIdentifier)
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
            //char[] chars = id.ToCharArray();
            //for (int i = 0; i < chars.Length; i++){
            //    if (!(Char.IsDigit(chars[i]) || Char.IsLetter(chars[i]) || chars[i] == '-')) return null; //Must be a letter or a digit.
            //}
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
        private DatabaseVirtualPathProvider provider;

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

        public DatabaseFile(string virtualPath, DatabaseVirtualPathProvider provider)
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