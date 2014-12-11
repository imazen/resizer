//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class ThumnailCacheIntegrationRule : ShellIntegrationRuleBase
    {
        const string ThumnailCacheGuid = "{C7657C4A-9F68-40FA-A4DF-96BC08EB3551}";
        const string ShellExGuid = "{E357FCCD-A995-4576-B01F-234630154E96}";

        public ThumnailCacheIntegrationRule()
            : base(Resources.ThumnailCacheIntegrationRule_Text)
        {
        }

        protected override void Check(MainForm form, string ext, RegistryKey rk, DataEntry[] de)
        {
            RegistryKey r = null;
            
            using (r = Registry.ClassesRoot.OpenSubKey(string.Format(CultureInfo.InvariantCulture, "{0}\\ShellEx\\{1}", ext, ShellExGuid)))
            {
                CheckValue(form, r, null, new string[] { ThumnailCacheGuid }, de);
            }
            
            if (r == null)
            {
                using (r = Registry.ClassesRoot.OpenSubKey(string.Format(CultureInfo.InvariantCulture, "SystemFileAssociations\\{0}\\ShellEx\\{1}", ext, ShellExGuid)))
                {
                    CheckValue(form, r, null, new string[] { ThumnailCacheGuid }, de);
                }
            }

            if (r == null)
            {
                string type = CheckStringValue(form, rk, PerceivedType, de);

                using (r = OpenSubKey(form, Registry.ClassesRoot, string.Format(CultureInfo.InstalledUICulture, "SystemFileAssociations\\{0}\\ShellEx\\{1}", type, ShellExGuid), de))
                {
                    CheckValue(form, r, null, new string[] { ThumnailCacheGuid }, de);
                }
            }
        }
    }
}
