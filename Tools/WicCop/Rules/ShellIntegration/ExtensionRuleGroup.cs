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

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class ExtensionRuleGroup : RuleBase
    {
        readonly string extension;
        readonly HashSet<string> mimes;
        readonly HashSet<Guid> containerFormats;
        readonly string progid;
        readonly string fileExtName;

        public ExtensionRuleGroup(string extension, HashSet<string> mimes, HashSet<Guid> containerFormats)
            : base(extension)
        {
            this.extension = extension;
            this.mimes = mimes;
            this.containerFormats = containerFormats;

            using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(extension))
            {
                if (rk != null)
                {
                    progid = rk.GetValue(null, null) as string;
                }
            }
            if (!string.IsNullOrEmpty(progid))
            {
                using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(progid))
                {
                    if (rk != null)
                    {
                        fileExtName = rk.GetValue(null, null) as string;
                        if (!string.IsNullOrEmpty(fileExtName))
                        {
                            Text = string.Format(CultureInfo.CurrentUICulture, "{1} ({0})", extension, fileExtName);
                        }
                    }
                }
            }

            Nodes.Add(new ShellIntegrationRule());
            Nodes.Add(new PropertyStoreIntegrationRule());
            Nodes.Add(new PhotoGalleryIntegrationRule());
            Nodes.Add(new ThumnailCacheIntegrationRule());
        }

        public string[] Mimes
        {
            get { return mimes.ToArray(); }
        }

        public Guid[] ContainerFormats
        {
            get { return containerFormats.ToArray(); }
        }

        public string Extension
        {
            get { return extension; }
        }

        public string FileTypeName
        {
            get { return string.IsNullOrEmpty(fileExtName) ? string.Format(CultureInfo.CurrentUICulture, Resources._0_File, Extension) : fileExtName; }
        }
    }
}
