Aliases: /docs/process-and-cache

# Independent processing and caching

The ImageResizer HttpModule offers two independently controlled sets of behaviors - processing, and caching.

Caching can work with any file type, and is ideal if you have a VirtualPathProvider that gets its data from a high-latency source like S3, Azure, or SQL. The caching layer isn't (yet) designed for output caching aspx pages, but you can give it a delegate that writes a stream, so it's quite flexible and can be reused.

Processing only works with images. When an image is processed, it is decoded, processed (applying querystring commands), then re-encoded. 
Processing only happens if the image isn't already cached, and that decision is made by the Cache plugin.

By default, processing occurs if (a) the URL uses a supported image extension and (b) 1 or more processing commands were specified in the querystring (excluding cache and process, of course).

By default, caching occurs  only if processing occurs.

If neither caching nor processing will occur for a request, the request is left alone.

* You can set &process=always to force a request to be processed (say it doesn't have a valid extension, but is still an image, or you want it to be re-encoded even though nothing else is happening to it).
* You can set &process=no to prevent a request from being treated as an image (instead, it will be treated as a binary stream).

* You can set &cache=always to force a request to be cached even if it isn't going to be processed
* You can set &cache=no to prevent a request from being cached (very useful for dynamic 'image studio' like apps where the URLs won't be reused).


# The catch

The ImageResizer doesn't even look at URLs that don't have a valid image extension. You'll need to change this behavior if you want to work with non-image-looking URLs.

[This article shows how to cache files based on folder or extension](/docs/howto/cache-non-images).


# Caching non-image files

When a non-image file is cached to disk, the extension ".unknown" is used. This helps avoid unintentional problems that would occur, if, say, a ".aspx" or ".config" file was somehow cached.

The mime-type sent with non-image URLs is 'application/octet-stream'.

# Images without correct extensions

If the URL doesn't have a correct image extension, you should specify the output format in the querystring (&format=jpg\|gif\|png). This will allow the mime-type and cached extension to be set properly. You will also need to set Pipeline.SkipFileTypeCheck and &process=always in the PostAuthorizeRequestStart handler [see article](/docs/howto/cache-non-images).

Alternatively, if you just have a handful of non-standard extensions that cleanly map to image types, you can call the following static method to add new mappings. (This method is subject to change)

	ImageResizer.Plugins.Basic.DefaultEncoder.AddImageExtension("jpg", ImageFormat.Jpeg);

