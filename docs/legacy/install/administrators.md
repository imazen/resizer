Aliases: /docs/install/administrators


# Guide for Server Administrators

The Image Resizer IIS Module **can be installed without editing code**. 

## Installing the HttpModule into an empty web site

If `Web.config` already exists in the target website or application, go to the next section.

1. Copy the contents of the `Samples\BasicIISSite\` folder of [the download](/download) into the new website.
2. Browse to the web site and type '/resizer.debug.ashx' directly after the domain or IP address. You should get a page of diagnostic information if everything is working. If you need help, [just ask](/support)!


## Installing the HttpModule into an existing web site

1. Copy `ImageResizer.dll` and `ImageResizer.pdb` [from the .zip download](/download) into the 'bin' folder of the website, creating it if it doesn't exist.
3. Save [this file](/attachments/Web.config.txt) into the root of the website and rename it to "Web.config". If a Web.config fie already exists, [you will need to edit it](/docs/install/web-config).
4. Browse to the web site and type '/resizer.debug.ashx' directly after the domain or IP address. You should get a page of diagnostic information if everything is working. If you need help, [just ask](/support)!

## Basic usage

The Image Resizer allows you to resize an image by just modifying the URL a little.

So given 'http://localhost/image.jpg', you could resize the image to 40px wide by using 'http://localhost/image.jpg?width=40'. You can do this from your browser's address bar, HTML page, or anywhere else. No more Photoshop just to do basic image changes.

[View more usage examples](/docs/examples)


## If you're not using IIS

If you're not hosting your website on a Windows server, consider having a secondary server or VM, and pointing a subdomain (like `images.mydomain.com`) to an IIS site to host your images.

## Notes on  IIS7/8 Classic Mode

* Classic Mode does not allow ImageResizer to interact with requests that don't end in ".ashx". So [without some more configuration](/docs/cleanurls), you will need to use the 'image.jpg.ashx?width=100' instead of the 'image.jpg?width=100' syntax. Integrated mode (the default) doesn't have this problem.

## Notes on nested applications

* IIS permits an application (or web site) to have another application inside it. Nested applications inherit the web.config from the parent application, so they will also expect to find ImageResizer.dll in their /bin folder. So, you can either do [some fancy web.config editing to avoid the inheritance](http://aspdotnetfaq.com/Faq/how-to-disable-web-config-inheritance-for-child-applications-in-subfolders-in-asp-net.aspx), or just copy the bin folder into all the sub-applications, which is probably a less error-prone choice.

## Notes on existing ASP.NET MVC websites

If you're installing ImageResizer into an existing ASP.NET MVC application, you need to add the `MvcRoutingShim` plugin in Web.Config.