Tags: plugin
Bundle: free
Edition: free
Aliases: /plugins/friendlyurls /plugins/folderresizesyntax
Tagline: Resize images without using the query string.

# FolderResizeSyntax plugin

(Previously named FriendlyUrls), now part of the Core (since 3.0.12).

Allows images to be resized by specifying a fake folder instead of using the querystring.

For example, `image.jpg?maxwidth=100&maxheight=100` would become `/resize(100,100)/image.jpg`. 

## Installation

1. Add `<add name="FolderResizeSyntax" />` inside `<plugins></plugins>` in Web.config. 
2. Remove `<add name="FriendlyUrls" />` if present. 
3. Remove the old ImageResizer.Plugins.FriendlyUrls.dll reference from the project if preset (it's no longer needed, as it has become part of the core).