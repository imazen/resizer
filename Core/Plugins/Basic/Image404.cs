using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Redirects image 404 errors to a querystring-specified server-local location,
    /// while maintaining querystring values (by default) so layout isn't disrupted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The image to use in place of missing images can be specified by the "404"
    /// parameter in the querystring.  The "404" value can also refer to a named
    /// value in the &lt;plugins&gt;/&lt;Image404&gt; setting in Web.config.
    /// </para>
    /// <para>
    /// Querystring commands to remove from the 404 request can be specified in
    /// the &lt;plugins&gt;/&lt;Image404&gt; setting in Web.config using the
    /// "removeCommands" attribute with a comma-separated list.  You can also
    /// use a "404.remove" querystring value (also a comma-separated list) to
    /// indicate commands to remove for an individual image request.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Using <c>&lt;img src="missingimage.jpg?404=image.jpg&amp;width=200" /&gt;</c>
    /// with the default setting (<c>&lt;image404 baseDir="~/" /&gt;</c>) will
    /// redirect to <c>~/image.jpg?width=200</c>.
    /// </para>
    /// <para>
    /// You may also configure 'variables', which is the recommended approach.
    /// For example, <c>&lt;image404 propertyImageDefault="~/images/nophoto.png" /&gt;</c>
    /// in the config file, and <c>&lt;img src="missingimage.jpg?404=propertyImageDefault&amp;width=200" /&gt;</c>
    /// will result in a redirect to <c>~/images/nophoto.png?width=200</c>.
    /// Any querystring values in the config variable take precedence over
    /// querystring values in the image querystring.  For example,
    /// <c>&lt;image404 propertyImageDefault="~/images/nophoto.png?format=png" /&gt;</c>
    /// in the config file and
    /// <c>&lt;img src="missingimage.jpg?format=jpg&amp;404=propertImageDefault&amp;width=200" /&gt;</c>
    /// will result in a redirect to <c>~/images/nophoto.png?format=png&amp;width=200</c>.
    /// </para>
    /// <para>
    /// <c>&lt;img src="notfound.jpg?rotate=45&amp;404=missing.jpg&amp;404.remove=rotate" /&gt;</c>
    /// will redirect to <c>~/missing.jpg</c>, <em>without</em> the <c>rotate</c>
    /// command.
    /// </para>
    /// <para>
    /// Similarly, <c>&lt;image404 removeCommands="rotate,flip" /&gt;</c> in the
    /// config file will ensure that <c>rotate</c> and <c>flip</c> commands are
    /// always removed from the redirect; a reference to
    /// <c>&lt;img src="notfound.jpg?flip=x&amp;width=50&amp;rotate=45&amp;404=missing.jpg" /&gt;</c>
    /// would redirect to <c>~/missing.jpg?width=50</c>.
    /// </para>
    /// </example>
    public class Image404:IQuerystringPlugin,IPlugin {

        Config c;
        private string[] removeCommands;

        public Image404(NameValueCollection args) {
            var commandList = args["removeCommands"];
            if (!string.IsNullOrEmpty(commandList)) {
                    this.removeCommands = commandList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

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

                //Merge commands from the 404 querystring with ones from the original image. 
                ResizeSettings imageQuery = new ResizeSettings(e.QueryString);
                imageQuery.Normalize();

                // remove commands listed for removal via the image querystring
                var commandList = e.QueryString["404.remove"];
                if (!string.IsNullOrEmpty(commandList)) {
                    foreach (var command in commandList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                        imageQuery.Remove(command);
                    }
                }

                // remove commands listed for removal via the Image404 configuration
                if (this.removeCommands != null) {
                    foreach (var command in this.removeCommands)
                    {
                        imageQuery.Remove(command);
                    } 
                }

                // Always remove the '404' and '404.remove' settings.
                imageQuery.Remove("404");
                imageQuery.Remove("404.remove");

                ResizeSettings i404Query = new ResizeSettings(Util.PathUtils.ParseQueryString(path));
                i404Query.Normalize();
                //Overwrite new with old
                foreach (string key in i404Query.Keys)
                    if (key != null) imageQuery[key] = i404Query[key];

                path = PathUtils.AddQueryString(PathUtils.RemoveQueryString(path), PathUtils.BuildQueryString(imageQuery));
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
