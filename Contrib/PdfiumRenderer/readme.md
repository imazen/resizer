Tags: plugin
Edition: free
Tagline: Render PDFs to images
Aliases: /plugins/pdfiumrenderer

# PdfiumRenderer plugin

*PLEASE NOTE*
* **This plugin is not forwards-compatible. Avoid usage for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do NOT use with untrusted data. This plugin passes source files to Pdfium, which (like all software with embedded codecs), has a history of vulnerabilities.**
* **Using this plugin with malicious PDFs could result in a remote code execution vulnerability.**
* You can view [recent PDFium vulnerabilities here](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=pdfium) and [here](https://web.nvd.nist.gov/view/vuln/search-results?query=pdfium&search_type=all&cves=on)

## Install via NuGet (recommended)

1. `Install-Package ImageResizer.Plugins.PdfiumRenderer`. Be sure to install with the `PdfiumViewer` dependency.
2. Install the native pdfium.dll via NuGet: `Install-Package PdfiumViewer.Native.x86_64.v8-xfa` (64-bit) and/or `Install-Package PdfiumViewer.Native.x86.v8-xfa` (32-bit). These packages automatically place the native DLLs in the correct output directory.
3. In the ImageResizer `<plugins />` section of web.config, insert `<add name="PdfiumRenderer" />`.

## Install manually

1. Add `ImageResizer.Plugins.PdfiumRenderer.dll` as a reference to the project, or put a copy of it in the /bin folder.
2. Add `PdfiumViewer.dll` as a reference to the project, or put a copy of it in the /bin folder.
3. Install the native pdfium.dll via the NuGet packages above, or manually place the DLLs:
  * Put the 32-bit or 64-bit version of `pdfium.dll` in the /bin folder, or
  * Create `/bin/x86` and `/bin/x64`, putting the 32-bit copy of `pdfium.dll` in the "x86" folder and the 64-bit copy of `pdfium.dll` in the "x64" folder. The correct bitness is then chosen automatically at runtime.
4. In the ImageResizer `<plugins />` section of web.config, insert `<add name="PdfiumRenderer" />`.
5. In an IIS web application scenario, ensure the relevant application pool identity has read+exec permissions on `/bin/x86` and `/bin/x64`.

## Dependencies

This plugin requires `pdfium.dll` to be present, and `PdfiumViewer.dll`. Both 32-bit and 64-bit variants of pdfium.dll are supported. The recommended way to obtain pdfium.dll is via the `PdfiumViewer.Native.x86.v8-xfa` and `PdfiumViewer.Native.x86_64.v8-xfa` NuGet packages.

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
