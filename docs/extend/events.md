Aliases: /docs/events /docs/extend/events

# Events

Events let you perform URL rewriting, force processing and/or caching of arbitrary files, tie into database queries, and do custom authorization. 

All event handlers are registered with the ImageResizer.Configuration.Config.Current.Pipeline class

All events include comprehensive event information and most include mutable objects that allow behavior to be modified.

### Pipeline.Rewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e);

Fired during PostAuthorizeRequest, after ResizeExtension has been removed.
Only fired on requests with extensions that match supported image types. 
You can add additional supported image extensions by registering a plugin that implements IQuerystringPlugin, or you can add an 
extra extension in the URL and remove it here. Example: .psd.jpg</para>


### Pipeline.AuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs e);

Fired after all rewriting should be finished.
Plugins wanting to add additional authorization rules can implement them in a handler,
and set "e.AllowAccess" to false. Plugins can also bypass 2nd-stage UrlAuthorization for the final file by setting "e.AllowAccess" to true. 1st stage UrlAuthorization for the original request URL cannot be bypassed.

### Pipeline.OnFirstRequest(IHttpModule sender, HttpContext context);

Fired once, on the first PostAuthorizeRequest event.

### Pipeline.PostAuthorizeRequestStart(IHttpModule sender, HttpContext context);

Fires during the PostAuthorizeRequest phase, prior to any module-specific logic.
Executes for every request to the website. Use only as a last resort. Other events occur only for image requests, and thus have lower overhead.

### Pipeline.RewriteDefaults(IHttpModule sender, HttpContext context, IUrlEventArgs e);

Fired during PostAuthorizeRequest, after Rewrite.
Any changes made here which conflict will be overwritten by the current query string values. I.e, this is a good place to specify default settings.
Only fired on accepted image types. Plugins can specify additional image extensions to intercept. Rewrite rules can be used to change extensions.

### Pipeline.PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e);

Fired after all other rewrite events.
Only fired on accepted image types. (see Rewrite)

### Pipeline.ImageMissing(IHttpModule sender, HttpContext context, IUrlEventArgs e);

Fired when an image is missing. Allows redirection to 'fallback' images.

### Pipeline.PreHandleImage(IHttpModule sender, HttpContext context, IResponseArgs e);

Fired immediately before the image request is sent off to the caching system for processing.
Allows modification of response headers, caching arguments, and callbacks.

### Pipeline.SelectCachingSystem(object sender, ICacheSelectionEventArgs e);

Allows cache selection to be determined by external code

