//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Drawing;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.ShellIntegration
{
    class PhotoGalleryIntegrationRule : ShellIntegrationRuleBase
    {
        const string PhotoGalleryGuid = "{FFE2A43C-56B9-4BF5-9A79-CC6D4285608A}";
        const string PhotoViewerDll = "PhotoViewer.dll";
        readonly static string PhotoGalleryPath = GetPhotoGalleryPath();

        public PhotoGalleryIntegrationRule()
            : base(Resources.PhotoGalleryIntegrationRule_Text)
        {
        }

        static string GetPhotoGalleryPath()
        {
            return string.Format(CultureInfo.InvariantCulture, "%ProgramFiles%\\Windows Photo {0}\\{1}",
                Environment.OSVersion.Version.Major <= 6 && Environment.OSVersion.Version.Minor < 1 ? "Gallery" : "Viewer",
                PhotoViewerDll);
        }

        protected override void Check(MainForm form, string ext, RegistryKey rk, DataEntry[] de)
        {
            bool openWith = false;
            bool imagePreview = false;

            string progid = CheckStringValue(form, rk, null, de);
            if (!string.IsNullOrEmpty(progid))
            {
                using (RegistryKey r = OpenSubKey(form, rk, "OpenWithProgids", de))
                {
                    if (r != null)
                    {
                        if (Array.IndexOf(r.GetValueNames(), progid) < 0)
                        {
                            form.Add(this, Resources.MissingRegistryValue, de, new DataEntry(Resources.Value, progid), new DataEntry(Resources.Key, rk.ToString()));
                        }
                    }
                }
                using (RegistryKey r = OpenSubKey(form, rk, string.Format(CultureInfo.InvariantCulture, "OpenWithList\\{0}", PhotoViewerDll), de, ref openWith))
                {
                }
                using (RegistryKey r = OpenSubKey(form, rk, "ShellExt\\ContextMenuHandlers\\ShellImagePreview", de, ref imagePreview))
                {
                    CheckValue(form, r, null, new string[] { PhotoGalleryGuid }, de);
                }

                using (RegistryKey r = OpenSubKey(form, Registry.ClassesRoot, progid, new DataEntry[0]))
                {
                    CheckStringValue(form, r, null, de);

                    using (RegistryKey r1 = OpenSubKey(form, r, "DefaultIcon", new DataEntry[0]))
                    {
                        string iconPath = CheckStringValue(form, r1, null, de);
                        if (!string.IsNullOrEmpty(iconPath))
                        {
                            using (TempFileCollection t = new TempFileCollection())
                            {
                                string file = t.AddExtension(ext);
                                File.WriteAllBytes(file, new byte[0]);
                                try
                                {
                                    Icon.ExtractAssociatedIcon(file).Dispose();
                                }
                                catch (Exception e)
                                {
                                    form.Add(this, Resources.CannotExtractIcon, de, new DataEntry(Resources.Key, r1.ToString()), new DataEntry(Resources.Value, Resources.RegistryValue_default), new DataEntry(Resources.Actual, iconPath), new DataEntry(e));
                                }
                            }
                        }
                    }

                    using (RegistryKey r1 = OpenSubKey(form, r, "shell\\open\\command", new DataEntry[0]))
                    {
                        CheckValue(form, r1, null, new string[] { string.Format(CultureInfo.InvariantCulture, "%SystemRoot%\\System32\\rundll32.exe \"{0}\", ImageView_Fullscreen %1", PhotoGalleryPath) }, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(form, r, "shell\\open", new DataEntry[0]))
                    {
                        CheckValue(form, r1, "MuiVerb", new string[] { string.Format(CultureInfo.InvariantCulture, "@{0},-3043", PhotoGalleryPath) }, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(form, r, "shell\\open\\DropTarget", new DataEntry[0]))
                    {
                        CheckValue(form, r1, "Clsid", new string[] { PhotoGalleryGuid }, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(form, r, "shell\\printto\\command", new DataEntry[0]))
                    {
                        CheckValue(form, r1, null, new string[] { "%SystemRoot%\\System32\\rundll32.exe \"%SystemRoot%\\System32\\shimgvw.dll\", ImageView_PrintTo /pt \"%1\" \"%2\" \"%3\" \"%4\"" }, de);
                    }
                }
            }

            using (RegistryKey r = OpenSubKey(form, Registry.ClassesRoot, string.Format(CultureInfo.InvariantCulture, "SystemFileAssociations\\{0}", ext), new DataEntry[0]))
            {
                using (RegistryKey r1 = OpenSubKey(form, r, string.Format(CultureInfo.InvariantCulture, "OpenWithList\\{0}", PhotoViewerDll), de, ref openWith))
                {
                }
                using (RegistryKey r2 = OpenSubKey(form, r, "ShellEx\\ContextMenuHandlers\\ShellImagePreview", de, ref imagePreview))
                {
                    CheckValue(form, r2, null, new string[] { PhotoGalleryGuid }, de);
                }
            }

            if (!openWith)
            {
                form.Add(this, Resources.MissingRegistryKey, de, new DataEntry(Resources.Key, new string[] 
                { 
                    string.Format(CultureInfo.InvariantCulture, "{2}\\{0}\\OpenWithList\\{1}", ext, PhotoViewerDll, Registry.ClassesRoot),
                    string.Format(CultureInfo.InvariantCulture, "{2}\\SystemFileAssociations\\{0}\\OpenWithList\\{1}", ext, PhotoViewerDll, Registry.ClassesRoot)
                }));
            }
            if (!imagePreview)
            {
                form.Add(this, Resources.MissingRegistryKey, de, new DataEntry(Resources.Key, new string[] 
                { 
                    string.Format(CultureInfo.InvariantCulture, "{2}\\{0}\\ShellEx\\ContextMenuHandlers\\ShellImagePreview", ext, PhotoViewerDll, Registry.ClassesRoot),
                    string.Format(CultureInfo.InvariantCulture, "{2}\\SystemFileAssociations\\{0}\\ShellEx\\ContextMenuHandlers\\ShellImagePreview", ext, PhotoViewerDll, Registry.ClassesRoot)
                }));
            }
        }
    }
}
