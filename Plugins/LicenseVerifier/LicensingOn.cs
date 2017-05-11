// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
using ImageResizer.Configuration.Issues;
using ImageResizer.Resizing;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Plugins.LicenseVerifier
{
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider, ILicenseDiagnosticsProvider
    {
        private const bool EnforcementEnabled = true;

        private static string LicenseDiagnosticsBanner
        {
            get
            {
                return "\n----------------\nLicense enforcement is active\n----------------\n";
            }
        } 
    }
}
