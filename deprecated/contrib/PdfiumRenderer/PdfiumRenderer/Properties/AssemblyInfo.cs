using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ImageResizer.Util;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ImageResizer.Plugins.PdfiumRenderer")]
[assembly: AssemblyDescription("PDFium-based plugin for PDF support.")]


#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ecb21814-b1f2-485c-ae72-1ceb1a150d9e")]
