using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImageResizer.Util;

// So the ImageResizer knows which bundle this assembly belongs to

[assembly: Edition("R4Elite")]
//Inform NativeDependencyManager where to find the download manifest
[assembly: NativeDependencies("Native.xml")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ImageResizer.Plugins.RedEye")]


// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d82fe812-a1b2-403b-b16e-70d4418db929")]
