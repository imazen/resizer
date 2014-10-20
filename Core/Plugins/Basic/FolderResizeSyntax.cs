using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Globalization;

namespace ImageResizer.Plugins.Basic {
    public class FolderResizeSyntax : IPlugin {

        public FolderResizeSyntax() {
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            return this;
        }

        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e) {
            //Handles /resize(width,height,format)/ and /resize(width,height)/ syntaxes.
            e.VirtualPath = parseResizeFolderSyntax(e.VirtualPath, e.QueryString);

        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            return true;
        }


        /// <summary>
        /// Matches /resize(x,y,f)/ syntax
        /// Fixed Bug - will replace both slashes.. make first a lookbehind
        /// </summary>
        protected Regex resizeFolder = new Regex(@"(?<=^|\/)resize\(\s*(?<maxwidth>\d+)\s*,\s*(?<maxheight>\d+)\s*(?:,\s*(?<format>jpg|png|gif)\s*)?\)\/", RegexOptions.Compiled
           | RegexOptions.IgnoreCase);


        /// <summary>
        /// Parses and removes the resize folder syntax "resize(x,y,f)/" from the specified file path. 
        /// Places settings into the referenced querystring
        /// </summary>
        /// <param name="path"></param>
        /// <param name="q">The collection to place parsed values into</param>
        /// <returns></returns>
        protected string parseResizeFolderSyntax(string path, NameValueCollection q) {
            Match m = resizeFolder.Match(path);
            if (m.Success) {
                //Parse capture groups
                int maxwidth = -1; if (!int.TryParse(m.Groups["maxwidth"].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out maxwidth)) maxwidth = -1;
                int maxheight = -1; if (!int.TryParse(m.Groups["maxheight"].Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out maxheight)) maxheight = -1;
                string format = (m.Groups["format"].Captures.Count > 0) ? format = m.Groups["format"].Captures[0].Value : null;

                //Remove first resize folder from URL
                path = resizeFolder.Replace(path, "", 1);

                //Add values to querystring
                if (maxwidth > 0) q["maxwidth"] = maxwidth.ToString(NumberFormatInfo.InvariantInfo);
                if (maxheight > 0) q["maxheight"] = maxheight.ToString(NumberFormatInfo.InvariantInfo);
                if (format != null) q["format"] = format;

                //Call recursive - this handles multiple /resize(w,h)/resize(w,h)/ occurrences
                return parseResizeFolderSyntax(path, q);
            }

            return path;
        }

    }
}
