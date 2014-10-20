This ImageResizer plugin requires native Ghostscript DLL for Windows to be included during runtime.
Both 32-bit and 64-bit Ghostscript DLLs are supported:

- 32-bit requires gsdll32.dll
- 64-bit requires gsdll64.dll

You must include the Ghostscript DLL appropriate for the process (either 32 or 64 bit). 
Both DLLs can included but only the correct one will be used.

Ghostscript can be downloaded from:
http://sourceforge.net/projects/ghostscript/
Here are direct links for V9.04. Use a newer version if available.
http://sourceforge.net/projects/ghostscript/files/GPL%20Ghostscript/9.04/gs904w64.exe/download   (64-bit)
http://sourceforge.net/projects/ghostscript/files/GPL%20Ghostscript/9.04/gs904w32.exe/download	 (32-bit)

You'll need to download the 32-bit and 64-bit exe files separately, then use 7-Zip (http://7-zip.org) to extract each to a folder. 

Inside the extracted folder, go to $_OUTDIR\bin to find the dll. 