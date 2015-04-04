This ImageResizer plugin requires native the pdfium DLL's for Windows to be included during runtime.
Both 32-bit and 64-bit pdfium DLL's are supported.

There are two options to include these native DLL's:

* Put either the 32-bit or 64-bit version of the pdfium DLL in the same directory as where the plugin is located;
* In the same directory as where the plugin is located, create an "x86" directory with the 32-bit DLL and an
  "x64" directory with the 64-bit DLL. The PdfiumViewer library will then automatically pick the correct
  one.

The pdfium DLL's can be downloaded from:

https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x86/pdfium.dll (32-bit)
https://github.com/pvginkel/PdfiumViewer/raw/master/Libraries/Pdfium/x64/pdfium.dll (64-bit)
