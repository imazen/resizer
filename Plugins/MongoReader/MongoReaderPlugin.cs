#region Using

using System;
using System.Collections.Specialized;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;
using ImageResizer.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

#endregion

namespace ImageResizer.Plugins.MongoReader
{
    /// <summary>
    ///     An ImageResizer Plugin that retrieves images from a MongoDB/GridFS store
    /// </summary>
    public class MongoReaderPlugin : IPlugin, IVirtualImageProvider, IMultiInstancePlugin, IRedactDiagnostics
    {
        private readonly MongoDatabase _db;
        private readonly MongoGridFS _grid;
        private readonly MongoGridFSSettings _gridSettings;
        private string _virtualFilesystemPrefix;

        /// <summary>
        ///     Create a MongoReaderPlugin with an existing MongoDatabase and specific settings for GridFS
        /// </summary>
        /// <param name="prefix">The virtual folder representing GridFS assets</param>
        /// <param name="db">An existing MongoDatabase instance</param>
        /// <param name="gridSettings">
        ///     Settings for the GridFS connection
        ///     <see href="http://api.mongodb.org/csharp/1.8/html/7a3abd48-0532-8e7f-3c05-6c9812eb06f8.htm" />
        /// </param>
        public MongoReaderPlugin(string prefix, MongoDatabase db, MongoGridFSSettings gridSettings)
        {
            _db = db;
            _gridSettings = gridSettings;
            _grid = _db.GetGridFS(gridSettings);
            VirtualFilesystemPrefix = prefix;
        }

        /// <summary>
        ///     Create a MongoReaderPlugin from plug-in arguments
        /// </summary>
        /// <param name="args"></param>
        public MongoReaderPlugin(NameValueCollection args)
        {
            VirtualFilesystemPrefix = string.IsNullOrEmpty(args["prefix"]) ? "~/gridfs/" : args["prefix"];

            var mongoUrl = new MongoUrl(args["connectionString"]);

            // Using new client, server database initialization. Wordy but recommended.
            var mongoClient = new MongoClient(mongoUrl);
            var mongoServer = mongoClient.GetServer();
            _db = mongoServer.GetDatabase(mongoUrl.DatabaseName);
            _gridSettings = new MongoGridFSSettings();
            _grid = _db.GetGridFS(_gridSettings);
        }

        /// <summary>
        ///     A reference to the GridFS instance used to retrieve files.
        /// </summary>
        public MongoGridFS GridFS
        {
            get { return _grid; }
        }

        /// <summary>
        ///     Requests starting with this path will be handled by this virtual path provider. Should be in app-relative form: "~/gridfs/". Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
        /// </summary>
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set
            {
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }

        /// <summary>
        ///     Install the MongoReader plugin
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        ///     Uninstall the MongoReader plugin
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        ///     Removes connection string attributes for security
        /// </summary>
        /// <param name="resizer"></param>
        /// <returns></returns>
        public Node RedactFrom(Node resizer)
        {
            foreach (var n in resizer.queryUncached("plugins.add"))
            {
                if (n.Attrs["connectionString"] != null) n.Attrs.Set("connectionString", "[redacted]");
            }
            return resizer;
        }

        /// <summary>
        ///     Checks if the virtual path has the same root as our virtual filesystem prefix
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Returns a MongoVirtual file matching the path, or null if the path doesn't
        ///     fall in this virtual root.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (!virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase)) return null;
            return new MongoVirtualFile(this, virtualPath, queryString);
        }


        /// <summary>
        ///     Represents a file stored in GridFS and the requested resize settings
        /// </summary>
        public class MongoVirtualFile : IVirtualFile, IVirtualFileSourceCacheKey
        {
            private readonly string _filename;
            private readonly MongoReaderPlugin _parent;

            /// <summary>
            ///     Resize settings from the query
            /// </summary>
            protected ResizeSettings Query;

            /// <summary>
            ///     The files virtual path
            /// </summary>
            protected string _virtualPath;

            /// <summary>
            ///     Creates a new MongoVirtualFile
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="virtualPath"></param>
            /// <param name="query"></param>
            public MongoVirtualFile(MongoReaderPlugin parent, string virtualPath, NameValueCollection query)
            {
                if (!virtualPath.StartsWith(parent.VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new ApplicationException("The specified virtual file must exist within the prefix: " +
                                                   parent.VirtualFilesystemPrefix);
                _virtualPath = virtualPath;
                Query = new ResizeSettings(query);
                _filename = virtualPath.Substring(parent.VirtualFilesystemPrefix.Length);
                _parent = parent;
            }

            /// <summary>
            ///     The virtual path to a GridFS file
            /// </summary>
            public string VirtualPath
            {
                get { return _virtualPath; }
            }

            /// <summary>
            ///     Locates and opens a GridFS file matching the filename. Using the files
            ///     id as opposed to its 'friendly' name will improve the response time
            /// </summary>
            /// <returns>A stream containing the requested file</returns>
            public Stream Open()
            {
                //First try to get it by id, next by filename
                if (_filename.StartsWith("id/", StringComparison.OrdinalIgnoreCase))
                {
                    //Strip the extension and id/ prefix
                    var sid = PathUtils.RemoveFullExtension(_filename.Substring(3));

                    ObjectId id;

                    if (ObjectId.TryParse(sid, out id))
                    {
                        var file = _parent._grid.FindOne(MongoDB.Driver.Builders.Query.EQ("_id", id));

                        if (file == null)
                            throw new FileNotFoundException("Failed to locate blob " + sid + " on GridFS.");

                        return file.OpenRead();
                    }
                }
                return _parent._grid.OpenRead(_filename);
            }


            /// <summary>
            ///     Returns the virtual folder cache-key
            /// </summary>
            /// <remarks>Ignores includeModifiedDate parameter</remarks>
            /// <param name="includeModifiedDate"></param>
            /// <returns></returns>
            public string GetCacheKey(bool includeModifiedDate)
            {
                return VirtualPath;
            }
        }
    }
}