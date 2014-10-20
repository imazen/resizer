/* Copyright (c) 2014 Imazen See license.txt for your rights. */
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
using ImageResizer.Storage;
using ImageResizer.ExtensionMethods;
using System.Data;
using System.Threading.Tasks;
namespace ImageResizer.Plugins.SqlReader
{
    /// <summary>
    /// Specialized VirtualPathProvider that allows accessing database images as if they are on disk.
    /// </summary>
    public class SqlReaderPlugin : BlobProviderBase, IVirtualImageProviderVppCaching, IIssueProvider, IMultiInstancePlugin
    {

        public SqlReaderPlugin():base(){
            StripFileExtension = true;
            QueriesAreStoredProcedures = false;
            ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
            ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
            VirtualFilesystemPrefix = "~/databaseimages";
        }
        public SqlReaderPlugin(NameValueCollection args)
            : this()
        {
            LoadConfiguration(args);
            ConnectionString = args.GetAsString("connectionString",this.ConnectionString);
            ImageIdType = args.Get("idType", this.ImageIdType);
            ImageBlobQuery = args.GetAsString("blobQuery", this.ImageBlobQuery);
            ModifiedDateQuery = args.GetAsString("modifiedQuery", this.ModifiedDateQuery);
            StripFileExtension = !args.Get("extensionPartOfId", false);
            QueriesAreStoredProcedures = args.Get("queriesAreStoredProcedures", false);
        }

        public delegate void AuthorizeEvent(String id);

        /// <summary>
        /// Called before accessing any row in the database. The row ID is passed as a string argument.
        /// If the current user should not access the row, throw an HttpException(403, "message")
        /// </summary>
        public event AuthorizeEvent BeforeAccess;

        /// <summary>
        /// Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public void FireBeforeAccess(string id)
        {
            if (BeforeAccess != null) BeforeAccess(id);
        }

        /// <summary>
        /// When true, the last file extension segment will be removed from the URL before the SQL Id is parsed. Only relevant when ImageIdType is a string type. Always true for other values.
        /// Configured by setting 'extensionPartOfId' to the opposite value.
        /// </summary>
        public bool StripFileExtension { get; set; }


        /// <summary>
        /// The database connection string. Defaults to null. You can specify an existing web.config connection string using
        /// the "ConnectionStrings:namedKey" convention.
        /// </summary>
        public string ConnectionString{get;set;}

        /// <summary>
        /// If true, the queries will executed as if they are sproc names.
        /// </summary>
        public bool QueriesAreStoredProcedures { get; set; }

        /// <summary>
        /// The query that returns the binary image data based on the ID. Defaults to "SELECT Content FROM Images WHERE ImageID=@id"
        /// </summary>
        public string ImageBlobQuery { get; set; }

        /// <summary>
        /// The query that returns the modified and created date of the image.  Defaults to "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id".
        /// Of all the dates returned by the query, the first non-empty date is used.
        /// </summary>
        public string ModifiedDateQuery { get; set; }


        private System.Data.SqlDbType imageIdType = System.Data.SqlDbType.Int;
        /// <summary>
        /// Specifies the type of ID used for images. Int, string, and GUID types are the only valid values.
        /// Throws an ArgumentOutOfRange exception if set to an invalid value.
        /// </summary>
        public SqlDbType ImageIdType
        {
            get { return imageIdType; }
            set
            {
                if (!IsStringType(value) && !IsIntType(value) && value != System.Data.SqlDbType.UniqueIdentifier)
                    throw new ArgumentOutOfRangeException("Int, TinyInt, SmallInt, BigInt, VarChar, NVarChar, NChar, Char, and UniqueIdentifier are the only valid values for ImageIdType");

                imageIdType = value;
            }
        }

        /// <summary>
        /// Returns true if the specified type is a kind of strings
        /// </summary>
        public bool IsStringType(SqlDbType t)
        {
            return t == System.Data.SqlDbType.VarChar || t == System.Data.SqlDbType.NVarChar ||
             t == System.Data.SqlDbType.NChar || t == System.Data.SqlDbType.Char;

        }
        /// <summary>
        /// Returns true if the specified type is a kind of integer
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool IsIntType(SqlDbType t)
        {
            return t == System.Data.SqlDbType.Int || t == System.Data.SqlDbType.TinyInt ||
                    t == System.Data.SqlDbType.SmallInt || t == System.Data.SqlDbType.BigInt;

        }

        /// <summary>
        /// Called before any database op. Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public virtual void FireAuthorizeEvent(string id)
        {
            FireBeforeAccess(id);
        }
        /// <summary>
        /// Returns a stream to the 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Stream> GetStream(string id)
        {
            FireAuthorizeEvent(id);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(ImageBlobQuery, conn);
                sc.CommandType = QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                sc.Parameters.Add(CreateIdParameter(id));
                SqlDataReader sdr = await sc.ExecuteReaderAsync();
                using (sdr)
                {
                    if (!await sdr.ReadAsync()) throw new FileNotFoundException("Failed to find the specified image " + id + " in the database"); //0 rows

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
            SqlParameter sp = new SqlParameter("id", ImageIdType);
            if (IsIntType(ImageIdType))
            {
                sp.Size = 4;
                sp.Value = long.Parse(id);
            }
            else if (ImageIdType == System.Data.SqlDbType.UniqueIdentifier)
                sp.Value = new Guid(id);
            else if (IsStringType(ImageIdType))
                sp.Value = id;

            return sp;
        }
  

        public override async Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString)
        {
            var id = ParseIdFromVirtualPath(virtualPath);
            SqlConnection conn = GetConnectionObj();
            conn.Open();
            using (conn)
            {
                SqlCommand sc = new SqlCommand(ModifiedDateQuery, conn);
                sc.Parameters.Add(CreateIdParameter(id));
                sc.CommandType = QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                SqlDataReader sdr = await sc.ExecuteReaderAsync();
                using (sdr)
                {
                    if (!await sdr.ReadAsync())
                    {
                        return new BlobMetadata() { Exists = false }; //0 rows
                    }
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        if (!sdr.IsDBNull(i) && sdr.GetValue(i) is DateTime)
                        {
                            DateTime val = (DateTime)sdr.GetValue(i);
                            if (val.Kind == DateTimeKind.Unspecified) val = DateTime.SpecifyKind(val, DateTimeKind.Utc);
                            //Return the first non-null datetime instance in the row. Regardless of value.
                            return new BlobMetadata(){ LastModifiedDateUtc = val, Exists = true};
                        }
                    }
                    //No modified date is available, but...
                    return new BlobMetadata() { Exists = true };
                }
            }
        }


        /// <summary>
        /// Creates and returns a SqlConnection object for the database based on the configuration.
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetConnectionObj()
        {

            //First, try the connection string as a connection string key.
            if (System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionString] != null)
                return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionString].ConnectionString);

            //Second, try the .NET syntax
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string key = ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] != null)
                    return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString);
                else
                    throw new ImageResizer.ImageProcessingException("SqlReader: Failed to locate the named connection string '" + key + "' in web.config");

            }

            //Third, try it as an actual connection string
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Returns a SqlCommand cache dependency using the modifiedQuery.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SqlCommand GetCacheDependencyQuery(string id)
        {
            SqlCommand sc = new SqlCommand(ModifiedDateQuery, GetConnectionObj());
            sc.CommandType = QueriesAreStoredProcedures ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
            sc.Parameters.Add(CreateIdParameter(id));
            return sc;
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
            if (!checkPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase)) return null;
            string id = checkPath.Substring(VirtualFilesystemPrefix.Length); //Strip prefix
            //Strip slashes at beginning 
            id = id.TrimStart(new char[] { '/', '\\' });
            //Strip extension if not a string 
            if (!IsStringType(ImageIdType) || StripFileExtension)
            {
                int length = id.LastIndexOf('.');
                if (length > -1) id = id.Substring(0, length);
            }
            //Can't be empty.
            if (id.Length < 1) return null;
            //Verify only valid characters present
            if (IsIntType(ImageIdType))
            {
                long val = 0;
                if (!long.TryParse(id, out val)) return null; // not a valid integer
            }
            else if (ImageIdType == System.Data.SqlDbType.UniqueIdentifier)
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
            else if (IsStringType(ImageIdType))
            {
                return id;
            }
            else
            {
                throw new ImageProcessingException("Only Integer, String, and GUID identifiers are suported by SqlReader");
            }

            return id;
        }




        /// <summary>
        /// Provides the diagnostics system with a list of configuration issues
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IIssue> GetIssues()
        {
            List<IIssue> issues = new List<IIssue>();

            //1) Verify named connection strings exist
            string prefix = "ConnectionStrings:";
            //ConnectionStrings:namedString convention
            if (ConnectionString != null && ConnectionString.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string key = ConnectionString.Trim().Substring(prefix.Length).Trim();
                if (System.Configuration.ConfigurationManager.ConnectionStrings[key] == null)
                    issues.Add(new Issue("SqlReader", "Failed to locate the named connection string '" + key + "' in web.config", "", IssueSeverity.ConfigurationError));

            }
            return issues;
        }





        public CacheDependency VppGetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return new SqlCacheDependency(GetCacheDependencyQuery(ParseIdFromVirtualPath(virtualPath)));
        }

        public string VppGetFileHash(string virtualPath, System.Collections.IEnumerable virtualPathDependencies)
        {
            return null;
        }

        public override Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            return this.GetStream(ParseIdFromVirtualPath(virtualPath));
        }
    }

}