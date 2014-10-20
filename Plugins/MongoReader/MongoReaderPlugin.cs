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
using ImageResizer.Storage;
using ImageResizer.ExtensionMethods;
using System.Threading.Tasks;
#endregion

namespace ImageResizer.Plugins.MongoReader
{
    /// <summary>
    ///     An ImageResizer Plugin that retrieves images from a MongoDB/GridFS store
    /// </summary>
    public class MongoReaderPlugin : BlobProviderBase, IMultiInstancePlugin
    {
        private readonly MongoDatabase _db;
        private readonly MongoGridFS _grid;
        private readonly MongoGridFSSettings _gridSettings;

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
            VirtualFilesystemPrefix = args.GetAsString("prefix","~/gridfs/");

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

        public override Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString)
        {
            return Task.FromResult<IBlobMetadata>(new BlobMetadata() { Exists = true });
        }

        public override Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            var _filename = virtualPath.Substring(VirtualFilesystemPrefix.Length);
            //First try to get it by id, next by filename
            if (_filename.StartsWith("id/", StringComparison.OrdinalIgnoreCase))
            {
                //Strip the extension and id/ prefix
                var sid = PathUtils.RemoveFullExtension(_filename.Substring(3));

                ObjectId id;

                if (ObjectId.TryParse(sid, out id))
                {
                    var file = _grid.FindOne(MongoDB.Driver.Builders.Query.EQ("_id", id));

                    if (file == null)
                        throw new FileNotFoundException("Failed to locate blob " + sid + " on GridFS.");

                    return Task.FromResult<Stream>(file.OpenRead());
                }
            }
           
            return Task.FromResult<Stream>(_grid.OpenRead(_filename));
        }
    }
}