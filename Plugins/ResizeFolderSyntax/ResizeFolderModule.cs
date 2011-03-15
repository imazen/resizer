using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace fbs.ImageResizer.Plugins.ResizeFolderSyntax {
    public class ResizeFolderModule {

        public static void Install() {
            fbs.ImageResizer.ResizingModule.RewriteDefaults += new UrlRewritingHook(InterceptModule_RewriteDefaults);
        }

        static void InterceptModule_RewriteDefaults(ResizingModule sender, UrlEventArgs e) {
            e.VirtualPath = parseResizeFolderSyntax(e.VirtualPath, e.QueryString);
        }

        /// <summary>
        /// Matches /resize(x,y,f)/ syntax
        /// Fixed Bug - will replace both slashes.. make first a lookbehind
        /// </summary>
        private static Regex resizeFolder = new Regex(@"(?<=^|\/)resize\(\s*(?<maxwidth>\d+)\s*,\s*(?<maxheight>\d+)\s*(?:,\s*(?<format>jpg|png|gif)\s*)?\)\/", RegexOptions.Compiled
           | RegexOptions.IgnoreCase);


        /// <summary>
        /// Parses and removes the resize folder syntax "resize(x,y,f)/" from the specified file path. 
        /// Places settings into the referenced querystring
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query">The collection to place parsed values into</param>
        /// <returns></returns>
        private static string parseResizeFolderSyntax(string path, NameValueCollection q) {
            Match m = resizeFolder.Match(path);
            if (m.Success) {
                //Parse capture groups
                int maxwidth = -1; int.TryParse(m.Groups["maxwidth"].Value, out maxwidth);
                int maxheight = -1; int.TryParse(m.Groups["maxheight"].Value, out maxheight);
                string format = (m.Groups["format"].Captures.Count > 0) ? format = m.Groups["format"].Captures[0].Value : null;

                //Remove first resize folder from URL
                path = resizeFolder.Replace(path, "", 1);

                //Add values to querystring
                if (maxwidth > 0) q["maxwidth"] = maxwidth.ToString();
                if (maxheight > 0) q["maxheight"] = maxheight.ToString();
                if (format != null) q["format"] = format;

                //Call recursive - this handles multiple /resize(w,h)/resize(w,h)/ occurrences
                return parseResizeFolderSyntax(path, q);
            }

            return path;
        }
    }
}
