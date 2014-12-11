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

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class PropertyStoreIntegrationRule : ShellIntegrationRuleBase, IWowRegistryChecked
    {
        const string PhotoMetadataHandler = "{a38b883c-1682-497e-97b0-0a3a9e801682}";
        static readonly string[] PhotoMetadataHandlerContainerFormats = new string[] { Consts.GUID_ContainerFormatTiff.ToString("B"), Consts.GUID_ContainerFormatJpeg.ToString("B") };

        public PropertyStoreIntegrationRule()
            : base(Resources.PropertyStoreIntegrationRule_Text)
        {
        }

        private string PropertyHandler
        {
            get { return string.Format(CultureInfo.InvariantCulture, @"SOFTWARE\Microsoft\Windows\CurrentVersion\PropertySystem\PropertyHandlers\{0}", Parent.Extension); }
        }

        protected override void Check(MainForm form, string ext, RegistryKey rk, DataEntry[] de)
        {
            using (RegistryKey r = OpenSubKey(form, Registry.LocalMachine, PropertyHandler, de))
            {
                string value = CheckStringValue(form, r, null, de);
                if (value != null)
                {
                    if (value == PhotoMetadataHandler)
                    {
                        using (RegistryKey r2 = OpenSubKey(form, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\PhotoPropertyHandler\ContainerAssociations", de))
                        {
                            foreach (Guid g in Parent.ContainerFormats)
                            {
                                if (0 <= Array.FindIndex(r2.GetValueNames(), delegate(string obj) { return string.Compare(obj, g.ToString("B"), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0; }))
                                {
                                    CheckValue(form, r2, g.ToString("B"), PhotoMetadataHandlerContainerFormats, de);
                                }
                            }
                        }
                    }
                    else
                    {
                        Guid? clsid = null;
                        try
                        {
                            clsid = new Guid(value);
                        }
                        catch (ArgumentException)
                        {
                            form.Add(this, Resources.InvalidGUIDRegistryValue, new DataEntry(Resources.Key, r.ToString()), new DataEntry(Resources.Value, Resources.RegistryValue_default));
                        }
                        if (clsid.HasValue)
                        {
                            Type t = Type.GetTypeFromCLSID(clsid.Value, false);
                            if (t == null)
                            {
                                object ps = null;
                                try
                                {
                                    ps = Activator.CreateInstance(t);
                                }
                                catch (Exception e)
                                {
                                    form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_Failed, "CoCreateInstance(...)"), new DataEntry(e), new DataEntry(Resources.CLSID, clsid));
                                }
                                finally
                                {
                                    ps.ReleaseComObject();
                                }
                            }
                        }
                    }
                }
            }
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            string res = Registry.GetValue(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", Registry.LocalMachine, PropertyHandler), null, null) as string;
            if (!string.IsNullOrEmpty(res) && string.Compare(res, PhotoMetadataHandler, StringComparison.OrdinalIgnoreCase) != 0)
            {
                yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{0}", res);
            }
        }
        #endregion
    }
}
