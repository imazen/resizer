using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageResizer.Configuration.Xml;

namespace ImageResizer.Plugins.Basic
{
    public class WebConfigLicenseReader : ILicenseProvider, IPlugin, IRedactDiagnostics
    {
        public ICollection<string> GetLicenses()
        {
            return licenses;
        }

        private IList<string> licenses = new List<string>(1);

        public IPlugin Install(Configuration.Config c)
        {

            foreach (var child in c.getNode("licenses")?
                .childrenByName("license")
                .Where(n => !string.IsNullOrWhiteSpace(n.TextContents)) ?? Enumerable.Empty<Node>())
            {
                licenses.Add(StaticLicenseProvider.CleanupLicenseString(child.TextContents));

            }

            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public Node RedactFrom(Node resizer)
        {
            foreach (Node n in resizer.queryUncached("licenses.license")?.Where(n => !string.IsNullOrWhiteSpace(n.TextContents)) ?? Enumerable.Empty<Node>())
            {
                n.TextContents = TryRedact(n.TextContents);
            }
            return resizer;
        }
        public static string TryRedact(string license)
        {
            var segments = license.Split(':');
            return segments.Count() > 1 ? string.Join(":",
                    segments.Take(segments.Count() - 2).Concat(new[] { "****redacted****", segments.Last() })) : license;
        }
    }
}
