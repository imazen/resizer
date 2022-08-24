using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ImageResizer.Util;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ImageResizer.Plugins.PdfRenderer")]
[assembly: AssemblyDescription("Ghostscript-based plugin for PDF support.")]



//Inform NativeDependencyManager where to find the download manifest
[assembly: NativeDependencies("Native.xml")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6ab5d794-eb33-480e-81d2-eb2a06c3959a")]
