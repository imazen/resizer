Tags: plugin
Edition: free
Tagline: Render PDFs to images
Aliases: /plugins/pdfiumrenderer

# PdfiumRenderer plugin

*PLEASE NOTE*
* **This plugin is not forwards-compatible. Avoid usage for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do NOT use with untrusted data. This plugin passes source files to Pdfium, which (like all software with embedded codecs), has a history of vulnerabilities.**
* **Using this plugin with malicious PDFs could result in a remote code execution vulnerability.**
* **We only provide a the version of pdfium.dll we used for development. You should locate and use the latest release of pdfium for optimal security.**
* You can view [recent PDFium vulnerabilities here](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=pdfium) and [here](https://web.nvd.nist.gov/view/vuln/search-results?query=pdfium&search_type=all&cves=on)

## Install manually

1. Add `ImageResizer.Plugins.PdfiumRenderer.dll` as a reference to the project, or put a copy of it in the /bin folder.
2. Add `PdfiumViewer.dll` as a reference to the project, or put a copy of it in the /bin folder.
3. In the ImageResizer `<plugins />` section of web.config, insert `<add name="PdfiumRenderer" downloadNativeDependencies="true"/>`. This setting tells the project to attempt to download the pdfium.dll dependency automatically.
4. Alternatively, in the ImageResizer `<plugins />` section in web.config or app.config, insert `<add name="PdfiumRenderer" downloadNativeDependencies="false"/>`. Download the 32-bit and/or 64-bit version of the pdfium.dll yourself, and either
  * Put the 32-bit or 64-bit version of `pdfium.dll` in the /bin folder, or
  * Create `/bin/x86` and `/bin/x64`, putting the 32-bit copy of `pdfium.dll` in the "x86" folder and the 64-bit copy of `pdfium.dll` in the "x64" folder. The correct bitness is then chosen automatically at runtime.
5. In an IIS web application scenario, ensure the relevant application pool identity has read+exec permissions on `/bin/x86` and `/bin/x64`.

## Install via nuget

1. `Install-Package ImageResizer.Plugins.PdfiumRenderer`. Be sure to install with the `PdfiumViewer` dependency.
2. In the ImageResizer `<plugins />` section of web.config, insert `<add name="PdfiumRenderer" downloadNativeDependencies="true"/>`.
3. Alternatively, in the ImageResizer `<plugins />` section of web.config, insert `<add name="PdfiumRenderer" downloadNativeDependencies="false"/>`. Download the 32-bit and/or 64-bit version of the pdfium.dll yourself, and either
  * Put the 32-bit or 64-bit version of the pdfium.dll in the /bin folder, or
  * Create `/bin/x86` and `/bin/x64`, putting the 32-bit copy of pdfium.dll in the `x86` folder and the 64-bit copy of pdfium.dll in the `x64` folder. The correct bitness is then chosen automatically at runtime.
4. In an IIS web application scenario, ensure the relevant application pool identity has read+exec permissions on /bin/x86 and /bin/x64.

## Dependencies

This plugin requires `pdfium.dll` to be present, and `PdfiumViewer.dll`. Both 32-bit and 64-bit variants of pdfium.dll are supported.

The pdfium DLLs can be downloaded from:

https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x86/pdfium.dll (32-bit)
https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x64/pdfium.dll (64-bit)

The PdfiumViewer DLL can be downloaded as part of the ImageResizer suite at:

http://imageresizing.net/download

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

