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

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class ShellIntegrationRuleGroup : RuleBase
    {
        public ShellIntegrationRuleGroup(Remote remote)
            : base(Resources.ShellIntegrationRule_Text)
        {
            foreach (var p in GetExtensions(remote))
            {
                Nodes.Add(new ExtensionRuleGroup(p.Key, p.Value.Key, p.Value.Value));
            }
        }

        public static Dictionary<string, KeyValuePair<HashSet<string>, HashSet<Guid>>> GetExtensions(Remote remote)
        {
            Dictionary<string, KeyValuePair<HashSet<string>, HashSet<Guid>>> res = remote == null ? new Dictionary<string, KeyValuePair<HashSet<string>, HashSet<Guid>>>(StringComparer.OrdinalIgnoreCase) : remote.GetExtensions();

            foreach (IWICBitmapCodecInfo ci in AllComponentsRuleGroup.GetComponentInfos(WICComponentType.WICDecoder))
            {
                Guid cf;
                ci.GetContainerFormat(out cf);
                foreach (string ext in Extensions.GetString(ci.GetFileExtensions).Split(','))
                {
                    KeyValuePair<HashSet<string>, HashSet<Guid>> data;
                    if (!res.TryGetValue(ext, out data))
                    {
                        data = new KeyValuePair<HashSet<string>, HashSet<Guid>>(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<Guid>());
                        res.Add(ext, data);
                    }
                    foreach (string mime in Extensions.GetString(ci.GetMimeTypes).Split(','))
                    {
                        data.Key.Add(mime);
                    }
                    data.Value.Add(cf);
                }
            }

            return res;
        }
    }
}
