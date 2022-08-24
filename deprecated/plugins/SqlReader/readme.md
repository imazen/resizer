Tags: plugin
uservoice: true
Edition: performance
Tagline: Process and resize images located in a MS SQL database. Extremely configurable, can work with nearly any database schema.
Aliases: /plugins/sqlreader


# SQLReader plugin

Allows you to access binary blobs in a SQL database using a URL. Accepts integer, GUID, and string identifiers for images. 

Can serve non-image files from SQL, or can be configured in 'untrusted data mode' where all SQL blobs are decoded and re-encoded to prevent any kind of attack.

### Example URLs

* http://mysite.com/databaseimages/38.jpg
* http://mysite.com/databaseimages/21EC2020-3AEA-1069-A2DD-08002B30309D.jpg
* http://mysite.com/databaseimages/flower32.jpg

See Samples/SqlReaderSample/ in the download for a comprehensive sample project, complete with uploading, listing, and processing.

### Features

* Can serve non-image data if desired
* Compatible with CloudFront and DiskCache
* Supports modified-date comparison between SQL and the file system so updated versions of cached files are detected
* Easy to configure


## Installation

Either run `Install-Package ImageResizer.Plugins.SqlReader` in the NuGet package manager, or:

1. Add a reference to ImageResizer.Plugins.SqlReader to your project.
2. Add SqlReader to the `<plugins />` section.

        <add name="SqlReader" 
          prefix="~/databaseimages/" 
          connectionString="database" 
          idType="UniqueIdentifier" 
          blobQuery="SELECT Content FROM Images WHERE ImageID=@id"
          modifiedQuery="Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id" 
          existsQuery="Select COUNT(ImageID) From Images WHERE ImageID=@id"
          requireImageExtension="false" 
          cacheUnmodifiedFiles="true"
          extensionPartOfId="false"
          checkForModifiedFiles="true"
          vpp="true"
          untrustedData="false" />

### Supported key types

Built-in support is included for the following kinds of keys in the URL. If you have another key type you'd like to request, e-mail support@imageresizing.net

* Int
* Guid
* TinyInt
* SmallInt
* BigInt
* VarChar
* NVarChar
* NChar
* Char
* UniqueIdentifier


## Configuration reference

* **prefix** - This is the app-relative virtual folder where the BLOBs will be accessible within. Separates the DB items from the rest of the site. Should be in the form "~/folder/", with leading ~/ and trailing /.  Defaults to `~/databaseimages/`, but `~/sql/` is probably a shorter and kinder alternative.
* **connectionString** - Can be the name of a connection string in web.config, or can be an actual connection string
* **idType** - One of the [SqlDbType values](http://msdn.microsoft.com/en-us/library/system.data.sqldbtype.aspx), the type that is used for the ID column on the images table.
* **blobQuery** - A SQL query that returns the binary image data based on the ID. Defaults to `SELECT Content FROM Images WHERE ImageID=@id`
* **modifiedQuery** - A query that returns the modified and created date of the image.  Defaults to `Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id`. Of all the dates returned by the query, the first non-empty date is used - this allows fallback if no modified date is available.
* **existsQuery** - A query that returns whether an image exists or not. Defaults to `Select COUNT(ImageID) From Images WHERE ImageID=@id`
* **extensionPartOfId** - (defaults false) If you are using a string ID type for the image, and the file extension is part of that ID, set this to true. 

