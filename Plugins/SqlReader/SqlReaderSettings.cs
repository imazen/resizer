/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.SqlReader {

    public delegate void AuthorizeEvent(String id);

    /// <summary>
    /// Holds settings used to create a SqlReader
    /// </summary>
    public class SqlReaderSettings {

        public SqlReaderSettings() {
        }

        public SqlReaderSettings(System.Collections.Specialized.NameValueCollection args) {
            if (!string.IsNullOrEmpty(args["prefix"])) this.PathPrefix = args["prefix"];
            if (!string.IsNullOrEmpty(args["connectionString"])) this.ConnectionString = args["connectionString"];
            this.ImageIdType = args.Get<SqlDbType>("idType",this.ImageIdType);
            if (!string.IsNullOrEmpty(args["blobQuery"])) this.ImageBlobQuery = args["blobQuery"];
            if (!string.IsNullOrEmpty(args["existsQuery"])) this.ImageExistsQuery = args["existsQuery"];
            if (!string.IsNullOrEmpty(args["modifiedQuery"])) this.ModifiedDateQuery  = args["modifiedQuery"];
            
            StripFileExtension = !args.Get<bool>("extensionPartOfId", false);
            RegisterAsVirtualPathProvider = args.Get<bool>("vpp", true);
            RequireImageExtension = args.Get<bool>("requireImageExtension", RequireImageExtension);
            UntrustedData = args.Get<bool>("untrustedData", UntrustedData);
            CacheUnmodifiedFiles = args.Get<bool>("cacheUnmodifiedFiles", CacheUnmodifiedFiles);
            CheckForModifiedFiles = args.Get<bool>("checkForModifiedFiles", true);
        }

        /// <summary>
        /// Called before accessing any row in the database. The row ID is passed as a string argument.
        /// If the current user should not access the row, throw an HttpException(403, "message")
        /// </summary>
        public event AuthorizeEvent BeforeAccess;

        /// <summary>
        /// Fires the BeforeAccess event
        /// </summary>
        /// <param name="id"></param>
        public void FireBeforeAccess(string id) {
            if (BeforeAccess != null) BeforeAccess(id);
        }

        private bool stripFileExtension = true;
        /// <summary>
        /// When true, the last file extension segment will be removed from the URL before the SQL Id is parsed. Only relevant when ImageIdType is a string type. Always true for other values.
        /// Configured by setting 'extensionPartOfId' to the opposite value.
        /// </summary>
        public bool StripFileExtension {
            get { return stripFileExtension; }
            set { stripFileExtension = value; }
        }

        private bool _registerAsVirtualPathProvider = true;
        /// <summary>
        /// (default: true) When true, the SqlReader will be registered as a VirtualPathProvider with ASP.NET, which will
        /// make this plugin's virtual files accessible from all code which depends on the VirtualPathProvider system. If trust levels don't allow that, it falls back to IVirtualImageProvider mode, which allows the image resizer to access the files, but not other systems, so you'll need to enable cacheUnmodifiedFiles if you want to access files without resizing them.
        /// </summary>
        public bool RegisterAsVirtualPathProvider {
            get { return _registerAsVirtualPathProvider; }
            set { _registerAsVirtualPathProvider = value; }
        }
         
        private bool _requireImageExtension = true;
        /// <summary>
        /// (default true) When false, all URLs inside the PathPrefix folder will be assumed to be images, and will be handled by this plugin.
        /// You should still use image extensions, otherwise we don't know what content type to send with the response, and browsers will choke. 
        /// It's  also the cleanest way to tell the image resizer what kind of file type you'd like back when you request resizing.
        /// This setting is designed to support non-image file serving from the DB.
        /// It will also cause conflicts if PathPrefix overlaps with a folder name used for something else.
        /// </summary>
        public bool RequireImageExtension {
            get { return _requireImageExtension; }
            set { _requireImageExtension = value; }
        }

        private bool _untrustedData = false;
        /// <summary>
        /// (default: false) When true, all requests will be re-encoded before being served to the client. Invalid or malicious images will fail with an error if they cannot be read as images.
        /// This should prevent malicious files from being served to the client.
        /// </summary>
        public bool UntrustedData {
            get { return _untrustedData; }
            set { _untrustedData = value; }
        }

        private bool _cacheUnmodifiedFiles = false;
        /// <summary>
        /// (default false). When true, files and unmodified images (i.e, no querystring) will be cached to disk (if they are requested that way) instead of only caching requests for resized images.
        /// DiskCache plugin must be installed for this to have any effect.
        /// </summary>
        public bool CacheUnmodifiedFiles {
            get { return _cacheUnmodifiedFiles; }
            set { _cacheUnmodifiedFiles = value; }
        }

        /// <summary>
        /// If true, SQL will be consulted on each image view to ensure the cached image is up to date and the original still exists. 
        /// </summary>
        public bool CheckForModifiedFiles{get;set;}
       

        private string pathPrefix = "~/databaseimages/";
        /// <summary>
        /// Defines a virtual path where database images can be accessed. Defaults to "~/databaseimages/"
        /// Ex image URL: localhost/databaseimages/4953.jpg
        /// </summary>
        public string PathPrefix {
            get { return pathPrefix; }
            set { pathPrefix = value; virtualPathPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(pathPrefix); }
        }

        private string virtualPathPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative("~/databaseimages/");
        /// <summary>
        /// Returns PathPrefix, but with the "~" resolved - I.e, a full virtual path. If the string doesn't start with / or ~, the application path is prepended as if it started with ~/
        /// </summary>
        public string VirtualPathPrefix {
            get { return virtualPathPrefix; }
        }

        private string connectionString = null;
        /// <summary>
        /// The database connection string. Defaults to null. You can specify an existing web.config connection string using
        /// the "ConnectionStrings:namedKey" convention.
        /// </summary>
        public string ConnectionString {
            get { return connectionString; }
            set { connectionString = value; }
        }

        private string imageBlobQuery =
            "SELECT Content FROM Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns the binary image data based on the ID. Defaults to "SELECT Content FROM Images WHERE ImageID=@id"
        /// </summary>
        public string ImageBlobQuery {
            get { return imageBlobQuery; }
            set { imageBlobQuery = value; }
        }

        private string modifiedDateQuery =
            "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns the modified and created date of the image.  Defaults to "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id".
        /// Of all the dates returned by the query, the first non-empty date is used.
        /// </summary>
        public string ModifiedDateQuery {
            get { return modifiedDateQuery; }
            set { modifiedDateQuery = value; }
        }

        private string imageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";
        /// <summary>
        /// The query that returns whether an image exists or not. Defaults to "Select COUNT(ImageID) From Images WHERE ImageID=@id"
        /// </summary>
        public string ImageExistsQuery {
            get { return imageExistsQuery; }
            set { imageExistsQuery = value; }
        }

        private System.Data.SqlDbType imageIdType = System.Data.SqlDbType.Int;
        /// <summary>
        /// Specifies the type of ID used for images. Int, string, and GUID types are the only valid values.
        /// Throws an ArgumentOutOfRange exception if set to an invalid value.
        /// </summary>
        public SqlDbType ImageIdType {
            get { return imageIdType; }
            set {
                if (!IsStringType(value) && !IsIntType(value) &&  value != System.Data.SqlDbType.UniqueIdentifier)
                    throw new ArgumentOutOfRangeException("Int, TinyInt, SmallInt, BigInt, VarChar, NVarChar, NChar, Char, and UniqueIdentifier are the only valid values for ImageIdType");

                imageIdType = value; 
            }
        }

        /// <summary>
        /// Returns true if the specified type is a kind of strings
        /// </summary>
        public bool IsStringType(SqlDbType t) {
                return t == System.Data.SqlDbType.VarChar || t == System.Data.SqlDbType.NVarChar ||
                 t == System.Data.SqlDbType.NChar || t == System.Data.SqlDbType.Char;
            
        }
        /// <summary>
        /// Returns true if the specified type is a kind of integer
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool IsIntType(SqlDbType t) {
            return t == System.Data.SqlDbType.Int || t == System.Data.SqlDbType.TinyInt ||
                    t == System.Data.SqlDbType.SmallInt || t == System.Data.SqlDbType.BigInt;
            
        }

    }
}
