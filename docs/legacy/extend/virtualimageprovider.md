Aliases: /docs/plugins/virtualimageprovider /docs/extend/virtualimageprovider

# Virtual Image Providers

If your images aren't stored on disk, you need a plugin. Read [plugin basics first, if you haven't already](/docs/plugins/basics) - it's very simple.

If you're using V4 or later, subclassing `BlobProviderBase` is an easier path.

Don't be intimidated. [Look at the Gradient plugin documentation and source code](/plugins/gradient). It's 1 page of source code and 7 lines of boilerplate code - all the rest is rubber on the road. Many of the other plugins (like S3Reader, SqlReader, etc) are more complicated than they need to be because they are also implementing `VirtualPathProvider`: a poorly designed abstract class courtesy of ASP.NET 2.0. If possible, avoid `VirtualPathProvider` and implement only `IVirtualImageProvider`. It's 1/10th the boilerplate, simpler to understand, and free of the framework-level bugs.

## Implementing IVirtualImageProvider

IVirtualImageProvider is an easy interface to implement, with only 2 methods

	public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
	    //TODO: return true if the specified path (with the specified querystring) is a virtual file under the 
			//jurisdiction of this plugin - usually you compare the path to the plugins 'prefix' here.
	}

	public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
	    //TODO: return an instance of a class implementing IVirtualFile, or null if the file doesn't exist.
	}

Usually, VirtualImageProviders use a configurable 'prefix' to separate their domain from other virtual image providers. For example, S3Reader defaults to "~/s3/". The following property handles the conversion of paths like "~/s3/", "s3", and "s3/" all into the required form of "/appname/s3/".

	private string _virtualFilesystemPrefix = null;
	/// <summary>
	/// Requests starting with this path will be handled by this virtual path provider. Should be in app-relative 
	/// form: "~/s3/". Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
	/// </summary>
	public string VirtualFilesystemPrefix
	{
	    get { return _virtualFilesystemPrefix; }
	    set { if (!value.EndsWith("/")) value += "/";  
						_virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value); }
	}

Here's how S3Reader implements those methods:

	public bool IsPathVirtual(string virtualPath)
	{
	    return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
	}

	public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
	    return IsPathVirtual(virtualPath) && new S3File(virtualPath, this).Exists;
	}

	public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
	    return (IsPathVirtual(virtualPath)) ? new S3File(virtualPath, this) : null;
	}

## Implementing IVirtualImage

IVirtualImage is even simpler; 1 method and 1 property.

	public interface IVirtualFile {
	    /// <summary>
	    /// The virtual path of the file (relative to the domain, like /app/folder/file.ext)
	    /// </summary>
	    string VirtualPath { get; }
	    /// <summary>
	    /// Returns an opened stream to the file contents.
	    /// </summary>
	    /// <returns></returns>
	    Stream Open();
	}

The VirtualPath is needed so the ImageResizer can make optimizations about choosing image decoders.

Here's an example to base your virtual class off. It's common to modify the constructor to accept a reference to the parent IVirtualImageProvider, and only make the Open() method a wrapper for an implementation located in the parent IVirtualImageProvider class.


	public class BasicVirtualFile : IVirtualFile {
	
	    public BasicVirtualFile(string virtualPath, NameValueCollection query) { 
				this._virtualPath = virtualPath;
				this.query = new ResizeSettings(query); 
			}
		
			protected string _virtualPath;
	    public string VirtualPath {
	        get { return _virtualPath; }
	    }
	    protected ResizeSettings query;

	    public System.IO.Stream Open() {
	        //TODO: Query your data store and open a Stream that can read the data (or copy it into a MemoryStream), 
					//then return the still-open stream. It will be closed for you later.
	    }

	}



