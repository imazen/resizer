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
using System.Text.RegularExpressions;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules
{
    static class ComponentInfoHelper
    {
        public delegate void GeGuidMethod(out Guid guid);

        public static readonly Regex ExtensionMask = new Regex(@"^\.(\w+)$");
        public static readonly Regex MimeMask = new Regex(@"^\w+((-|\.)(\w+))*/\w+((-|\.)(\w+))*$");
        static readonly Regex VersionMask = new Regex(@"^[0-9]+[\.[0-9]+]{1,3}$");

        public static string[] CheckCommaSeparatedString(MainForm form, Extensions.GetStringMethod method, Regex mask, RuleBase rule)
        {
            HashSet<string> res = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            HashSet<string> dups = new HashSet<string>();
            foreach (string s in CheckNotEmptyString(form, method, rule).Split(','))
            {
                if (res.Contains(s))
                {
                    dups.Add(s);
                }
                else if (!mask.IsMatch(s))
                {
                    form.Add(rule, method.ToString(Resources._0_DontMatchMask), new DataEntry(Resources.Value, s));
                }
                else
                {
                    res.Add(s);
                }
            }
            if (dups.Count > 0)
            {
                form.Add(rule, method.ToString(Resources._0_Duplicated), new DataEntry(Resources.Value, dups.ToArray()));
            }

            return res.ToArray();
        }

        public static string CheckNotEmptyString(MainForm form, Extensions.GetStringMethod method, RuleBase rule)
        {
            string s = Extensions.GetString(method);
            if (string.IsNullOrEmpty(s))
            {
                form.Add(rule, method.ToString(Resources._0_EmptyString));
            }

            return s;
        }

        public static void CheckNotReserverdGuid(MainForm form, GeGuidMethod method, RuleBase rule)
        {
            Guid guid;
            method(out guid);

            string s;
            if (ReservedGuids.Instance.TryGetValue(guid, out s))
            {
                form.Add(rule, method.ToString(Resources._0_ReservedGUID), new DataEntry(Resources.Value, guid));
            }
        }

        public static void CheckVersion(MainForm form, Extensions.GetStringMethod method, RuleBase rule)
        {

            string version = CheckNotEmptyString(form, method, rule);
            if (!string.IsNullOrEmpty(version))
            {
                if (VersionMask.IsMatch(version))
                {
                    form.Add(rule, method.ToString(Resources._0_DontMatchMask), new DataEntry(Resources.Value, version));
                }
            }
        }

        public static void CheckEquals<TInfo>(MainForm form, Func<TInfo> method, RuleBase<ComponentRuleGroup> rule, Func<TInfo, TInfo, DataEntry[][]> compare)
            where TInfo : class, IWICComponentInfo
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            TInfo info = factory.CreateComponentInfo(rule.Parent.Clsid) as TInfo;
            TInfo i = null;
            try
            {
                i = method();
                if (i == null)
                {
                    form.Add(rule, method.ToString(Resources._0_NULL));
                }
                else 
                {
                    foreach (DataEntry[] de in compare(info, i))
                    {
                        form.Add(rule, string.Format(CultureInfo.CurrentUICulture, Resources.InconsistentComponentInfo, typeof(TInfo).Name, method.ToString("{0}")), de);
                    }
                }
            }
            catch (Exception e)
            {
                form.Add(rule, method.ToString(Resources._0_Failed), new DataEntry(e));
            }
            finally
            {
                i.ReleaseComObject();
                info.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        public static void Check<TInfo, TTag>(MainForm form, Guid clsid, Action<MainForm, TInfo, TTag> check, TTag tag, RuleBase rule, bool checkVersions)
            where TInfo : class, IWICComponentInfo
            where TTag : class
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            TInfo info = null;
            try
            {
                info = (TInfo)factory.CreateComponentInfo(clsid);

                CheckNotEmptyString(form, info.GetAuthor, rule);
                CheckNotEmptyString(form, info.GetFriendlyName, rule);

                if ((info.GetSigningStatus() & WICComponentSigning.WICComponentDisabled) == WICComponentSigning.WICComponentDisabled)
                {
                    form.Add(rule, Resources.ComponentDisabled);
                }
                
                CheckNotReserverdGuid(form, info.GetVendorGUID, rule);

                if (checkVersions || info.GetSpecVersion(0, null) > 0)
                {
                    CheckVersion(form, info.GetSpecVersion, rule);
                }
                if (checkVersions || info.GetVersion(0, null) > 0)
                {
                    CheckVersion(form, info.GetVersion, rule);
                }

                check(form, info, tag);
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
            }
        }
    }
}
