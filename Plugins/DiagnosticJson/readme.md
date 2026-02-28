Tags: plugin
Edition: free
Aliases: /plugins/diagnosticjson

# DiagnosticJson plugin

DiagnosticJson allows you to access the estimated dimensions and information about the image as json instead of returning the image itself.

## Activation commands

* `&diagnosticjson=layout` triggers the JSON response.
* `&j.indented=true` causes pretty-printing

## Installation

Either run `Install-Package ImageResizer.Plugins.DiagnosticJson` in the NuGet package manager, or:

1. Add a reference to ImageResizer.Plugins.DiagnosticJson.dll in your project.
2. Add `<add name="DiagnosticJsonPlugin" />` in the `<plugins>` section of Web.Config

## Version history

### v4.3 (current)

All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2). No functional changes to this plugin.

### v4.2.8 and prior

This plugin has been stable since its initial release. No breaking changes.