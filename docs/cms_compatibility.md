Aliases: /docs/workswith/dotnetnuke /docs/workswith/ektron /docs/workswith/episerver /docs/workswith/umbraco /docs/workswith/sitecore /docs/workswith/sitefinity /docs/workswith/orchard /docs/cms_compatibility

# CMS Compatibility

ImageResizer can be installed side-by-side with the following content management systems - no additional steps are required.

* DotNetNuke
* EPiServer
* Umbraco 4, 5, 6


# Additional information for certain CMSes

## Articles on using ImageResizer with Umbraco

* [heltBlank: Umbraco and ImageResizer](http://heltblank.wordpress.com/2012/02/13/imageresizing-net-and-umbraco-5-jupiter/)
* [uBootstrap for Umbraco](http://our.umbraco.org/projects/starter-kits/ubootstrap) now [includes ImageResizer to handle responsive imaging needs](http://jlusar.es/ubootstrap-fluent-layout)


## Walkthroughs on using ImageResizer with EPiServer

* [Frederik Vig: Add powerful image resizing, cropping and manipulation support to your EPiServer website](http://www.frederikvig.com/2013/01/add-powerful-image-resizing-cropping-and-manipulation-support-to-your-episerver-website/)
* [Anders Hattestad: Automatically change images in a responsive design to scale (EPiServer)](http://world.episerver.com/Blogs/Anders-Hattestad/Dates/2012/8/Automatically-change-images-in-a-responsive-design-to-scale/)


## Ektron includes ImageResizer

Ektron 8.6 and higher include ImageResizer Essential Edition, but do not register the HttpHandler properly. This means `ImageResizer.dll` may already be installed in `/bin`, but you will still have to modify `Web.config`.

Follow standard installation instructions. If you get an assembly binding error, you may need to upgrade to a newer release of Ektron (Ektron 8.6 uses a version-specific assembly reference to ImageResizer; this should be fixed in later releases).

## Sitecore and ImageResizer

Standard installation steps work with SiteCore, but you *cannot use the jpg.ashx syntax* - so ensure you're using IIS integrated mode. All functionality should be accessible with the normal, non-suffixed syntax (`/resizer.debug`, `/image.jpg?width=100`).

## Sitefinity and ImageResizer

Sitefinity 3.X and earlier versions require no additional steps.

Sitefinity 4.X and later versions prevent external libraries and code from accessing uploaded files in a standardized manner (They no longer support ASP.NET's VirtualPathProvider system, nor do they provide an equivalent). 

URIs are a good way to identify files, and we are disappointed that Sitefinity 4.X+ does not provide an API which accepts a virtual path.

While we are looking for a solution that supports all Sitefinity 'Providers' and 'Libraries' in an integrated way, you can use our existing plugins to access the data-stores behind the providers directly. SqlReader can be used if you're uploading files to SQL, and S3Reader, AzureReader, or RemoteReader if you're uploading to blob storage. If you're uploading to the local filesystem, you can use the VirtualFolder plugin to modify the path. 

[One of our users has written an article on how to use ImageResizer and Sitefinity together via filesystem storage](http://blog.falafel.com/Blogs/guest-posts/2013/04/11/how-to-use-the-imageresizer-library-within-sitefinity).


## Orchard and ImageResizer

Standard installation steps are sufficient for the Essential Edition, but an additional Web.config file must be changed for DiskCache to work properly.

Older versions of Orchard Contrib.Cache  duplicate content once for each handler for the HttpApplication.PreSendRequestHeaders event [See bug report on CodePlex](http://stackoverflow.com/questions/14777337/imageresizer-net-with-cache-plugin-causing-duplicate-output-cache)

While the [ImageResizer architecture is designed to make multi-tenanting support easy to implement](/docs/extend/multi-tenanting), no Orchard guru has yet stepped forward to implement the `glue`. Thus, there is not a good multi-tenanting story for ImageResizer and Orchard yet.


### Articles on Orchard and ImageResizer

Dave Gardner [wrote a fantastic 7-part article on how to make a gallery module for Orchard using the ImageResizer](http://bigsitesdoneright.com/big-blog/orchard-cascade-gallery-tutorial-series).

[Bertrand Le Roy (Orchard author): State of .NET Image Resizing: how does imageresizer do?](http://weblogs.asp.net/bleroy/archive/2011/10/22/state-of-net-image-resizing-how-does-imageresizer-do.aspx) * Note the performance chart is bogus due to a mistake in the Bertrand's benchmark code. [The corrected chart](http://downloads.imageresizing.net/Oct29-2011-comparison.png) displays actual apples-to-apples data, instead of comparing low-quality and high-quality image resizing as the original benchmark does.


## Orchard + ImageResizer Disk Cache

Strangely, Orchard disables the serving of static files at the site root level by removing all httpHandlers. For Disk Caching to work, you will need to customize the imagecache/Web.config file. 

Restoring the StaticFileHandler mappings should re-enable efficient disk caching. 

  <?xml version="1.0"?>
  <configuration>
    <system.web>
      <authorization>
        <deny users="*" />
      </authorization>
      <httpHandlers>
        <!-- iis6 - for any request in this location, return via managed static file handler -->
        <add path="*" verb="*" type="System.Web.StaticFileHandler" />
      </httpHandlers>
    <system.webServer>
      <validation validateIntegratedModeConfiguration="false"/>
      <handlers accessPolicy="Script,Read">
        <!--
        iis7 - for any request to a file exists on disk, return it via native http module.
        accessPolicy 'Script' is to allow for a managed 404 page.
        -->
        <add name="imagecache" path="*" verb="*" modules="StaticFileModule" preCondition="integratedMode" resourceType="File" requireAccess="Read" />
      </handlers>
    </system.webServer>
  </configuration>

