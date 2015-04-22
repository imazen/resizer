Tags: plugin
Bundle: free
Edition: free
Tagline: Prevent MVC Routes from taking over the ImageResizer's requests.
Aliases: /plugins/mvcroutingshim

# MvcRoutingShim plugin

Prevents ASP.NET MVC routes from conflicting with the ImageResizer. Takes a minimalist approach by disabling MVC routing only for requests that the ImageResizer is actually working on. Note that you still may need to add IgnoreRoute statements to allow the original images to be viewed without using the ImageResizer.

Included in 3.0.12+

## Installation

Either run `Install-Package ImageResizer.MvcWebConfig` in the NuGet package manager (this will also install the HttpModule settings), or:

1. Add a reference to ImageResizer.Mvc.dll, or run "Install-Package ImageResizer.Mvc" in the NuGet package manager.
2. Add `<add name="MvcRoutingShim" />` in the `<plugins />` section of Web.config. (The NuGet package doesn't add this automatically).
  
