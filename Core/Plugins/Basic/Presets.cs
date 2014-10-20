using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Basic {
    public class Presets:IPlugin,IQuerystringPlugin, IIssueProvider, ISettingsModifier {

        public Presets() {
        }

        public Presets(Dictionary<string, ResizeSettings> defaults, Dictionary<string, ResizeSettings> settings, bool onlyAllowPresets) {
            this.OnlyAllowPresets = onlyAllowPresets;
            if (defaults.Comparer == StringComparer.OrdinalIgnoreCase) this.defaults = defaults;
            else this.defaults = new Dictionary<string, ResizeSettings>(defaults, StringComparer.OrdinalIgnoreCase);

            if (settings.Comparer == StringComparer.OrdinalIgnoreCase) this.settings = settings;
            else this.settings = new Dictionary<string, ResizeSettings>(settings, StringComparer.OrdinalIgnoreCase);
        }
        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            ParseXml(c.getConfigXml().queryFirst("presets"),c);
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite += Pipeline_Rewrite;
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;
            return this;
        }

        Dictionary<string, ResizeSettings> defaults = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ResizeSettings> settings = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);


        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            ApplyPreset(e, defaults);
        }
        void Pipeline_Rewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            if (OnlyAllowPresets) {
                e.QueryString = FilterQuerystring(e.QueryString, false);
            }
            ApplyPreset(e, settings);
            
        }
        void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            if (e.QueryString["preset"] != null) e.QueryString.Remove("preset"); //Remove presets so they aren't processed again during ModifySettings
        }

        /// <summary>
        /// Returns an new NameValueCollection instance that only includes the "preset" and ("hmac" and "urlb64", if specified) querystring pairs from the specified instance. 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="keepHmacAndUrl64"></param>
        /// <returns></returns>
        public static NameValueCollection FilterQuerystring(NameValueCollection s, bool keepHmacAndUrl64) {
            NameValueCollection q = new NameValueCollection();
            string preset = s["preset"];
            string hmac = s["hmac"];
            string urlb64 = s["urlb64"];
            if (preset != null) q["preset"] = preset.ToLowerInvariant();
            if (keepHmacAndUrl64 && hmac != null) q["hmac"] = hmac;
            if (keepHmacAndUrl64&& urlb64 != null) q["urlb64"] = urlb64;
            return q;
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

        public ResizeSettings Modify(ResizeSettings s) {
            if (!string.IsNullOrEmpty(s["preset"])) {
                string[] presets = s["preset"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in presets) {
                    //Apply defaults
                    if (defaults.ContainsKey(p)) {
                        ResizeSettings dq = defaults[p];
                        foreach (string key in dq.Keys) {
                            if (s[key] == null) s[key] = dq[key]; //Overwrite null and missing values on defaults.
                        }
                    }
                    //Apply overrides
                    if (settings.ContainsKey(p)) {
                        ResizeSettings sq = settings[p];
                        foreach (string key in sq.Keys) {
                            s[key] = sq[key]; //Overwrite null and missing values on defaults.
                        }
                    }
                }
                s.Remove("preset");
            }
            return s;
        }




        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite -= Pipeline_Rewrite;
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        private bool _onlyAllowPresets = false;
        /// <summary>
        /// If true, the plugin will block all commands except those specified in a preset, and the &amp;preset command itself. 
        /// Only applies to InterceptModule (the URL API). Does not apply to ImageBuilder.Build calls. To replicate the behavior, simply prevent any querystring keys except 'preset' from being passed to ImageBuilder.Build.
        /// </summary>
        public bool OnlyAllowPresets
        {
          get { return _onlyAllowPresets; }
          set { _onlyAllowPresets = value; }
        }

        protected void ParseXml(Node n, Config conf) {
            if (n == null ) return;
            OnlyAllowPresets = n.Attrs.Get("onlyAllowPresets", OnlyAllowPresets);
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
