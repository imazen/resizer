Tags: plugin
Bundle: free
Edition: free
Tagline: Prevent System.Routing from taking over the ImageResizer's requests.
Aliases: /plugins/mvcroutingshim

# MvcRoutingShim plugin (built into core since v4)

Prevents ASP.NET MVC's `System.Web.Routing` from intercepting requests that the ImageResizer needs to handle. Takes a minimalist approach by disabling routing only for requests that the ImageResizer is actually processing.

**This plugin is installed automatically by ImageResizer v4.** Do **not** add `<add name="MvcRoutingShim" />` to your `<plugins>` section — doing so will cause an "instance of the specified plugin has already been added" error.

If you are upgrading from v3 to v4, remove any `<add name="MvcRoutingShim" />` line from your Web.config.

You may still need to add `routes.IgnoreRoute` statements in your MVC route configuration to allow original (unprocessed) images to be served directly without going through MVC routing.
