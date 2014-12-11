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
using System.Runtime.InteropServices;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;

namespace Microsoft.Test.Tools.WicCop.Rules
{
    class AllComponentsRuleGroup : RuleBase
    {
        public AllComponentsRuleGroup(string text, WICComponentType type, Action<ComponentRuleGroup> childCreator, Remote remote)
            : base (text)
        {
            foreach (var p in GetComponentInfoPairs(type, remote))
            {
                Nodes.Add(new ComponentRuleGroup(p.Key, p.Value, childCreator));
            }
        }

        public static IEnumerable<KeyValuePair<Guid, string>> GetComponentInfoPairs(WICComponentType type, Remote remote)
        {
            HashSet<Guid> guids = new HashSet<Guid>();

            foreach (IWICComponentInfo ci in GetComponentInfos(type))
            {
                Guid clsid;
                ci.GetCLSID(out clsid);
                guids.Add(clsid);

                string s = Extensions.GetString(ci.GetFriendlyName);

                if (string.IsNullOrEmpty(s))
                {
                    s = string.Format(CultureInfo.CurrentUICulture, "CLSID: {0}", clsid);
                }

                yield return new KeyValuePair<Guid, string>(clsid, s);
            }

            if (remote != null)
            {
                foreach (var p in remote.GetComponentInfoPairs(type))
                {
                    if (!guids.Contains(p.Key))
                    {
                        yield return p;
                    }
                }
            }
        }

        public static IEnumerable<IWICComponentInfo> GetComponentInfos(WICComponentType type)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IEnumUnknown eu = null;
            object[] o = new object[1];

            try
            {
                eu = factory.CreateComponentEnumerator(type, WICComponentEnumerateOptions.WICComponentEnumerateRefresh | WICComponentEnumerateOptions.WICComponentEnumerateDisabled);

                int hr = 0;
                while (hr == 0)
                {
                    uint fetched = 0;
                    hr = eu.Next(1, o, ref fetched);
                    Marshal.ThrowExceptionForHR(hr);
                    if (fetched == 1)
                    {
                        IWICComponentInfo ci = (IWICComponentInfo)o[0];
                        try
                        {
                            yield return ci;
                        }
                        finally
                        {
                            ci.ReleaseComObject();
                        }
                    }
                }
            }
            finally
            {
                factory.ReleaseComObject();
                eu.ReleaseComObject();
            }
        }
    }
}
