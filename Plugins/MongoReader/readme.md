Tags: plugin
uservoice: true
Edition: elite
Tagline: Allows GridFS files to be resized and served
Aliases: /plugins/mongoreader


# MongoReader plugin

Allows files stored on MongoDB GridFS to be resized and processed as if they were local.

### Example URLs

You can access files by their ID (best) or their filename (if it is URL-safe).

* http://localhost/gridfs/filename.jpg?width=100
* http://localhost/gridfs/folder/filename.jpg?width=100
* http://localhost/gridfs/id/4f44195642f73910f056eb33.jpg?width=200

## Installation

Either run `Install-Package ImageResizer.Plugins.MongoReader` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.MongoReader.dll to your project (MongoDB.Driver.dll and MongoDB.BSON.dll will automatically be copied)
2. Add `<add name="MongoReader" connectionString="mongodb://user:password@servername/database" />` inside `<plugins></plugins>` in Web.config.


## Configuration

You must specify a [valid connection string that includes both the database name and credentials](http://www.mongodb.org/display/DOCS/Connections).

  <add name="MongoReader" prefix="~/gridfs" connectionString="mongodb://user:password@servername/database" />

* connectionString - A [valid MongoDB connection string](http://www.mongodb.org/display/DOCS/Connections).

## Version history

### v4.3 (current)

* **BREAKING:** MongoDB.Driver upgraded from 2.1.0 to 3.6.0.
* **BREAKING:** Constructor signature changed: `MongoReaderPlugin(string prefix, MongoDatabase db, MongoGridFSSettings gridSettings)` is now `MongoReaderPlugin(string prefix, IMongoDatabase db, string bucketName = null)`.
* **BREAKING:** `GridFS` property renamed to `GridFSBucket` (type `GridFSBucket`).
* All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2).

### v4.2.8 and prior

* Used MongoDB.Driver 2.1.0.
* Constructor accepted `MongoDatabase` and `MongoGridFSSettings` parameters.
* `GridFS` property was of the legacy GridFS type.
* Targeted .NET 4.5/4.5.2.