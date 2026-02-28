Tags: plugin
Aliases: /plugins/psd /plugins/psdr.. /plugins/psdreader
Edition: elite
Tagline: Adds support for PSD source files (reads embedded preview).

# PsdReader plugin

*PLEASE NOTE*
* **Do not use with untrusted PSD files. This plugin has not undergone security testing.**


Adds support for .PSD source files. No configuration required.

Transparent PSD files will be displayed with a white background. For transparency support for PSDs, see [FreeImageDecoder](/plugins/freeimage).

## Installation

Either run `Install-Package ImageResizer.Plugins.PsdReader` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.PsdReader.dll to your project
2. Add `<add name="PsdReader" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

## Usage

Simply reference a .psd file as you would a .jpg

    image.psd?width=100

A 100px wide JPEG will be returned.

You can also simply convert the image to another format.

    image.psd?format=png
    image.psd?format=jpg

## Version history

### v4.3 (current)

All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2). No functional changes to this plugin.

### v4.2.8 and prior

This plugin has been stable since its initial release. No breaking changes.