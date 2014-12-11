//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.Com
{
    class ComRule : RuleBase<ComponentRuleGroup>, IWowRegistryChecked
    {
        public ComRule()
            : base(Resources.ComRule_Text)
        {
        }

        private string ClsidKey
        {
            get { return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}", Parent.Clsid); }
        }

        private string InprocServer32Key
        {
            get { return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}\\InprocServer32", Parent.Clsid); }
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(InprocServer32Key))
            {
                if (rk == null)
                {
                    form.Add(this, Resources.ComRegistrationMissing);
                }
                else
                {
                    string tm = rk.GetValue("ThreadingModel", null) as string;
                    if (tm != "Both")
                    {
                        form.Add(this, Resources.BothTreadingModelNotSupported, new DataEntry(Resources.Expected, "Both"), new DataEntry(Resources.Actual, tm));
                    }
                }
            }
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            yield return ClsidKey;
        }
        #endregion
    }
}
