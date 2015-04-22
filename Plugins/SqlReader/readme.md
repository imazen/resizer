Tags: plugin
uservoice: true
Edition: performance
Tagline: Process and resize images located in a MS SQL database. Extremely configurable, can work with nearly any database schema. A VirtualPathProvider.
Aliases: /plugins/sqlreader


# SQLReader plugin

Allows you to access binary blobs in a SQL database using a URL. Accepts integer, GUID, and string identifiers for images. 

Can serve non-image files from SQL, or can be configured in 'untrusted data mode' where all SQL blobs are decoded and re-encoded to prevent any kind of attack.

### Example Urls

* http://mysite.com/databaseimages/38.jpg
* http://mysite.com/databaseimages/21EC2020-3AEA-1069-A2DD-08002B30309D.jpg
* http://mysite.com/databaseimages/flower32.jpg

See Samples/SqlReaderSample/ in the download for a comprehensive sample project, complete with uploading, listing, and processing.

### Features

* Can serve non-image data if desired
* Compatible with CloudFront and DiskCache
* Supports modified-date comparison between SQL and the filesystem so updated versions of cached files are detected.
* Easy to configure.


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

## Configuration reference

SqlReader is configurable via XML in v3.1 and higher - previous versions need to use the code configuration method.

* **prefix** - This is the app-relative virtual folder where the BLOBs will be accessible within. Separates the DB items from the rest of the site. Should be in the form "~/folder/", with leading ~/ and trailing /.  Defaults to '~/databaseimages/', but '~/sql/' is probably a shorter and kinder alternative.
* **connectionString** - Can be the name of a connection string in web.config, or can be an actual connection string
* **idType** - One of the [SqlDbType values](http://msdn.microsoft.com/en-us/library/system.data.sqldbtype.aspx), the type that is used for the ID column on the images table.
* **blobQuery** - A SQL query that returns the binary image data based on the ID. Defaults to "SELECT Content FROM Images WHERE ImageID=@id"
* **modifiedQuery** - A query that returns the modified and created date of the image.  Defaults to "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id". Of all the dates returned by the query, the first non-empty date is used - this allows fallback if no modified date is available.
* **existsQuery** - A query that returns whether an image exists or not. Defaults to "Select COUNT(ImageID) From Images WHERE ImageID=@id"
* **requireImageExtension** - (default true) When false, all URLs inside the PathPrefix folder will be sent to this plugin. You should still use image extensions, otherwise we don't know what content type to send with the response, and browsers will choke. It's  also the cleanest way to tell the image resizer what kind of file type you'd like back when you request resizing. This setting is designed to support non-image file serving from the DB. It will also cause conflicts if PathPrefix overlaps with a folder name used for something else.
* **cacheUnmodifiedFiles** -    (default false). When true, files and unmodified images (i.e, no querystring) will be cached to disk (if they are requested that way) instead of only caching requests for resized images. DiskCache plugin must be installed for this to have any effect.
* **extensionPartOfId** - (defaults false) If you are using a string ID type for the image, and the file extension is part of that ID, set this to true. 
* **untrustedData** - (default: false) When true, all requests will be re-encoded before being served to the client. Invalid or malicious images will fail with an error if they cannot be read as images. This should prevent malicious files from being served to the client.
* **vpp=true\|false** - Defaults to true. When true, the SqlReader will be registered as a VirtualPathProvider with ASP.NET, which will make this plugin's virtual files accessible from all code, which depends on the VirtualPathProvider system. If trust levels don't allow that, it falls back to IVirtualImageProvider mode, which allows the image resizer to access the files, but not other systems, so you'll need to enable cacheUnmodifiedFiles if you want to access files without resizing them. 
* **checkForModifiedFiles=true\|false** - If true, will query the DB for each request to verify the image hasn't changed. Defaults to true; set false for improved performance.

## Code configuration example

This also shows how to have granular access control for SQL rows.

       public class Global : System.Web.HttpApplication
       {
           protected void Application_Start(object sender, EventArgs e)
           {
              //Configure Sql Backend
              SqlReaderSettings s = new SqlReaderSettings();
              s.ConnectionString = ConfigurationManager.ConnectionStrings["database"].ConnectionString;
              s.PathPrefix = "~/databaseimages/";
              s.ImageIdType = System.Data.SqlDbType.UniqueIdentifier;
              s.ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
              s.ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
              s.ImageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";
              s.CacheUnmodifiedFiles = true;
              s.RequireImageExtension = false;
               //Add plugin
               new SqlReaderPlugin(s).Install(Config.Current);

               //Add access control
               Config.Current.Plugins.LoadPlugins();
               Config.Current.Plugins.Get<SqlReaderPlugin>().Settings.BeforeAccess += delegate(string id) {
                   bool allowed = true;
                   //INSERT HERE: execute query or whatever to check authorization to view this files
                   //  SqlParameter pId = Config.Current.Plugins.Get<SqlReaderPlugin>().CreateIdParameter(id);
                   // 
                   if (HttpContext.Current.Request.QueryString["denyme"] != null) allowed = false;
                   //END pretend code

                   if (!allowed) throw new HttpException(403, "Access denied to this resource.");
               };
      

           }
      }
  
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

