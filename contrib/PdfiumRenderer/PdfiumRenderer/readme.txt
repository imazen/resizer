This ImageResizer plugin requires native pdfium DLLs for Windows to be included during runtime.
Both 32-bit and 64-bit pdfium DLLs are supported.

There are two options to include these native DLLs:

* Put either the 32-bit or 64-bit version of the pdfium DLL in the same directory as where the plugin is located;
* In the same directory as where the plugin is located, create an "x86" directory with the 32-bit DLL and an
  "x64" directory with the 64-bit DLL. The PdfiumViewer library will then automatically pick the correct
  one.

 Native files can be downloaded from https://github.com/pvginkel/PdfiumBuild/tree/master/Builds/2018-04-08 or (for the most updated version) built from source code from https://pdfium.googlesource.com/pdfium/