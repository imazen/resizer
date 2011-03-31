using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Encoding;
using fbs.ImageResizer.Plugins;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Resizing;
using fbs.ImageResizer.Configuration.Xml;
using fbs.ImageResizer.Configuration.Issues;

namespace fbs.ImageResizer.Configuration {
    public class PluginConfig :IssueSink, IEncoderProvider {
        protected Config c;
        /// <summary>
        /// Creates a new plugin config section, attached to the specified parent
        /// </summary>
        /// <param name="c"></param>
        public PluginConfig(Config c) : base("Plugins"){
            this.c = c;
            imageBuilderExtensions = new SafeList<ImageBuilderExtension>();
            imageEncoders = new SafeList<IEncoder>();
            cachingSystems = new SafeList<ICache>();
            urlModifyingPlugins = new SafeList<IUrlPlugin>();
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

        public IList<IPlugin> GetPluginsByType(Type type) {
            List<IPlugin> results = new List<IPlugin>();
            foreach (IPlugin p in AllPlugins)
                if (p.GetType().IsAssignableFrom(type)) //Like instance of. Can be a subclass
                    results.Add(p); 
            return results;
        }

        protected void removePluginsByName(string name) {
            Type t = GetPluginType(name);
            if (t == null) return; //Failed to acquire type
            foreach (IPlugin p in GetPluginsByType(t))
                p.Uninstall(c);
        }
        protected void addPluginByName(string name) {
            IPlugin p = CreatePluginByName(name);
            p.Install(c);
        }
        protected void clearPluginsByType(string type) {
            Type t = null;
            if ("encoders".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(IEncoder);
            if ("caches".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(ICache);
            if ("imagebuilderextensions".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(ImageBuilderExtension);
            if ("all".Equals(type, StringComparison.OrdinalIgnoreCase) || type == null) t = typeof(IPlugin);
            if (t == null) this.AcceptIssue(new Issue("Plugins", "Unrecognized type value \"" + type + "\" in clearPluginsByType(type).", "", IssueSeverity.ConfigurationError));
            else {
                IList<IPlugin> results = GetPluginsByType(t);
                foreach (IPlugin p in results)
                    p.Uninstall(c);
            }
        }

        protected Type GetPluginType(string name) {
            Type t = null;

            string convention = "fbs.ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.');
            string alternate = "fbs.ImageResizer.Plugins." + name.TrimStart('.');

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
                this.AcceptIssue(new Issue("Plugins", "Failed to load plugin by name \"" + name + "\"",
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



        protected SafeList<ImageBuilderExtension> imageBuilderExtensions = null;
        /// <summary>
        /// Currently registered set of ImageBuilderExtensions. 
        /// </summary>
        public SafeList<ImageBuilderExtension> ImageBuilderExtensions { get { return imageBuilderExtensions; } }

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

        protected SafeList<IUrlPlugin> urlModifyingPlugins = null;
        /// <summary>
        /// Plugins which accept new querystring arguments or new file extensions are registered here.
        /// </summary>
        public SafeList<IUrlPlugin> UrlModifyingPlugins { get { return urlModifyingPlugins; } }

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
        /// Removes the specified plugin from AllPlugins, UrlModifyingPlugins, CachingSystems, ImageEncoders, and ImageBuiderExtensions, based
        /// on which interfaces the instance implements.
        /// Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void remove_plugin(object plugin) {
            if (plugin is IPlugin) AllPlugins.Remove(plugin as IPlugin);
            if (plugin is IUrlPlugin) UrlModifyingPlugins.Remove(plugin as IUrlPlugin);
            if (plugin is ICache) CachingSystems.Remove(plugin as ICache);
            if (plugin is IEncoder) ImageEncoders.Remove(plugin as IEncoder);
            if (plugin is ImageBuilderExtension) ImageBuilderExtensions.Remove(plugin as ImageBuilderExtension);
        }

        /// <summary>
        /// For use only by plugins during .Install. Call Plugin.Install instead of this method.
        /// Adds the specified plugin to AllPlugins, UrlModifyingPlugins, CachingSystems, ImageEncoders, and ImageBuiderExtensions, based
        /// on which interfaces the instance implements. For ICache and IEncoder, the plugin is inserted at the beginning of CachingSystems and ImageEncoders respectively.
        /// Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void add_plugin(IPlugin plugin) {
            AllPlugins.Add(plugin);
            if (plugin is IUrlPlugin) UrlModifyingPlugins.Add(plugin as IUrlPlugin);
            if (plugin is ICache) CachingSystems.AddFirst(plugin as ICache);
            if (plugin is IEncoder) ImageEncoders.AddFirst(plugin as IEncoder);
            if (plugin is ImageBuilderExtension) ImageBuilderExtensions.Add(plugin as ImageBuilderExtension);
        }




    }
}
