using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Plugins.Basic {
    public class Presets:IPlugin,IQuerystringPlugin, IIssueProvider {

        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
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
            if (OnlyAllowPresets) {
                string preset = e.QueryString["preset"];
                e.QueryString.Clear();
                e.QueryString["preset"] = preset.ToLowerInvariant();
            }
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

        private bool _onlyAllowPresets = false;
        /// <summary>
        /// If true, the plugin will block all commands except those specified in a preset, and the &amp;preset command itself
        /// </summary>
        public bool OnlyAllowPresets
        {
          get { return _onlyAllowPresets; }
          set { _onlyAllowPresets = value; }
        }

        protected void ParseXml(Node n, Config conf) {
            if (n == null ) return;
            OnlyAllowPresets = Util.Utils.getBool(n.Attrs, "onlyAllowPresets", OnlyAllowPresets);
            if (n.Children == null) return;
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

        public IEnumerable<IIssue> GetIssues() {
            if (OnlyAllowPresets && c.Plugins.Has<FolderResizeSyntax>()) {
                return new IIssue[]{new Issue("Presets","The FolderResizeSyntax allows clients to circumvent the 'onlyAllowPresets' setting by pulling values from the path into the querystring.",
                    "You should remove the FolderResizeSyntax to ensure 'onlyAllowPresets' can be enforced.", IssueSeverity.Critical)};
            } else if (OnlyAllowPresets) {
                return new IIssue[]{new Issue("Presets","Standard resizing commands are currently disabled; only presets are enabled.",
                    "To fix, set <presets onlyAllowPresets=\"false\"> (it is currently set to true).", IssueSeverity.Warning)};
            }
            return new IIssue[] { };
        }
    }
}
