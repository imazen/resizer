using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;
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
    /// </example>
    public class Image404:IQuerystringPlugin,IPlugin {

        public enum FilterMode
        {
            IncludeUnknownCommands,
            ExcludeUnknownCommands,
            IncludeAllCommands,
            ExcludeAllCommands,
        }

        readonly static MatcherCollection DefaultWhitelist = new MatcherCollection(
            // deterministic sizing and color -- copied by default
            new Matcher("maxwidth"), 
            new Matcher("maxheight"),
            new Matcher("width"),
            new Matcher("height"),
            new Matcher("w"),
            new Matcher("h"),
            
            new Matcher(delegate (string name, string value) { // crop=auto
                return string.Equals(name, "crop", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase); }),

            new Matcher("mode"),
            new Matcher("anchor"),
            new Matcher("scale"),
            new Matcher("zoom"),
            new Matcher("bgcolor"),
            new Matcher("paddingWidth"),
            new Matcher("paddingColor"),
            new Matcher("borderWidth"),
            new Matcher("borderColor"),
            new Matcher("shadowWidth"),
            new Matcher("shadowOffset"),
            new Matcher("shadowColor"),
            new Matcher("margin"),
            new Matcher("dpi"),

            // output size, format -- copied by default
            new Matcher("format"),
            new Matcher("quality"),
            new Matcher("colors"),
            new Matcher("subsampling"),
            new Matcher("dither"),
            new Matcher("speed"),
            new Matcher("ignoreicc"),

            // visual filters (simple and advanced), post-processing -- copied by default
            new Matcher(delegate(string name) { return name.StartsWith("s.", StringComparison.OrdinalIgnoreCase);}),
            new Matcher(delegate(string name) { return name.StartsWith("a.", StringComparison.OrdinalIgnoreCase);}),
            new Matcher("flip"),
            new Matcher("rotate"));

        readonly static MatcherCollection DefaultBlacklist = new MatcherCollection(
            // not useful -- excluded by default
            new Matcher("watermark"),
            new Matcher("cache"),
            new Matcher("process"),
            new Matcher("builder"),
            new Matcher("decoder"),
            new Matcher("encoder"),

            // unlikely to need -- excluded by default
            new Matcher("sflip"),
            new Matcher("srotate"),
            new Matcher("autorotate"),

            new Matcher(delegate (string name, string value) { // crop=anything-other-than-auto
                return string.Equals(name, "crop", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase); }),

            new Matcher("cropxunits"),
            new Matcher("cropyyunits"),
            new Matcher("trim.threshold"),
            new Matcher("trim.percentpadding"),

            // never want to copy -- excluded by default
            new Matcher("hmac"),
            new Matcher("urlb64"),
            new Matcher("frame"),
            new Matcher("page"),
            new Matcher("color1"),
            new Matcher("color2"));

        Config c;
        private FilterMode filterMode = FilterMode.ExcludeUnknownCommands;
        private MatcherCollection except = MatcherCollection.Empty;

        // To support configurable include/exclude modes and exceptions, uncomment
        // this constructor.
        ////public Image404(NameValueCollection args) {
        ////    this.filterMode = NameValueCollectionExtensions.Get(args, "mode", FilterMode.ExcludeUnknownCommands);
        ////    this.except = MatcherCollection.Parse(args["except"]);
        ////    }

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

                // Merge commands from the 404 querystring with ones from the
                // original image.  We start by sanitizing the querystring from
                // the image.
                ResizeSettings imageQuery = new ResizeSettings(e.QueryString);
                imageQuery.Normalize();

                // Use the configured settings by default.
                var filterMode = this.filterMode;
                var except = this.except;

                // To support querystring-specifiable filter mode and exceptions,
                // uncomment the if block below.
                ////// If the imageQuery includes specific 404 command-filtering,
                ////// (*either* of the values), use those instead of the
                ////// configured defaults.
                ////if (!string.IsNullOrEmpty(e.QueryString["404.filterMode"]) ||
                ////    !string.IsNullOrEmpty(e.QueryString["404.except"]))
                ////{
                ////    filterMode = NameValueCollectionExtensions.Get(e.QueryString, "404.filterMode", FilterMode.ExcludeUnknownCommands);
                ////    except = MatcherCollection.Parse(e.QueryString["404.except"]);
                ////}

                // remove all of the commands we're supposed to remove... we
                // clone the list of keys so that we're not modifying the collection
                // while we enumerate it.
                var shouldRemove = CreateRemovalMatcher(filterMode, except);
                var names = new List<string>(imageQuery.AllKeys);

                foreach (var name in names)
                {
                    if (shouldRemove(name, imageQuery[name]))
                    {
                        imageQuery.Remove(name);
                    }
                }

                // Always remove the '404', '404.filterMode', and '404.except' settings.
                imageQuery.Remove("404");
                imageQuery.Remove("404.filterMode");
                imageQuery.Remove("404.except");

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

        private static Matcher.NameAndValue CreateRemovalMatcher(FilterMode filterMode, MatcherCollection except)
        {
            Matcher.NameAndValue shouldRemove = null;

            switch (filterMode)
            {
                case FilterMode.IncludeUnknownCommands:
                    // To include unknown commands, we remove blacklisted
                    // and 'except' commands.
                    shouldRemove = delegate(string name, string value)
                    {
                        return DefaultBlacklist.IsMatch(name, value) ||
                                    except.IsMatch(name, value);
                    };
                    break;

                case FilterMode.ExcludeUnknownCommands:
                    // To exclude unknown commands, we *keep* whitelisted
                    // and 'except' commands.
                    shouldRemove = delegate(string name, string value)
                    {
                        return !(DefaultWhitelist.IsMatch(name, value) ||
                                    except.IsMatch(name, value));
                    };
                    break;

                case FilterMode.IncludeAllCommands:
                    // To include all commands, we remove any of the 'except'
                    // commands.
                    shouldRemove = delegate(string name, string value)
                    {
                        return except.IsMatch(name, value);
                    };
                    break;

                case FilterMode.ExcludeAllCommands:
                    // To exclude all commands, we only keep the 'except'
                    // commands.
                    shouldRemove = delegate(string name, string value)
                    {
                        return !except.IsMatch(name, value);
                    };
                    break;
            }

            return shouldRemove;
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

        private class MatcherCollection
        {
            private Matcher[] matchers;

            public static MatcherCollection Empty = new MatcherCollection();

            public static MatcherCollection Parse(string commandList)
            {
                if (string.IsNullOrEmpty(commandList))
                {
                    return MatcherCollection.Empty;
                }

                var commands = commandList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var matchers = new Matcher[commands.Length];

                for (var i = 0; i < commands.Length; i++)
                {
                    matchers[i] = new Matcher(commands[i]);
                }

                return new MatcherCollection(matchers);
            }

            public MatcherCollection(params Matcher[] matchers)
            {
                this.matchers = matchers;
            }

            public bool IsMatch(string name, string value)
            {
                foreach (var matcher in this.matchers)
                {
                    if (matcher.IsMatch(name, value))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private class Matcher
        {
            public delegate bool NameOnly(string name);
            public delegate bool NameAndValue(string name, string value);

            private string name;
            private NameOnly nameTest;
            private NameAndValue nameAndValueTest;

            public Matcher(string name)
            {
                this.name = name;
            }

            public Matcher(NameOnly nameTest)
            {
                this.nameTest = nameTest;
            }

            public Matcher(NameAndValue nameAndValueTest)
            {
                this.nameAndValueTest = nameAndValueTest;
            }

            public bool IsMatch(string name, string value)
            {
                if (this.name != null)
                {
                    return string.Equals(this.name, name, StringComparison.OrdinalIgnoreCase);
                }

                if (this.nameTest != null)
                {
                    return this.nameTest(name);
                }

                if (this.nameAndValueTest != null)
                {
                    return this.nameAndValueTest(name, value);
                }

                return false;
            }
        }
    }
}
