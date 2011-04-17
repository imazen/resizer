using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using ImageResizer.Plugins;
using ImageResizer.Caching;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

namespace ImageResizer.Configuration {
    public class PluginConfig :IssueSink, IEncoderProvider {

        public override IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>(base.GetIssues());
            //Verify all plugins are registered as IPlugins also.
            //Verify there is something other than NoCache registered
            if (c.Plugins.CachingSystems.First is ImageResizer.Plugins.Basic.NoCache)
                issues.Add(new Issue("NoCache is only for development usage, and cannot scale to production use."));

            if (c.Plugins.ImageEncoders.First is ImageResizer.Plugins.Basic.DefaultEncoder)
                issues.Add(new Issue("DefaultEncoder produces poor GIF and 8-bit PNG results. PrettyGIFs is suggested."));
            
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
                    add_plugin_by_name(n["name"]);
                else if (n.Name.Equals("remove", StringComparison.OrdinalIgnoreCase))
                    remove_plugins_by_name(n["name"]);
                else if (n.Name.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    clear_plugins_by_type(n["type"]);
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

        public void remove_plugins_by_name(string name) {
            Type t = get_plugin_type(name);
            if (t == null) return; //Failed to acquire type
            foreach (IPlugin p in GetPluginsByType(t))
                if (!p.Uninstall(c)) AcceptIssue(new Issue("Plugin " + t.FullName + " reported a failed uninstall attempt triggered by a <remove name=\"" + name + "\" />.", IssueSeverity.Error));
        }
        public void add_plugin_by_name(string name) {
            IPlugin p = CreatePluginByName(name);
            p.Install(c);
        }
        public void clear_plugins_by_type(string type) {
            Type t = null;
            if ("encoders".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(IEncoder);
            if ("caches".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(ICache);
            if ("extensions".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(BuilderExtension);
            if ("all".Equals(type, StringComparison.OrdinalIgnoreCase) || type == null) t = typeof(IPlugin);
            if (t == null) this.AcceptIssue(new Issue("Unrecognized type value \"" + type + "\" in <clear type=\"" + type + "\" />.", IssueSeverity.ConfigurationError));
            else {
                IList<IPlugin> results = GetPluginsByType(t);
                foreach (IPlugin p in results)
                    if (!p.Uninstall(c)) AcceptIssue(new Issue("Plugin " + p.GetType().FullName + " reported a failed uninstall attempt triggered by a <clear type=\"" + type + "\" />.", IssueSeverity.Error));
            }
        }

        public Type get_plugin_type(string searchName) {
            Type t = null;

            int commaAt = searchName.IndexOf(',');
            int dotAt = searchName.IndexOf('.');

            //If there is a dot or period, try the exact name first.
            if (dotAt > -1 || commaAt > -1)
                t = Type.GetType(searchName, false, true);

            if (t != null) return t;

            //Split the name and assembly apart
            string assembly = commaAt > -1 ? searchName.Substring(commaAt) : null;
            string name = commaAt > -1 ? searchName.Substring(0, commaAt) : searchName;

            //Without the assembly specified, does it still have a name?
            bool hasDot = name.IndexOf('.') > -1;

            List<string> alternateNames = new List<string>();
            //ImageResizer.Plugins.Basic.DefaultEncoder
            if (hasDot) alternateNames.Add(name);
            //DefaultEncoder, NoCache, etc.
            alternateNames.Add("ImageResizer.Plugins.Basic." + name.TrimStart('.'));
            //AnimatedGifs
            if (!hasDot) alternateNames.Add("ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.') + "Plugin");
            //AnimatedGifsPlugin
            if (!hasDot && name.EndsWith("Plugin")) 
                alternateNames.Add("ImageResizer.Plugins." + name.Substring(0,name.Length - 6).Trim('.') + "." + name.Trim('.'));
            //Basic.DefaultEncoder
            alternateNames.Add("ImageResizer.Plugins." + name.TrimStart('.'));
            //For the depreciated convention of naming the plugin namespace and class the same.
            if (!hasDot) alternateNames.Add("ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.'));
            //Plugins.Basic.DefaultEncoder
            alternateNames.Add("ImageResizer." + name.TrimStart('.'));
            //PluginWithNoNamespace
            if (!hasDot) alternateNames.Add(name);

            List<string> assemblies = new List<string>();
            //1) If an assembly was specified, search it first
            if (assembly != null) assemblies.Add(assembly);
            //2) Follow by searching the Core, the currently executing assembly
            assemblies.Add(""); // Defaults to current assembly
            //3) Next, add all assemblies that have "ImageResizer" in their name 
            List<string> otherAssemblies = new List<string>();
             //Add ImageResizer-related assemblies first
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
                string aname = a.FullName;
                if (aname.IndexOf("ImageResizer", StringComparison.OrdinalIgnoreCase) > -1) assemblies.Add(", " + aname);
                else 
                    otherAssemblies.Add(", " + aname);
            }
            //4) Last, add all remaining assemblies
            assemblies.AddRange(otherAssemblies);

            //Now multiply 
            List<string> qualifiedNames = new List<string>(assemblies.Count * alternateNames.Count);

            //For each assembly, try each namespace-qualified class name.
            foreach (string assemblyName in assemblies) {
                foreach (string className in alternateNames)
                    qualifiedNames.Add(className + assemblyName);
            }


            //Now, try them all.
            foreach (string s in qualifiedNames) {
                if (t != null) return t;
                Debug.WriteLine("Trying " + s);
                t = Type.GetType(s, false, true);
                if (t != null) Debug.WriteLine("Success!");
            }


            //Ok, time to log problem.
            if (t == null) {
                StringBuilder attempts = new StringBuilder();
                foreach (string s in qualifiedNames)
                    attempts.Append(", \"" + s + "\"");
                this.AcceptIssue(new Issue( "Failed to load plugin by name \"" + searchName + "\"",
                    "Verify the plugin DLL is located in /bin, and that the name is spelled correctly. \n" +
                    "Attempted using \"" + searchName + "\"" + attempts.ToString() + ".", IssueSeverity.ConfigurationError));
            }
            return t;
        }
        protected IPlugin CreatePluginByName(string name) {
            Type t = get_plugin_type(name);
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





        public void RemoveAllPlugins() {
            //Follow uninstall protocol
            foreach (IPlugin p in AllPlugins)
                p.Uninstall(c);

            IList[] collections = new IList[]{AllPlugins.GetCollection(),QuerystringPlugins.GetCollection(),FileExtensionPlugins.GetCollection()
                                                ,CachingSystems.GetCollection(),ImageEncoders.GetCollection(),ImageBuilderExtensions.GetCollection()};
            //Then check all collections, logging an issue if they aren't empty.

            foreach(IList coll in collections){
                if (coll.Count > 0){
                    string items = "";
                    foreach(object item in coll)
                        items += item.ToString() + ", ";

                    this.AcceptIssue(new Issue("Collection " + coll.ToString() + " was not empty after RemoveAllPlugins() executed!",
                                        "Remaining items: " + items, IssueSeverity.Error));
                }

            }
            
        }
    }
}
