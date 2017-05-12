// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/

namespace ImageResizer.Plugins.LicenseVerifier
{
    partial class LicenseEnforcer<T> 
    {
        private const bool EnforcementEnabled = false;

        private static string LicenseDiagnosticsBanner
        {
            get
            {
                return "\n----------------\nDRM-free: License enforcement is off\n----------------\n";
            }
        }
    }
}

