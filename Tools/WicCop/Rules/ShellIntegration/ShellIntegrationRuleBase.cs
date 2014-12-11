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
using System.IO;
using System.Text;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    abstract class ShellIntegrationRuleBase : RuleBase<ExtensionRuleGroup>
    {
        protected const string PerceivedType = "PerceivedType";

        public ShellIntegrationRuleBase(string text)
            : base (text)
        {
        }

        protected abstract void Check(MainForm form, string ext, RegistryKey rk, DataEntry[] de);

        protected RegistryKey OpenSubKey(MainForm form, RegistryKey rk, string name, DataEntry[] de, ref bool exists)
        {
            if (rk == null)
            {
                return null;
            }
            RegistryKey res = rk.OpenSubKey(name);

            exists |= res != null;

            return res;
        }

        protected RegistryKey OpenSubKey(MainForm form, RegistryKey rk, string name, DataEntry[] de)
        {
            if (rk == null)
            {
                return null;
            }
            RegistryKey res = rk.OpenSubKey(name);
            if (res == null)
            {
                form.Add(this, Resources.MissingRegistryKey, de, new DataEntry(Resources.Key, string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", rk, name)));
            }

            return res;
        }

        protected string CheckStringValue(MainForm form, RegistryKey rk, string value, DataEntry[] de)
        {
            if (rk == null)
            {
                return null;
            }
            try
            {
                RegistryValueKind k = rk.GetValueKind(value);
                if (k != RegistryValueKind.String && k != RegistryValueKind.ExpandString)
                {
                    form.Add(this, Resources.UnexpectedRegistryValueType, de, new DataEntry(Resources.Value, value ?? Resources.RegistryValue_default), new DataEntry(Resources.Key, rk.ToString()), new DataEntry(Resources.Expected, new RegistryValueKind[] { RegistryValueKind.ExpandString, RegistryValueKind.String }), new DataEntry(Resources.Actual, k));

                    return null;
                }
                else
                {
                    string res = rk.GetValue(value, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

                    if (string.IsNullOrEmpty(res))
                    {
                        form.Add(this, Resources.EmptyRegistryValue, de, new DataEntry(Resources.Value, value ?? Resources.RegistryValue_default), new DataEntry(Resources.Key, rk.ToString()));
                    }

                    return res;
                }
            }
            catch (IOException)
            {
                form.Add(this, Resources.MissingRegistryValue, de, new DataEntry(Resources.Value, value ?? Resources.RegistryValue_default), new DataEntry(Resources.Key, rk.ToString()));

                return null;
            }
        }

        static string RegistryNormilize(string s)
        {
            StringBuilder sb = new StringBuilder();
            string c = s.Replace(",", " , ");
            while (c.Length != 0)
            {
                string add = null;

                if (c.Length == 1)
                {
                    add = c;
                    c = "";
                }
                else if (c[0] == '\"')
                {
                    int start = 1;

                    while (start < c.Length)
                    {
                        int idx = c.IndexOf('\"', start);
                    
                        if (idx < 0)
                        {
                            add = c.Substring(1).Replace("\"\"", "\"");
                            c = "";
                            break;
                        }
                        else if (idx + 1 < c.Length && c[idx + 1] == '\"')
                        {
                            start = idx + 2;
                        }
                        else
                        {
                            add = c.Substring(1, idx - 1).Replace("\"\"", "\"");
                            c = c.Substring(idx + 1).TrimStart();
                            break;
                        }
                    }
                }
                else
                {
                    int idx = c.IndexOf(' ');
                    if (idx < 0)
                    {
                        add = c;
                        c = "";
                    }
                    else
                    {
                        add = c.Substring(0, idx);
                        c = c.Substring(idx + 1).TrimStart();
                    }
                }

                add = Environment.ExpandEnvironmentVariables(add).Trim().ToLower();
                if (add.Length > 0)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append('\"');
                    sb.Append(add);
                    sb.Append('\"');
                }
            }

            return sb.ToString();
        }

        protected void CheckValue(MainForm form, RegistryKey rk, string value, string[] valid, DataEntry[] de)
        {
            string v = CheckStringValue(form, rk, value, de);
            if (!string.IsNullOrEmpty(v))
            {
                if (Array.FindIndex(valid, delegate(string obj) { return string.Compare(RegistryNormilize(v), RegistryNormilize(obj), StringComparison.OrdinalIgnoreCase) == 0; }) < 0)
                {
                    form.Add(this, Resources.UnexpectedRegistryValue, de, new DataEntry(Resources.Actual, v), new DataEntry(Resources.Expected, valid), new DataEntry(Resources.Value, value ?? Resources.RegistryValue_default), new DataEntry(Resources.Key, rk.ToString()));
                }
            }
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            string ext = Parent.Extension;
            DataEntry[] de = new DataEntry[] { new DataEntry(Resources.FileExtennsion, ext) };
            using (RegistryKey rk = OpenSubKey(form, Registry.ClassesRoot, ext, de))
            {
                if (rk != null)
                {
                    Check(form, ext, rk, de);
                }
            }
        }
    }
}
