Aliases: /docs/interceptmodule /docs/extend/interceptmodule

# InterceptModule 

This document was last updated on Feb 22, 2012, and describes software that may change.

`InterceptModule` is the ImageResizer's HttpModule. You typically install it by registering it in the `httpModules` section (for < IIS 7 Integrated) and the `modules` section for IIS7+ Integrated mode.

Alternatively, you can instantiate the HttpModule in code and call its Init(HttpApplication) method at any time. Obviously, until Init() is called, it won't operate. [More details on installing HttpModules via code](http://stackoverflow.com/questions/239802/programmatically-register-httpmodules-at-runtime).

## Events handled

The ImageResizer handles two application events: PostAuthorizeRequest and PreSendRequestHeaders.

### PostAuthorizeRequest - Occurs directly after the request has been authenticated and authorized, not before.

This is where most of the work takes place.

1. Fire `Pipeline.PostAuthorizeRequest` event for plugins. Plugins can edit a copy of the request path and querystring during this event - 
   this allows plugins like CloudFront to function. Used minimally, as it has a higher performance impact on the site as a whole.
2. Remove any fake extensions. For example, on legacy IIS modes, ".ashx" needs to be appended for things to work properly: `image.jpg.ashx?width=100`.
3. If the remaining extension is an image type we handle, OR if `Pipeline.SkipFileTypeCheck` is true, then continue. Otherwise quit, having modified nothing about the request.
4. Fire `Pipeline.Rewrite` on the modified path and querystring
5. Fire `Pipeline.RewriteDefaults` (querystring changes here are only kept if they don't conflict with #4)
6. Fire `Pipeline.PostRewrite`
7. Check if the resulting querystring contains any registered ImageResizer commands. If not, quit, having modified nothing.
8. Call UrlAuthorizationModule.CheckUrlAccessForPrincipal() to verify that the final rewrite path is still authorized for access by the current user.
9. Call Pipeline.AuthorizeImage, where handlers can deny or override the CheckUrlAccessForPrincipal() result. If the event args come back negative, throw an 403 HttpException.
10. Check if the file exists physically (unless vppMode=Always)
11. If the file doesn't exist physically (or if vppMode=Always), then get an IVirtualFile instance for the specified path, using (a) IVirtualImageProvider plugins, and (b) the VirtualPathProvider system.
12. If the file is missing, fire Pipeline.ImageMissing and exit, having modified nothing (although a plugin could perform a redirect in the event).
13. If the file exists virtually or physically, we continue to part 2. If a FileNotFoundException occurs during part 2, we follow #12.

### PostAuthorizeRequest part 2

1. We determine whether we need to process the image (are any processing-specific commands specified?)
2. We determine whether we need to cache the image (by default, we only do this if we are processing it, but high-latency IVirtualImageProviders may force caching on unmodified files).
3. If we're doing neither, we step away from the request without having changed anything.
4. Otherwise, we set two keys in context.Items[] to let other plugins know we're handling things. (This allows the MvcRoutingShim to stop conflicting routes).
5. We create a ResponseArgs object and start populating it
6. We get the last modified date from the source file (if present)
7. We calculate the final mime-type and suggested file extension
8. We build a caching key and save the final querystring and path to the ResponseArgs instance
9. We define an anonymous method that writes the resized image to a given stream, and store it in ResponseArgs.
10. We fire Pipeline.PreHandleImage(this,context, responseArgs) in case any final work needs to be done by plugins
11. We call Pipeline.GetCacheProvider().GetCachingSystem(context,responseArgs) to select the appropriate caching plugin.
12. We call .Process(context,responseArgs) on the result. During this call, the caching plugin may call RemapHandler, RewritePath, or perform a redirection.


### Excerpt from ResizeImageToStream anonymous method

	if (!isProcessing) {
	    //Just duplicate the data
	    using (Stream source = (vf != null) ? vf.Open(): 
	                    File.Open(HostingEnvironment.MapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read)) {
	        Utils.copyStream(source, stream);
	    }
	} else {
	    //Process the image
	    if (vf != null)
	        conf.GetImageBuilder().Build(vf, stream, settings);
	    else
	        conf.GetImageBuilder().Build(HostingEnvironment.MapPath(virtualPath), stream, settings); //Use a physical path to bypass virtual file system
	}


### PreSendRequestHeaders - Occurs at the last second in which HTTP headers can be modified

This is where the module applies the caching settings and any other custom http header modifications that were specified in ResponseArgs. Nothing else happens here.
