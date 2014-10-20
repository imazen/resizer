using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Test.Wex.Dgt.InteropServices.ComTypes;
using Microsoft.Win32;
using System.Globalization;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    class PhotoGalleryntegrationRule : ShellIntegrationRuleBase
    {
        const string PhotoGalleryGuid = "{FFE2A43C-56B9-4BF5-9A79-CC6D4285608A}";
        const string PhotoViewerDll = "PhotoViewer.dll";

        public PhotoGalleryntegrationRule()
            : base ("Windows Photo Gallery Integration")
        {
        }


        protected override void Check(ListView.ListViewItemCollection collection, string ext, RegistryKey rk, IWICBitmapDecoderInfo info)
        {
            DataEntry[] de = new DataEntry[] { new DataEntry("File Extension", ext)};
            string progid = CheckStringValue(collection, rk, null, de);
            if (!string.IsNullOrEmpty(progid))
            {
                using (RegistryKey r = OpenSubKey(collection, rk, "OpenWithProgids", de))
                {
                    if (r != null)
                    {
                        if (Array.IndexOf(r.GetValueNames(), progid) < 0)
                        {
                            collection.Add(this, "Registry value is missing.", de, new DataEntry("Expected Values", progid), new DataEntry("Key", rk.ToString()));
                        }
                    }
                }
                using (RegistryKey r = OpenSubKey(collection, rk, string.Format(CultureInfo.InvariantCulture, "OpenWithList\\{0}", PhotoViewerDll), de))
                {
                }
                using (RegistryKey r = OpenSubKey(collection, rk, "ShelExt\\ContextMenuHandlers\\ShellImagePreview", de))
                {
                    CheckValue(collection, r, null, new string[] { PhotoGalleryGuid }, de);
                }

                using (RegistryKey r = OpenSubKey(collection, Registry.ClassesRoot, progid, new DataEntry[0]))
                {
                    CheckStringValue(collection, r, null, de);

                    using (RegistryKey r1 = OpenSubKey(collection, r, "DefaultIcon", new DataEntry[0]))
                    {
                        CheckStringValue(collection, r1, null, de);
                        // TODO get and check icon
                    }

                    using (RegistryKey r1 = OpenSubKey(collection, r, "shell\\open\\command", new DataEntry[0]))
                    {
                        CheckValue(collection, r1, null, new string []{"%SystemRoot%\\System32\\rundll32.exe \"%ProgramFiles%\\Windows Photo Gallery\\PhotoViewer.dll\", ImageView_Fullscreen %1"}, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(collection, r, "shell\\open", new DataEntry[0]))
                    {
                        CheckValue(collection, r1, "MuiVerb", new string[] { "@%ProgramFiles%\\Windows Photo Gallery\\PhotoViewer.dll,-3043" }, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(collection, r, "shell\\open\\DropTarget", new DataEntry[0]))
                    {
                        CheckValue(collection, r1, "Clsid", new string[] { PhotoGalleryGuid }, de);
                    }
                    using (RegistryKey r1 = OpenSubKey(collection, r, "shell\\printto\\command", new DataEntry[0]))
                    {
                        CheckValue(collection, r1, null, new string[] { "%SystemRoot%\\System32\\rundll32.exe \"%ProgramFiles%\\Windows Photo Gallery\\PhotoViewer.dll\", ImageView_PrintTo /pt \"%1\" \"%2\" \"%3\" \"%4\"" }, de);
                    }
                }
            }

            using (RegistryKey r = OpenSubKey(collection, Registry.ClassesRoot, string.Format(CultureInfo.InvariantCulture, "SystemFileAssociations\\{0}", ext), new DataEntry[0]))
            {
                using (RegistryKey r2 = OpenSubKey(collection, r, "ShellEx\\ContextMenuHandlers\\ShellImagePreview", de))
                {
                    CheckValue(collection, r2, null, new string[] { PhotoGalleryGuid }, de);
                }
            }
        }
    }
}
