Tags: plugin
Bundle: free
Edition: free
Tagline: Create an IIS-like virtual folder that works in Cassini as well as IIS, and doesn't require IIS configuration.
Aliases: /plugins/virtualfolder

# VirtualFolder plugin

Functions like an IIS virtual folder, but works within Visual Studio/Cassini web server.

IIS Virtual Folders perform better, however, so this plugin should only be used for testing, development, or as a last resort.

A virtual folder masking the root of the website can sometimes trigger perpetual restarts. To avoid this, set vpp="false" (will make the images/files only accessible when they have a querystring) or use a subfolder for the virtualPath value instead of "~/".

## Installation

1. Add `<add name="VirtualFolder" virtualPath="~/" physicalPath="..\Images" vpp="false "/>` to the `<plugins />` section.
2. You're done. If you want to add more virtual folders, repeat step 1.


### Notes

* VirtualPath can either be app-relative (~/folder) or domain-relative (/appfolder/folder).

* PhysicalPath can be absolute (C:\folder\etc) or relative to the app physical path (..\Images).
