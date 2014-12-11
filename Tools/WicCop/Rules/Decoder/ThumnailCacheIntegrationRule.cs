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
    class ThumnailCacheIntegrationRule : ShellIntegrationRuleBase
    {
        const string ThumnailCacheGuid = "{C7657C4A-9F68-40FA-A4DF-96BC08EB3551}";
        const string ShellExGuid = "{E357FCCD-A995-4576-B01F-234630154E96}";

        public ThumnailCacheIntegrationRule()
            : base("Thumnail Cache Integration")
        {
        }

        protected override void Check(ListView.ListViewItemCollection collection, string ext, RegistryKey rk, IWICBitmapDecoderInfo info)
        {
            DataEntry[] de = new DataEntry[] { new DataEntry("File Extension", ext)};
            using (RegistryKey r = OpenSubKey(collection, Registry.ClassesRoot, string.Format(CultureInfo.InvariantCulture, "{0}\\ShellEx\\{1}", ext, ShellExGuid), de))
            {
                CheckValue(collection, r, null, new string[] { ThumnailCacheGuid }, de);
            }
            using (RegistryKey r = OpenSubKey(collection, Registry.ClassesRoot, string.Format(CultureInfo.InvariantCulture, "SystemFileAssociations\\{0}\\ShellEx\\{1}", ext, ShellExGuid), de))
            {
                CheckValue(collection, r, null, new string[] { ThumnailCacheGuid }, de);
            }
        }
    }
}
