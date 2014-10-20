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
using System.Linq;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.Properties;
using System.IO;

namespace Microsoft.Test.Tools.WicCop.Rules.Wow
{
    class WowRule : RuleBase
    {
        public WowRule()
            : base(Resources.X86)
        {
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            form.RunParentRemotely(this);

            IWowRegistryChecked r = Parent as IWowRegistryChecked;
            if (r != null)
            {
                foreach (string key in r.GetKeys())
                {
                    CheckRegistry(form, key, r);
                }
            }
        }

        private void CheckRegistry(MainForm form, string key, IWowRegistryChecked parent)
        {
            using (RegistryKey x64 = Registry.ClassesRoot.OpenSubKey(key))
            {
                string wowKey = string.Format(CultureInfo.InvariantCulture, "Wow6432Node\\{0}", key);
                using (RegistryKey wow = Registry.ClassesRoot.OpenSubKey(wowKey))
                {
                    if (x64 == null || wow == null)
                    {
                        if (x64 != wow)
                        {
                            form.Add(this, Resources.MissingRegistryKey, new DataEntry(Resources.Key, string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", Registry.ClassesRoot, wow == null ? wowKey : key)));
                        }
                    }
                    else
                    {
                        foreach (string s in new HashSet<string>(x64.GetValueNames().Union(wow.GetValueNames()), StringComparer.OrdinalIgnoreCase))
                        {
                            RegistryValueKind kind = RegistryValueKind.Unknown;
                            RegistryValueKind kindWow = RegistryValueKind.Unknown;
                            try
                            {
                                kind = x64.GetValueKind(s);
                                kindWow = wow.GetValueKind(s);
                            }
                            catch (IOException)
                            {
                                form.Add(this, Resources.MissingRegistryValue, new DataEntry(Resources.Key, (kind == RegistryValueKind.Unknown ? x64 : wow).ToString()), new DataEntry(Resources.Value, s));
                            }
                            if (kind != RegistryValueKind.Unknown && kindWow != RegistryValueKind.Unknown)
                            {
                                if (kind != kindWow)
                                {
                                    form.Add(this, Resources.InconsistentRegistryValueType, new DataEntry(Resources.Key, x64.ToString()), new DataEntry(Resources.WowKey, wow.ToString()), new DataEntry(Resources.Value, s), new DataEntry(Resources.X86, kindWow), new DataEntry(Resources.Amd64, kind));
                                }
                                else
                                {
                                    object value = x64.GetValue(s, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                                    object valueWow = wow.GetValue(s, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                                    if (!object.Equals(value, valueWow))
                                    {
                                        string[] strings = value as string[];
                                        if (strings == null || !strings.ItemsEqual(valueWow as string[]))
                                        {
                                            byte[] bytes = value as byte[];
                                            if (bytes == null || !bytes.ItemsEqual(valueWow as byte[]))
                                            {
                                                form.Add(this, Resources.InconsistentRegistryValue, new DataEntry(Resources.Key, x64.ToString()), new DataEntry(Resources.WowKey, wow.ToString()), new DataEntry(Resources.Value, string.IsNullOrEmpty(s) ? Resources.RegistryValue_default : value), new DataEntry(Resources.X86, value), new DataEntry(Resources.Amd64, valueWow));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        foreach (string s in new HashSet<string>(x64.GetSubKeyNames().Union(wow.GetSubKeyNames()), StringComparer.OrdinalIgnoreCase))
                        {
                            CheckRegistry(form, string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", key, s), parent);
                        }
                    }
                }
            }
        }
    }
}
