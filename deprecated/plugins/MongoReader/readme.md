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