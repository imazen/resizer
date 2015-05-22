Tags: plugin
Edition: free
Tagline: Render PDFs to images
Aliases: /plugins/pdfiumrenderer

# PdfiumRenderer plugin

## Installation

1. Add ImageResizer.Plugins.PdfiumRenderer.dll to the project or /bin.
3. In the `<plugins />` section, insert `<add name="PdfiumRenderer" downloadNativeDependencies="true"/>`

## Via nuget

1. `Install-Package ImageResizer.Plugins.PdfiumRenderer`
3. In the `<plugins />` section of Web.config, insert `<add name="PdfiumRenderer" downloadNativeDependencies="true"/>`


This plugin requires `pdfium.dll` to be present. Both 32-bit and 64-bit variants are supported.

There are two options to include these native Dlls:

* Put either the 32-bit or 64-bit version of the pdfium DLL in the same directory as where the plugin is located;
* In the same directory as where the plugin is located, create an "x86" directory with the 32-bit DLL and an
  "x64" directory with the 64-bit DLL. The PdfiumViewer library will then automatically pick the correct
  one.
* Specify `downloadNativeDependencies=true` during installation.

The pdfium DLLs can be downloaded from:

https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x86/pdfium.dll (32-bit)
https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x64/pdfium.dll (64-bit)

## Parameters

Given a URL to a pdf on the same server, add ?page=1&width=600

* `width` and `height` control the rendered page size.
* `page` - determines which page is rendered.
* `annotation=true` - renders annotations.
* `lcd=true` -optimizes rendered text for LCD displays.
* `grayscale=true` - Renders in grayscale.
* `halftone=true` - Forces halftone rendering.
* `print=true` - Optimizes for printing.
* `transparent=true` - Enables transparency.

