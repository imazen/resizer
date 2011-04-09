using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using ImageResizer.Plugins;
using ImageResizer.Caching;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Configuration {
    public class PluginConfig :IssueSink, IEncoderProvider {

        public override IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>(base.GetIssues());
            //Verify all plugins are registered as IPlugins also.
            //Verify there is something other than NoCache registered
            if (c.Plugins.CachingSystems.First is NoCache)
                issues.Add(new Issue("NoCache is only for development usage, and cannot scale to production use."));
            
            return issues;
        }
        protected Config c;
        /// <summary>
        /// Creates a new plugin config section, attached to the specified parent
        /// </summary>
        /// <param name="c"></param>
        public PluginConfig(Config c) : base("Plugins"){
            this.c = c;
            imageBuilderExtensions = new SafeList<BuilderExtension>();
            imageEncoders = new SafeList<IEncoder>();
            cachingSystems = new SafeList<ICache>();
            querystringPlugins = new SafeList<IQuerystringPlugin>();
            fileExtensionPlugins = new SafeList<IFileExtensionPlugin>();
            allPlugins = new SafeList<IPlugin>();
        }

        protected volatile bool _pluginsLoaded = false;
        protected object _loadPluginsSync = new object();
        /// <summary>
        /// Processes the &lt;plugins&gt; section if they are not already loaded. Thread safe.
        /// Only executes once.
        /// </summary>
        public void LoadPlugins() {
            lock (_loadPluginsSync) {
                if (_pluginsLoaded) return;
                loadPluginsInternal();
                _pluginsLoaded = true;
            }
        }
        /// <summary>
        /// Returns true if the &lt;plugins&gt; section has been processed
        /// </summary>
        public bool PluginsLoaded {
            get {
                lock (_loadPluginsSync) return _pluginsLoaded;
            }
        }

        /// <summary>
        /// Not thread safe. Performs actual work.
        /// </summary>
        protected void loadPluginsInternal() {
            Node plugins = c.getNode("plugins"); 
            if (plugins == null) return;
            foreach (Node n in plugins.Children) {
                if (n.Name.Equals("add", StringComparison.OrdinalIgnoreCase)) 
                    addPluginByName(n["name"]);
                else if (n.Name.Equals("remove", StringComparison.OrdinalIgnoreCase))
                    removePluginsByName(n["name"]);
                else if (n.Name.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    clearPluginsByType(n["type"]);
                else {
                    this.AcceptIssue(new Issue("Plugins", "Unexpected element <" + n.Name + "> in <plugins></plugins>.",
                                                "Element XML: " + n.ToXmlElement().OuterXml, IssueSeverity.Warning));
                }
            }
        }
        /// <summary>
        /// Returns the subset of AllPlugins which implement the specified type or interface
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IList<IPlugin> GetPluginsByType(Type type) {
            List<IPlugin> results = new List<IPlugin>();
            foreach (IPlugin p in AllPlugins)
                if (type.IsAssignableFrom(p.GetType())) //Like instance of. Can be a subclass
                    results.Add(p); 
            return results;
        }

        protected void removePluginsByName(string name) {
            Type t = GetPluginType(name);
            if (t == null) return; //Failed to acquire type
            foreach (IPlugin p in GetPluginsByType(t))
                if (!p.Uninstall(c)) AcceptIssue(new Issue("Plugin " + t.FullName + " reported a failed uninstall attempt triggered by a <remove name=\"" + name + "\" />.", IssueSeverity.Error));
        }
        protected void addPluginByName(string name) {
            IPlugin p = CreatePluginByName(name);
            p.Install(c);
        }
        protected void clearPluginsByType(string type) {
            Type t = null;
            if ("encoders".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(IEncoder);
            if ("caches".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(ICache);
            if ("imagebuilderextensions".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(BuilderExtension);
            if ("all".Equals(type, StringComparison.OrdinalIgnoreCase) || type == null) t = typeof(IPlugin);
            if (t == null) this.AcceptIssue(new Issue("Unrecognized type value \"" + type + "\" in <clear type=\"" + type + "\" />.", IssueSeverity.ConfigurationError));
            else {
                IList<IPlugin> results = GetPluginsByType(t);
                foreach (IPlugin p in results)
                    if (!p.Uninstall(c)) AcceptIssue(new Issue("Plugin " + p.GetType().FullName + " reported a failed uninstall attempt triggered by a <clear type=\"" + type + "\" />.", IssueSeverity.Error));
            }
        }

        protected Type GetPluginType(string name) {
            Type t = null;

            string convention = "ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.');
            string alternate = "ImageResizer.Plugins." + name.TrimStart('.');

            //If there is a dot or period, try the exact name first.
            bool looksQualified = (name.IndexOfAny(new char[] { '.', ',' }) > -1);
            if (looksQualified)
                t = Type.GetType(name, false, true);
            //Try the default convention, with the namespace and plugin class name identical
            if (t == null)
                t = Type.GetType(convention, false, true);
            //Try the exact name if we didn't already try it
            if (t == null && !looksQualified)
                t = Type.GetType(name, false, true);
            //Try some other stuff...
            if (t == null)
                t = Type.GetType(alternate, false, true);

            //Ok, time to fail.
            if (t == null) {
                this.AcceptIssue(new Issue( "Failed to load plugin by name \"" + name + "\"",
                    "Attempted using \"" + name + "\", \"" + convention + "\", and \"" + alternate + "\". \n" +
                    "Verify the plugin DLL is located in /bin, and that the name is spelled correctly.", IssueSeverity.ConfigurationError));
            }
            return t;
        }
        protected IPlugin CreatePluginByName(string name) {
            Type t = GetPluginType(name);
            object plugin = Activator.CreateInstance(t, false);

            if (!(plugin is IPlugin))
                this.AcceptIssue(new Issue("Plugins", "Specified plugin doesn't implement IPlugin as required: " + t.ToString(),null, IssueSeverity.ConfigurationError));

            return plugin as IPlugin;
        }



        protected SafeList<BuilderExtension> imageBuilderExtensions = null;
        /// <summary>
        /// Currently registered set of ImageBuilderExtensions. 
        /// </summary>
        public SafeList<BuilderExtension> ImageBuilderExtensions { get { return imageBuilderExtensions; } }

        protected SafeList<IEncoder> imageEncoders = null;
        /// <summary>
        /// Currently registered IEncoders. 
        /// </summary>
        public SafeList<IEncoder> ImageEncoders { get { return imageEncoders; } }

        protected SafeList<ICache> cachingSystems = null;
        /// <summary>
        /// Currently registered ICache instances
        /// </summary>
        public SafeList<ICache> CachingSystems { get { return cachingSystems; } }

        protected SafeList<IQuerystringPlugin> querystringPlugins = null;
        /// <summary>
        /// Plugins which accept querystring arguments are registered here.
        /// </summary>
        public SafeList<IQuerystringPlugin> QuerystringPlugins { get { return querystringPlugins; } }


        protected SafeList<IFileExtensionPlugin> fileExtensionPlugins = null;
        /// <summary>
        /// Plugins which accept new file extensions (in the url) are registered here.
        /// </summary>
        public SafeList<IFileExtensionPlugin> FileExtensionPlugins { get { return fileExtensionPlugins; } }


        protected SafeList<IPlugin> allPlugins = null;
        /// <summary>
        /// All plugins should be registered here. Used for diagnostic purposes.
        /// </summary>
        public SafeList<IPlugin> AllPlugins { get { return allPlugins; } }


        public IEncoderProvider EncoderProvider { get { return this; } }

        /// <summary>
        /// Returns an instance of the first encoder that claims to be able to handle the specified settings.
        /// Returns null if no encoders are available.
        /// </summary>
        /// <param name="originalImage"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettings settings) {
            
            foreach (IEncoder e in this.ImageEncoders) {
                IEncoder result = e.CreateIfSuitable(originalImage, settings);
                if (result != null) return result;
            }
            return null;
        }



        /// <summary>
        /// For use only by plugins during .Uninstall.
        /// Removes the specified plugin from AllPlugins, QuerystringPlugins, CachingSystems, ImageEncoders, and ImageBuiderExtensions, based
        /// on which interfaces the instance implements.
        /// Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void remove_plugin(object plugin) {
            if (plugin is IPlugin) AllPlugins.Remove(plugin as IPlugin);
            if (plugin is IQuerystringPlugin) QuerystringPlugins.Remove(plugin as IQuerystringPlugin);
            if (plugin is IFileExtensionPlugin) FileExtensionPlugins.Remove(plugin as IFileExtensionPlugin);
            if (plugin is ICache) CachingSystems.Remove(plugin as ICache);
            if (plugin is IEncoder) ImageEncoders.Remove(plugin as IEncoder);
            if (plugin is BuilderExtension) ImageBuilderExtensions.Remove(plugin as BuilderExtension);
        }

        /// <summary>
        /// For use only by plugins during .Install. Call Plugin.Install instead of this method.
        /// Adds the specified plugin to AllPlugins, QuerystringPlugins, CachingSystems, ImageEncoders, and ImageBuiderExtensions, based
        /// on which interfaces the instance implements. For ICache and IEncoder, the plugin is inserted at the beginning of CachingSystems and ImageEncoders respectively.
        /// Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void add_plugin(IPlugin plugin) {
            AllPlugins.Add(plugin);
            if (plugin is IQuerystringPlugin) QuerystringPlugins.Add(plugin as IQuerystringPlugin);
            if (plugin is IFileExtensionPlugin) FileExtensionPlugins.Add(plugin as IFileExtensionPlugin);
            if (plugin is ICache) CachingSystems.AddFirst(plugin as ICache);
            if (plugin is IEncoder) ImageEncoders.AddFirst(plugin as IEncoder);
            if (plugin is BuilderExtension) ImageBuilderExtensions.Add(plugin as BuilderExtension);
        }




    }
}
