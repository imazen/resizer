// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
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
        private readonly IMongoDatabase _db;
        private readonly GridFSBucket _bucket;

        /// <summary>
        ///     Create a MongoReaderPlugin with an existing IMongoDatabase and optional GridFS bucket name
        /// </summary>
        /// <param name="prefix">The virtual folder representing GridFS assets</param>
        /// <param name="db">An existing IMongoDatabase instance</param>
        /// <param name="bucketName">Optional GridFS bucket name (defaults to "fs")</param>
        public MongoReaderPlugin(string prefix, IMongoDatabase db, string bucketName = null)
        {
            _db = db;
            _bucket = bucketName != null
                ? new GridFSBucket(db, new GridFSBucketOptions { BucketName = bucketName })
                : new GridFSBucket(db);
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
            var mongoClient = new MongoClient(mongoUrl);
            _db = mongoClient.GetDatabase(mongoUrl.DatabaseName);
            _bucket = new GridFSBucket(_db);
        }

        /// <summary>
        ///     A reference to the GridFS bucket used to retrieve files.
        /// </summary>
        public GridFSBucket GridFSBucket
        {
            get { return _bucket; }
        }

        public override Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString)
        {
            return Task.FromResult<IBlobMetadata>(new BlobMetadata() { Exists = true });
        }

        public override async Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
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
                    return await _bucket.OpenDownloadStreamAsync(id);
                }
            }

            return await _bucket.OpenDownloadStreamByNameAsync(_filename);
        }
    }
}
