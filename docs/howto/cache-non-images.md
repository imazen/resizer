Aliases: /docs/howto/cache-non-images

# How to DiskCache non-image files.

By default, the ImageResizer ignores all requests that don't have a supported image extension. 

We can change this with an event handler and a tiny bit of code. We'll need to determine which requests get cached to disk by checking the Config.Current.Pipeline.PreRewritePath and possibly ModifiedQueryString. Since this code will run for every request, don't do any database lookups, IO calls, or anything *slow*. Use smart string comparison code.


	Config.Current.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender2, HttpContext context) {
			string path = Config.Current.Pipeline.PreRewritePath;
			//Only work with requests in a certain folder
			if (!path.StartsWith(PathUtils.ResolveAppRelative("~/folder/of/files"), StringComparison.OrdinalIgnoreCase)) return;
			//And only those with certain extensions (Maybe we are dynamically generating ZIP files with a VirtualPathProvider?)
			if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return;
		
			Config.Current.Pipeline.SkipFileTypeCheck = true; //Skip the file extension check. FakeExtensions will still be stripped.
			//Non-images will be served as-is
			//Cache all file types, whether they are processed or not.
			Config.Current.Pipeline.ModifiedQueryString["cache"] = ServerCacheMode.Always.ToString();
	};
	

## I'm getting 404s when I try to access non-image files.

Non-image files are cached internally with a .unknown file extension to prevent exploitation or code execution.

IIS may not serve these files by default, as they don't have an assigned mime-type.

[Read this article on how to add a mapping to IIS6](http://support.microsoft.com/kb/326965).

On IIS7, you need to place the following settings inside web.config.

	<configuration>
	    <system.webServer>
	        <staticContent>
	            <mimeMap fileExtension=".unknown" mimeType="application/octet-stream" />
	     </staticContent>
	    </system.webServer>
	</configuration> 

## I'm getting failed requests with a status code of 200 or 206 for PDF files

IIS 7.5 cannot handle certain kinds of range requests, such as the ones made by most PDF reader plugins. 

There's a hotfix for that: http://support.microsoft.com/kb/979543

