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

[assembly: AssemblyCompany("Nathanael Jones")]
[assembly: AssemblyCopyright("Copyright © 2011 Nathanael Jones")]


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:/*
[assembly: AssemblyVersion("3.1.2.*")]
[assembly: AssemblyFileVersion("3.1.2.*")]
[assembly: AssemblyInformationalVersion("3-1-alpha-2")]

[assembly: Commit("git-commit-guid-here")]



// These commented out settings are for the build script to access
// [assembly: PackageName("Resizer")]
// [assembly: NugetVersion("3.1.2")]
// [assembly: DownloadServer("http://downloads.imageresizing.net/")]