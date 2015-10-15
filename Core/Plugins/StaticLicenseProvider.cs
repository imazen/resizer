using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    public class StaticLicenseProvider : ILicenseProvider, IPlugin
    {
        public StaticLicenseProvider() { }
        public StaticLicenseProvider(params string[] licenses)
        {
            foreach(string s in licenses)
                AddLicense(s);
        }

        private List<String> licenses = new List<string>();
        

        public void AddLicense(string license)
        {
           licenses.Add(license.Trim().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", ""));
        }

        public ICollection<string> GetLicenses()
        {
            return licenses.AsReadOnly();
        }

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
