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
    class ShellIntegrationRule : ShellIntegrationRuleBase
    {
        const string ContentType = "Content Type";
        const string PerceivedType = "PerceivedType";

        public ShellIntegrationRule()
            : base ("Windows Explorer Integration")
        {
        }


        protected override void Check(ListView.ListViewItemCollection collection, string ext, RegistryKey rk, IWICBitmapDecoderInfo info)
        {
            CheckValue(collection, rk, ContentType, Extensions.GetString(info.GetMimeTypes).Split(','), new DataEntry[] { new DataEntry("File Extension", ext) });
            CheckValue(collection, rk, PerceivedType, new string[] { "image" }, new DataEntry[] { new DataEntry("File Extension", ext) });
        }
    }
}
