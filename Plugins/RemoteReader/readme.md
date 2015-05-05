Tags: plugin
Aliases: /plugins/remotetreader /plugins/remotereader
Edition: performance
Tagline: Allows images located on external servers to be securely resized and processed as if they existed locally.

# RemoteReader plugin

The RemoteReader plugin allows the ImageResizer to resize and display images that are located at any URL. Kind of like a resizing relay.

There are 3 layers of security to prevent abuse. 

1. The API signs the remote url with HMAC SHA-256 to prevent anyone from tampering or generating them without access to the signing key.
2. A whitelist approach is taken to sites. By default, no sites are allowed. You must handle the RemoteReaderPlugin.Current.AllowRemoteRequest event to permit sites (or portions of sites) to be relayed.
4. The  plugin requires that all remote images be decoded and re-encoded. Files are never returned as-is. This ensures that the files are valid images, and prevents XSS attacks. This means that without a querystring, /remote/ image requests will not work.


## Syntax

There are two syntax options. 

1. Using a signed remote URL. (Using RemoteReaderPlugin.Current.CreateSignedUrl(remoteUrl, resizingSettings) <br />
   http://mysite.com/remote.jpg.ashx?width=100&height=200&urlb64=45b45c4a2099b...&hmac=a2099ba2099b

2. Use a human-friendly syntax where the domain name is specified as a folder. 
   http://mysite.com/remote/othersite.com/otherfolder/image.jpg?width=100&height=200

It is possible to set 'allowAllSignedRequests=true', but you must handle the RemoteReaderPlugin.Current.AllowRemoteRequest event and set args.DenyRequest=false to allow the human-friendly syntax to work.

## Installation

Either run `Install-Package ImageResizer.Plugins.RemoteReader` in the NuGet package manager, then follow the 3rd step below, or:

1. Add ImageResizer.Plugins.RemoteReader.dll to your project
2. Add `<add name="RemoteReader" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. Add `<remotereader signingKey="put a long and very secure key here"></remotereader>` inside `<resizer></resizer>`. Make sure the key is kept safe, and is the same across all servers in the web farm (if you're using one). This key can contain any xml-safe characters, and should be as long as possible. URLs generated with one key will not work with another.


## Configuration reference

  <configuration>
    <resizer>
      <remotereader signingKey="ag383ht23sag#laf#lafF#oyfafqewt;2twfqw" allowAllSignedRequests="false" allowRedirects="5">
        <allow domain="imageresizing.net" /> <!-- XML whitelisting requires V3.2 or higher -->
        <allow domain="*.imageresizing.net" onlyWhenSigned="true"/> 
      </remotereader>
      <plugins>
        <add name="RemoteReader" />
      </plugins>
    </resizer>
  </configuration>

### AllowRedirects (V3.1.5+)

In V3.1.5 and higher, redirects are supported, but the default is to throw a 500 error if more than 5 are used. You can configure the number of followed redirects with the allowRedirects setting, or set it to 0 to disable following redirects.

## Exceptions

404 errors are turned into FileNotFoundExceptions, which are turned back into 404 errors in the URL API. 
403 errors are turned into 403 HttpExceptions.

All other exceptions are WebExceptions

## Example event handler for whitelisting part of a website for relay

    using ImageResizer.Plugins.RemoteReader;
    
    protected void Application_Start(object sender, EventArgs e) {
      RemoteReaderPlugin.Current.AllowRemoteRequest += Current_AllowRemoteRequest;
    }

    static void Current_AllowRemoteRequest(object sender, RemoteRequestEventArgs args) {
      if (args.RemoteUrl.StartsWith("http://atrustedwebsite.com/photos/", StringComparison.OrdinalIgnoreCase))
        args.DenyRequest = false;
    }
  
  
## Example code for generating a signed URL

    using ImageResizer.Plugins.RemoteReader;
    
    img1.ImageUrl = RemoteReaderPlugin.Current.CreateSignedUrl("http://atrustedwebsite.com/photos/leaf.jpg", 
                                          new ResizeSettings("width=200&height=100"));
    //For the following to work, allowAllSignedRequests must be true
    img2.ImageUrl = RemoteReaderPlugin.Current.CreateSignedUrl("http://arandomwebsite.com/photos/leaf.jpg", 
                                          new ResizeSettings("width=200&height=100"));

## Limitations of human-friendly syntax

The human-friendly syntax has to go through the IIS and ASP.NET request filtering/normalization system, which may cause issues if your URLs have special characters or spaces.

In 3.1.5 and higher, spaces are supported in URLs, but to support '+' characters in remote URLs, you have to [make a change in Web.config](http://stackoverflow.com/questions/1453218/is-enabling-double-escaping-dangerous), as IIS considers '+' dangerous by default. IIS needs Prozac.

    <system.webServer>
        <security>
            <requestFiltering allowDoubleEscaping="True"/>
        </security>
    </system.webServer>

## Proxy auto-detection

.NET automatically attempts to detect the proxy configuration each time the application starts. To prevent this (often) unnecessary 2-10 second delay, you can disable proxy detection in web.config (below).

    <configuration>
      <system.net>
        <defaultProxy enabled="false">
        </defaultProxy>
      </system.net>
    </configuration>

## Non-ascii URLs

.NET [requires a tiny bit of configuration to allow non-ascii characters in remote URLs](http://stackoverflow.com/questions/6107621/uri-iswellformeduristring-needs-to-be-updated).