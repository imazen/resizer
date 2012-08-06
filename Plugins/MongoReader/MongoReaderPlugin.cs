using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using ImageResizer.Util;
using System.Collections.Specialized;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System.IO;

namespace ImageResizer.Plugins.MongoReader {
    public class MongoReaderPlugin:IPlugin, IVirtualImageProvider, IMultiInstancePlugin, IRedactDiagnostics {
        MongoDatabase db;
        MongoGridFSSettings gridSettings;
        MongoGridFS grid;
        /// <summary>
        /// A reference to the GridFS instance used to retrieve files.
        /// </summary>
        public MongoGridFS GridFS { get { return grid; } }

        public MongoReaderPlugin(string prefix, MongoDatabase db, MongoGridFSSettings gridSettings) {
            this.db = db;
            this.gridSettings = gridSettings;
            this.grid = this.db.GetGridFS(gridSettings);
            VirtualFilesystemPrefix = prefix;
        }

        public MongoReaderPlugin(NameValueCollection args) {
            VirtualFilesystemPrefix = string.IsNullOrEmpty(args["prefix"]) ? "~/gridfs/" : args["prefix"];

            string connectionString = args["connectionString"];
            this.db = MongoDatabase.Create(connectionString);
            gridSettings = new MongoGridFSSettings();
            grid = db.GetGridFS(gridSettings);
        }

        public Configuration.Xml.Node RedactFrom(Node resizer) {
            foreach (Node n in resizer.queryUncached("plugins.add")) {
                if (n.Attrs["connectionString"] != null) n.Attrs.Set("connectionString", "[redacted]");
            }
            return resizer;
        }

        private string _virtualFilesystemPrefix = null;
        /// <summary>
        /// Requests starting with this path will be handled by this virtual path provider. Should be in app-relative form: "~/gridfs/". Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
        /// </summary>
        public string VirtualFilesystemPrefix {
            get { return _virtualFilesystemPrefix; }
            set { if (!value.EndsWith("/")) value += "/"; _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value); }
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public bool FileExists(string virtualPath, NameValueCollection queryString) {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString) {
            if (!virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase)) return null;
            return new MongoVirtualFile(this, virtualPath, queryString);
        }


        public class MongoVirtualFile : IVirtualFile, IVirtualFileSourceCacheKey {

            public MongoVirtualFile(MongoReaderPlugin parent, string virtualPath, NameValueCollection query) {
                if (!virtualPath.StartsWith(parent.VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase)) throw new ApplicationException("The specified virtual file must exist within the prefix: " + parent.VirtualFilesystemPrefix);
                this._virtualPath = virtualPath;
                this.query = new ResizeSettings(query);
                this.filename = virtualPath.Substring(parent.VirtualFilesystemPrefix.Length);
                this.parent = parent;
            }
            private MongoReaderPlugin parent;
            private string filename;

            protected string _virtualPath;
            public string VirtualPath {
                get { return _virtualPath; }
            }
            protected ResizeSettings query;

            public System.IO.Stream Open() {
                //First try to get it by id, next by filename
                ObjectId id;
                if (filename.StartsWith("id/", StringComparison.OrdinalIgnoreCase) ) {
                    //Strip the extension and id/ prefix
                    string sid = PathUtils.RemoveFullExtension(filename.Substring(3));
                    if (ObjectId.TryParse(sid, out id)) {
                        var file = parent.grid.FindOne(Query.EQ("_id", id));
                        if (file == null) throw new FileNotFoundException("Failed to locate blob " + sid + " on GridFS.");
                        return file.OpenRead();
                    }
                } 
                return parent.grid.OpenRead(filename);
            }


            public string GetCacheKey(bool includeModifiedDate) {
                return VirtualPath;
            }
        }
    }
}
