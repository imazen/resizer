//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class ShellIntegrationRule : ShellIntegrationRuleBase
    {
        const string ContentType = "Content Type";

        public ShellIntegrationRule()
            : base (Resources.ShellIntegrationRuleGroup_Text)
        {
        }


        protected override void Check(MainForm form, string ext, RegistryKey rk, DataEntry[] de)
        {
            CheckValue(form, rk, ContentType, Parent.Mimes, de);
            CheckValue(form, rk, PerceivedType, new string[] { "image" }, de);
        }
    }
}
