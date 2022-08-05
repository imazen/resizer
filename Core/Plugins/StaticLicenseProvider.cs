using System.Collections.Generic;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    public class StaticLicenseProvider : ILicenseProvider, IPlugin
    {
        public StaticLicenseProvider()
        {
        }

        public StaticLicenseProvider(params string[] licenses)
        {
            foreach (var s in licenses)
                AddLicense(s);
        }

        public static StaticLicenseProvider One(string license)
        {
            var p = new StaticLicenseProvider();
            p.AddLicense(license);
            return p;
        }

        private List<string> licenses = new List<string>(1);


        public void AddLicense(string license)
        {
            licenses.Add(CleanupLicenseString(license));
        }

        /// <summary>
        ///     Cleans up a license string that may have been split across multiple lines and indented
        /// </summary>
        /// <param name="license"></param>
        /// <returns></returns>
        public static string CleanupLicenseString(string license)
        {
            //TODO: Don't remove spaces from user comment. 
            return license.Trim().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
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