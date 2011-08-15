using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Text.RegularExpressions;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Redirects image 404 errors to a querystring-specified server-local location, while maintaining querystring values so layout isn't disrupted.
	/// For example, missingimage.jpg?404=image.jpg&amp;width=200
    /// with the default setting &lt;image404 baseDir="~/" /&gt; will redirect to ~/image.jpg?width=200.
    /// You may also configure 'variables', which is the reccomended approach.
    /// Ex. &lt;image404 propertyImageDefault="~/images/nophoto.png" /&gt; and use them like so: missingimage.jpg?404=propertImageDefault?width=200 -> ~/images/nophoto.png?width=200.
    /// Querystring values in the variable value take precedence. For example, 
	/// Ex. &lt;image404 propertyImageDefault="~/images/nophoto.png?format=png" /&gt; and missingimage.jpg?format=jpg&amp;404=propertImageDefault?width=200 -> ~/images/nophoto.png?format=png&amp;width=200.
    /// </summary>
    public class Image404:IQuerystringPlugin,IPlugin {

        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            if (c.Plugins.Has<Image404>()) throw new InvalidOperationException();

            c.Pipeline.ImageMissing += new Configuration.UrlEventHandler(Pipeline_ImageMissing);
            c.Plugins.add_plugin(this);
            return this;
        }

        void Pipeline_ImageMissing(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e) {
            if (!string.IsNullOrEmpty(e.QueryString["404"])) {
                //Resolve the path to virtual or app-relative for
                string path = resolve404Path(e.QueryString["404"]);
                //Resolve to virtual path
                path = Util.PathUtils.ResolveAppRelative(path);
                //Merge/overwrite with the current request querystring (current request settings take precedence)
                e.QueryString.Remove("404"); //Remove the 404 ref
                path = Util.PathUtils.MergeQueryString(path, e.QueryString);
                //Redirect
                context.Response.Redirect(path, true);
            }
        }

        protected string resolve404Path(string path) {
            //1 If it starts with 'http(s)://' throw an exception.
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) throw new ImageProcessingException("Image 404 redirects must be server-local. Received " + path);

            //2 If it starts with a slash, use as-is
            if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase)) return path;
            //3 If it starts with a tilde, use as-is.
            if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase)) return path;
            //3 If it doesn't have a slash or a period, see if it is a attribute of <image404>.
            if (new Regex("^[a-zA-Z][a-zA-Z0-9]*$").IsMatch(path)) {
                string val = c.get("image404." + path,null);
                if (val != null) return val;
            }
            //4 Otherwise, join with image404.basedir or the application root
            string baseDir = c.get("image404.basedir","~/");
            path = baseDir.TrimEnd('/') + '/' + path.TrimStart('/');
            return path;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.ImageMissing -= Pipeline_ImageMissing;
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "404" };
        }
    }
}
