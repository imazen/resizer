Tags: plugin
Bundle: free
Edition: free
Tagline: Supply default images instead of a 404 when an image is missing. 
Aliases: /plugins/image404 /plugins/404image

# Image404 plugin

## Installation

1. Add `<add name="Image404" />` to the `<plugins>` section.
2. (optional) Add `<image404 baseDir="~/images/404pics/" />` to the `<resizer>` section to specify a default directory for fallback images.
  

## Usage

Add `&404=name` in your image URLs. If the original file is missing, the request will be 302 redirected to the image specified by 'name'. The other querystring values will be copied to the new request, allowing your '404' image to appear properly in the page layout.

'name' can be one of the following

1. An app-relative path, like `~/images/404.png`
2. A domain-relative path, like `/app/images/404.png`
3. A relative path, like `404.png`. This kind of path will be combined with the value of `baseDir` specified in the configuration above.
4. A named path, like 'default' or 'myimage'. You can create a named path by adding an attribute to the image404 element: `<image404 default='~/images/404.png' myimage='~/funny/pic.jpg' />` 

It is strongly suggested that you do not specify fallbacks unless images are *supposed* to be missing. Otherwise, you will never get 404 errors or other indications that there is a problem on your site.
This is why the querystring is used. See bottom of page for examples on applying a 404 fallback to an entire folder.

## Examples

### Basic example using baseDir

Configuration: `<image404 baseDir="~/images/" />`

URL: `/missingimage.jpg?width=200&height=400&404=404.png`

302 redirected to: `/appfolder/images/404.png?width=200&height=400`

### Using named paths

Configuration: `<image404 default="~/images/404.png?bgcolor=black" />`

URL: `/missingimage.jpg?width=200&height=200&404=default`

302 redirected to: `/appfolder/images/404.png?width=200&height=200&bgcolor=black`

### Automatically applying a 404 fallback to all images in a folder

This can be accomplished by adding a URL rewriting handler during App_Start. 
The following sample affects all paths containing '/propertyimages/'

    ImageResizer.Configuration.Config.Current.Pipeline.RewriteDefaults += 
    
      delegate(IHttpModule m, HttpContext c, ImageResizer.Configuration.IUrlEventArgs args) {
        
        if (args.VirtualPath.IndexOf("/propertyimages/", StringComparison.OrdinalIgnoreCase) > -1)
          args.QueryString["404"] = "~/images/404.png";
          
      };


