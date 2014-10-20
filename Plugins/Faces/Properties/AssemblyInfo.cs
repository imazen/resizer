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
[assembly: AssemblyTitle("ImageResizer.Plugins.Faces")]


[assembly: ComVisibleAttribute(true)]
