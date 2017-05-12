// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/

using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider,
        ILicenseDiagnosticsProvider
    {
        const bool EnforcementEnabled = true;

        static string LicenseDiagnosticsBanner =>
            "\n----------------\nLicense enforcement is active\n----------------\n";
    }
}
