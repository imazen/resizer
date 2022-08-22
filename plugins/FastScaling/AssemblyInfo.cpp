// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
#include "stdafx.h"

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security::Permissions;
using namespace ImageResizer::Util;
//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly:AssemblyTitleAttribute(L"ImageResizer.Plugins.FastScaling")];

// So the ImageResizer knows which bundle this assembly belongs to
[assembly:EditionAttribute ("R4Performance")];

[assembly:ComVisible(false)];

[assembly:CLSCompliantAttribute(true)];


// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#if TRIAL
[assembly:BuildTypeAttribute ("trial")];
[assembly:AssemblyProductAttribute ("Image Resizer Plugin - requires license key")];
#else
[assembly:AssemblyProductAttribute ("Image Resizer")];
#endif

[assembly:AssemblyCompanyAttribute ("Imazen LLC")];
[assembly:AssemblyCopyrightAttribute (L"Copyright © 2017 Imazen LLC")];

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:/*
[assembly: AssemblyVersionAttribute("4.0.0.0")];
[assembly: AssemblyFileVersionAttribute("4.1.3.745")];
[assembly: AssemblyInformationalVersionAttribute("4.1.3-prerelease.745")];

[assembly: CommitAttribute("2c64c3100e64c944222e04388a6d44b2909d4115")];

[assembly: BuildDateAttribute("2017-05-05T12:13:13.1434691+00:00")];

// These commented out settings are for the build script to access
// [assembly: PackageName("Resizer")]
// [assembly: NugetVersionAttribute("4.0.0-prerelease")]
// [assembly: DownloadServer("http://downloads.imageresizing.net/")]
