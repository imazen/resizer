Tags: plugin
Bundle: free
Edition: free
Tagline: Prevent System.Routing from taking over the ImageResizer's requests.
Aliases: /plugins/mvcroutingshim

# MvcRoutingShim plugin

Prevents System.Routing from conflicting with the ImageResizer. Takes a minimalist approach by disabling routing only for requests that the ImageResizer is actually working on. Note that you still may need to add IgnoreRoute statements to allow the original images to be viewed without using the ImageResizer.

This plugin is installed by default in ImageResizer v4+, as part of ImageResizer.dll