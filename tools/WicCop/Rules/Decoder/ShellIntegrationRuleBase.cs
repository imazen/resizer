using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Test.Wex.Dgt.InteropServices.ComTypes;
using Microsoft.Win32;
using System.Globalization;
using System.IO;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    abstract class ShellIntegrationRuleBase : RuleBase<ComponentRuleGroup>
    {
        public ShellIntegrationRuleBase(string text)
            : base (text)
        {
        }

        protected abstract void Check(ListView.ListViewItemCollection collection, string ext, RegistryKey rk, IWICBitmapDecoderInfo info);

        protected RegistryKey OpenSubKey(ListView.ListViewItemCollection collection, RegistryKey rk, string name, DataEntry[] de)
        {
            if (rk == null)
            {
                return null;
            }
            RegistryKey res = rk.OpenSubKey(name);
            if (res == null)
            {
                collection.Add(this, "Registry key is missing.", de, new DataEntry("Key", string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", rk, name)));
            }

            return res;
        }

        protected string CheckStringValue(ListView.ListViewItemCollection collection, RegistryKey rk, string value, DataEntry[] de)
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
                    collection.Add(this, "Incorrect type of the registry value.", de, new DataEntry("Value Name", value ?? "(default)"), new DataEntry("Key", rk.ToString()), new DataEntry("Expected Kind", new RegistryValueKind[] { RegistryValueKind.ExpandString, RegistryValueKind.String }), new DataEntry("Actual Kind", k));

                    return null;
                }
                else
                {
                    string res = rk.GetValue(value, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

                    if (string.IsNullOrEmpty(res))
                    {
                        collection.Add(this, "Empty value is specified.", de, new DataEntry("Value", value ?? "(default)"), new DataEntry("Key", rk.ToString()));
                    }

                    return res;
                }
            }
            catch (IOException)
            {
                collection.Add(this, "The registry value is missing.", de, new DataEntry("Value Name", value ?? "(default)"), new DataEntry("Key", rk.ToString()));

                return null;
            }
        }

        protected void CheckValue(ListView.ListViewItemCollection collection, RegistryKey rk, string value, string[] valid, DataEntry[] de)
        {
            string v = CheckStringValue(collection, rk, value, de);
            if (!string.IsNullOrEmpty(v))
            {
                if (Array.FindIndex(valid, delegate(string obj) { return string.Compare(v, obj, StringComparison.OrdinalIgnoreCase) == 0; }) < 0)
                {
                    collection.Add(this, "Unexpected value is specified.", de, new DataEntry("Actual Value", v), new DataEntry("Expected Values", valid), new DataEntry("Key", rk.ToString()));
                }
            }
        }

        protected override void RunOverride(ListView.ListViewItemCollection collection, object tag)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmapDecoderInfo info = null;
            try
            {
                info = (IWICBitmapDecoderInfo)factory.CreateComponentInfo(Parent.Clsid);
                string[] mimes = Extensions.GetString(info.GetMimeTypes).Split(',');

                foreach (string ext in Extensions.GetString(info.GetFileExtensions).Split(','))
                {
                    using (RegistryKey rk = OpenSubKey(collection, Registry.ClassesRoot, ext, new DataEntry[] { new DataEntry("File Extension", ext) }))
                    {
                        if (rk != null)
                        {
                            Check(collection, ext, rk, info);
                        }
                    }
                }
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
            }
        }
    }
}
