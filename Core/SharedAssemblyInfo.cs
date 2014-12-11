using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using ImageResizer.Util;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#if TRIAL
[assembly: BuildType("trial")]
[assembly: AssemblyProduct("Image Resizer Plugin - Trial Version")]
#else
[assembly: AssemblyProduct("Image Resizer")]
#endif

[assembly: AssemblyCompany("Imazen LLC")]
[assembly: AssemblyCopyright("Copyright © 2013 Imazen LLC")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:/*
[assembly: AssemblyVersion("3.4.4.*")]
[assembly: AssemblyFileVersion("3.4.4.*")]
[assembly: AssemblyInformationalVersion("3-4-4")]

[assembly: Commit("git-commit-guid-here")]



// These commented out settings are for the build script to access
// [assembly: PackageName("Resizer")]
// [assembly: NugetVersion("3.4.4")]
// [assembly: DownloadServer("http://downloads.imageresizing.net/")]
