using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Plugins.Basic {
    public class Presets:IPlugin,IQuerystringPlugin {

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            ParseXml(c.getConfigXml().queryFirst("presets"),c);
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite += Pipeline_Rewrite;
            return this;
        }

        Dictionary<string, ResizeSettings> defaults = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ResizeSettings> settings = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);


        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            ApplyPreset(e, defaults);
        }
        void Pipeline_Rewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            ApplyPreset(e, settings);
        }

        void ApplyPreset(IUrlEventArgs e, Dictionary<string, ResizeSettings> dict) {
            if (!string.IsNullOrEmpty(e.QueryString["preset"])) {
                string[] presets = e.QueryString["preset"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in presets) {
                    if (!dict.ContainsKey(p)) continue;
                    ResizeSettings query = dict[p];
                    foreach (string key in query.Keys) {
                        e.QueryString[key] =query[key];
                    }
                }
            }
        }



        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite -= Pipeline_Rewrite;
            return true;
        }


        protected void ParseXml(Node n, Config conf) {
            if (n == null || n.Children == null) return;
            foreach (Node c in n.Children) {
                string name = c.Attrs["name"];
                if (c.Name.Equals("preset", StringComparison.OrdinalIgnoreCase)) {

                    //Verify the name is specified and is unique.
                    if (string.IsNullOrEmpty(name) || defaults.ContainsKey(name) || settings.ContainsKey(name)) {
                        conf.configurationSectionIssues.AcceptIssue(new Issue("Presets", "The name attribute for each preset must be specified, and must be unique.",
                        "XML: " + c.ToString(), IssueSeverity.ConfigurationError));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(c.Attrs["defaults"])) defaults[name] = new ResizeSettings(c.Attrs["defaults"]);
                    if (!string.IsNullOrEmpty(c.Attrs["settings"])) settings[name] = new ResizeSettings(c.Attrs["settings"]);
                }
            }
            return;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "preset" };
        }
    }
}
