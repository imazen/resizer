Aliases: /plugins/pdfrenderer
Tags: Plugin
Bundle: free
Edition: free
Tagline: Obsolete. See PdfiumRenderer. 

# PdfRenderer

*PLEASE NOTE*
* **This plugin has been deprecated in favor of PdfiumRenderer**
* **This plugin uses the Ghostscript library, which is under the GPLv3. As such, any binaries using that library are also under the GPL v3. [Click here for the full licensing details on this](/licenses/pdfrenderer).**
* **This plugin is not forwards-compatible. Avoid usage for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do NOT use with untrusted data. This plugin passes source files to potentially vulnerable software (Ghostscript).**
* **Using this plugin with malicious PDFs could result in a remote code execution vulnerability.**
* You can find [recent Ghostscript vulnerabilities here](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=ghostscript)


Jason Morse is the original author of this plugin. 

The PdfRenderer plugin renders PDF files to the dimensions specified by the `width` and `height` commands. You may use the `mode` command to pad, crop, stretch, or seam carve the result to match your desired aspect ratio. All resizer commands can be used in combination with the plugin.

To access, add `?format=png` or `format=jpg` after a PDF url. 

Ex. `/docs.pdf?format=png&width=400`.

**Ghostscript does not support multiple instances per process. This means your application needs a dedicated application pool, and you MUST disable overlapped recycles or you're going to get intermittent errors and failed requests. Once Ghostscript stops working, you usually have to restart the app pool to fix it.**

### Requirements

* gsdll32.dll or gsdll64.dll in the /bin directory, depending on application bitness. Including both is a good idea, in case you need to change the bitness unexpectedly.

### Configuration

The default render size is 800x600, and the maximum render size is 4000x4000. This may be XML configurable in future releases.

### Syntax

This plugin is activated when any PDF url has one of the [resizer commands](/docs/reference) in the querystring. 

It uses the `width` and `height` commands to optimize the PDF rendering to the desired size. Any image processing commands may be used later, but it is not compatible with the WicBuilder or FreeImageBuilder pipelines. 

* Width/height - Choose the dimensions of the output image. The pdf will be rendered within the box, and padded to fit. You can use &mode=crop to crop the pdf to a specific aspect ratio, or &mode=max to just allow the output image to match the aspect ratio of the PDF.
* Pdfwidth/pdfheight - You can use pdfwidth and pdfheight to specify alternate rendering dimensions for the PDF, causing it to be resized after it is generated. This is useful for making high-quality thumbnails, as rendering a PDF to an 80x80px square can be too aliased. Specifying &width=80&pdfwidth=240 can often provide much better results than &width=80 by itself. (3.1.5+)
* page=X - (default - page 1) Choose the page you wish to render.
* gridfit=true|false (default - false) When true, uses TrueType grid fitting for glyph rendering.
* subpixels=true|false (default - false) When true, fonts are rendered to a subpixel grid instead of the pixel great.

### Limitations

Ghostscript does not provide size information for individual pages. As a result, every page in a PDF will be rendered on a canvas, which has the size of the largest page in the document. It is possible to compensate for this behavior by using [the WhitespaceTrimmer](/plugins/whitespacetrimmer) plugin, using `&trim.threshold=100`. 

**Ghostscript does not support multiple instances per process. This means your application needs a dedicated application pool, and you MUST disable overlapped recycles or you're going to get intermittent errors and failed requests. Once Ghostscript stops working, you usually have to restart the app pool to fix it.**

### Performance characteristics

PDF rendering can be an intensive operation, and is highly dependent on the PDF and server. Expect 250 to 1000 milliseconds. This plugin should be combined with the [DiskCache](/plugins/diskcache) plugin for best performance.

## Installation

1. Add ImageResizer.Plugins.PdfRenderer.dll to your project or run `Install-Package ImageResizer.Plugins.PdfRenderer` in the NuGet package manager.
2. Add `<add name="PdfRenderer" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. If you set `downloadNativeDependencies="false"` or you're running < V3.2, place gsdll32.dll and gsdll64.dll in the /bin directory.

## Where do I get gsdll32 and gsdll64?

The download doesn't include gsdll32 and gsdll64 because they would add 18MB to the download size, and there are new versions on a frequent basis.

The dlls for version 9.04 (Jan 12 2012) can be [downloaded here](http://downloads.imageresizing.net/GhostScript_9_04.zip), but for newer releases you'll have to download and extract the dlls from SourceForge yourself.

To get the very latest version

1. Visit the [Ghostscript Sourceforge page](http://sourceforge.net/projects/ghostscript/)
2. Download both the 32-bit and 64-bit EXE files for the latest release.
3. Download and install [the world's most awesome (de)compression utility (7-Zip)](http://7-zip.org).
4. Right click on each EXE file and extract them to a folder.
5. In each extracted folder, go to `$_OUTDIR\bin` to find `gsdll32.dll` and `gsdll64.dll`
