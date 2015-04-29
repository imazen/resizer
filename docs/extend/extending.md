Aliases: /docs/extending /docs/plugins/extending /docs/extend/extending 

# Extending the Image Resizer

The goal of the v3 rewrite was to make everything open and extensible. V2 was a monolithic design that, while elegant and very concise, was hard to extend except through source code modification.

V3 introduces the concept of Plugins, while adding an [array of Events](/docs/events) that can be used to customize the behavior of the pipeline.
It also introduces a dedicated configuration section for the Resizer and its plugins, which is freeform and easily queried with a CSS-like syntax through the Config.Current class.

## Plugins can

1.  Modify caching behavior or implement new kinds of persistent caches (ICache)
2.  Modify http headers
3.  Modify the behavior of image processing (40+ methods to override, with XML documentation)
4.  Implement new visual effects, add watermarks or new functionality
5.  Perform URL rewriting or query string expansion by registering an [event handler](/docs/events)
6.  Add support for new image formats
7.  Add support for new image output formats (IEncoder)
8.  Apply advanced security rules
9.  Provide virtualized access to images on an abnormal data source (See [Making an IVirtualImageProvider](/docs/plugins/virtualimageprovider))

## Interfaces

* IPlugin - Implement this to allow addition and removal of the plugin via webconfig. 
* ICache - Implement this to create an alternate caching system
* IEncoder - Implement this to modify how images are encoded into binary streams (For example, adding or improving an output format)
* IQuerystringPlugin - Always implement this if your plugin accepts querystring arguments
* IFileExtensionPlugin - Implement this if your plugin adds support for new file types
* IMultiInstancePlugin - Indicates your plugin supports multiple instances
* ISettingsModifier - Allows a plugin to modify settings specified by either the managed or URL API
* IVirtualImageProvider - Implement to provide virtualized image access or tie into an alternate data store [more...](/docs/plugins/virtualimageprovider)
* IVirtualFile - Your IVirtualImageProvider returns instances of this
* IVirtualBitmapFile - Implement this if you are building a VirtualPathProvider that produces bitmap files (No need unless your VPP modifies or generates images)
* IVirtualFileWithModifiedDate - Your VirtualFile instances should implement this so the caching system can detect changes to source files

## BuilderExtension

Inherit from this class if you want to interact with image layout and rendering. Includes 40+ override-able methods for modifying every part of the processing pipeline.
You'll also need to implement IPlugin (and possibly IQuerystringPlugin.

* Support new source image formats by overriding PreLoadImage and/or LoadImageFailed
* Add new effects with LayoutEffect and RenderEffect
* Do custom watermarking with RenderOverlays

