// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using ImageResizer.Collections;
using Imazen.Common.Issues;
using ImageResizer.Configuration.Logging;
using ImageResizer.Configuration.Plugins;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins;
using ImageResizer.Plugins.Basic;
using ImageResizer.Resizing;

namespace ImageResizer.Configuration
{
    /// <summary>
    ///     Provides thread-safe access to plugin addition, removal, and querying methods
    /// </summary>
    public class PluginConfig : IssueSink, IEncoderProvider
    {
        protected NativeDependencyManager ndeps = new NativeDependencyManager();

        protected Config c;

        /// <summary>
        ///     Creates a new plugin config section, attached to the specified parent
        /// </summary>
        /// <param name="c"></param>
        public PluginConfig(Config c) : base("Plugins")
        {
            this.c = c;
            imageBuilderExtensions = new SafeList<BuilderExtension>();
            imageEncoders = new SafeList<IEncoder>();
            cachingSystems = new SafeList<IAsyncTyrantCache>();
            querystringPlugins = new SafeList<IQuerystringPlugin>();
            fileExtensionPlugins = new SafeList<IFileExtensionPlugin>();
            allPlugins = new SafeList<IPlugin>();
            virtualProviderPlugins = new SafeList<IVirtualImageProvider>();
            settingsModifierPlugins = new SafeList<ISettingsModifier>();
            configProviders = new SafeList<ICurrentConfigProvider>();
            LicenseError = c.getNode("licenses")?.Attrs?.Get<LicenseErrorAction>("licenseError") ?? LicenseError;
            LicenseScope = c.getNode("licenses")?.Attrs?.Get<LicenseAccess>("licenseScope") ?? LicenseScope;
        }

        internal PluginLoadingHints hints = new PluginLoadingHints();

        [CLSCompliant(false)] protected volatile bool _pluginsLoaded = false;

        protected object _loadPluginsSync = new object();

        /// <summary>
        ///     Processes the &lt;plugins&gt; section if they are not already loaded. Thread safe.
        ///     Only executes once.
        /// </summary>
        public void LoadPlugins()
        {
            lock (_loadPluginsSync)
            {
                if (_pluginsLoaded) return;
                loadPluginsInternal();
                _pluginsLoaded = true;
            }
        }

        /// <summary>
        ///     Forces loading of the plugins specified in configuration, even if they have already been loaded. Will add duplicate
        ///     plugins and throw exceptions if you do not call RemoveAll() first.
        /// </summary>
        public void ForceLoadPlugins()
        {
            lock (_loadPluginsSync)
            {
                loadPluginsInternal();
                _pluginsLoaded = true;
            }
        }

        /// <summary>
        ///     Returns true if the &lt;plugins&gt; section has been processed
        /// </summary>
        public bool PluginsLoaded
        {
            get
            {
                lock (_loadPluginsSync)
                {
                    return _pluginsLoaded;
                }
            }
        }

        /// <summary>
        ///     Not thread safe. Performs actual work.
        /// </summary>
        protected void loadPluginsInternal()
        {
            var plugins = c.getNode("plugins");
            if (plugins == null) return;
            foreach (var n in plugins.Children)
                if (n.Name.Equals("add", StringComparison.OrdinalIgnoreCase))
                    add_plugin_by_name(n["name"], n.Attrs.Count > 1 ? n.Attrs : null);
                else if (n.Name.Equals("remove", StringComparison.OrdinalIgnoreCase))
                    remove_plugins_by_name(n["name"]);
                else if (n.Name.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    clear_plugins_by_type(n["type"]);
                else
                    AcceptIssue(new Issue("Plugins", "Unexpected element <" + n.Name + "> in <plugins></plugins>.",
                        "Element XML: " + n.ToXmlElement().OuterXml, IssueSeverity.Warning));
        }

        /// <summary>
        ///     Returns the subset of installed plugins which implement the specified type or interface
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IList<IPlugin> GetPlugins(Type type)
        {
            var results = new List<IPlugin>();
            foreach (var p in AllPlugins)
                if (type.IsAssignableFrom(p.GetType())) //Like instance of. Can be a subclass
                    results.Add(p);
            return results;
        }

        /// <summary>
        ///     Returns all registered instances of the specified plugins
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IList<T> GetAll<T>()
        {
            var results = new List<T>();
            var t = typeof(T);
            foreach (var p in AllPlugins)
                if (t.IsAssignableFrom(p.GetType())) //Like instance of. Can be a subclass
                    results.Add((T)p);
            return results;
        }

        /// <summary>
        ///     Returns true if at least one plugin of the specified type is registered.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasPlugin(Type type)
        {
            foreach (var p in AllPlugins)
                if (type.IsAssignableFrom(p.GetType())) //Like instance of. Can be a subclass
                    return true;
            return false;
        }

        /// <summary>
        ///     Returns true if 1 or more instances of the type are registered.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Has<T>()
        {
            return Comparer<T>.Default.Compare(Get<T>(), default) != 0;
        }


        /// <summary>
        ///     Returns the first registered instance of the specified plugin. For IMultiInstancePlugins, use GetAll()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            var t = typeof(T);
            foreach (var p in AllPlugins)
                if (t.IsAssignableFrom(p.GetType())) //Like instance of. Can be a subclass
                    return (T)p;
            return default;
        }

        /// <summary>
        ///     Returns the first registered instance of the specified plugin, or creates a new instance if the plugin isn't
        ///     installed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrInstall<T>() where T : IPlugin, new()
        {
            var instance = Get<T>();
            if (instance != null) return instance;

            new T().Install(c);

            return Get<T>();
        }

        /// <summary>
        ///     Returns the first registered instance of the specified plugin, or installs the given instance instead, then
        ///     re-tries the query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newInstance"></param>
        /// <returns></returns>
        public T GetOrInstall<T>(T newInstance) where T : IPlugin
        {
            var instance = Get<T>();
            if (instance != null) return instance;

            //Our instance may not get installed if there is a duplicate already
            newInstance.Install(c);

            return Get<T>();
        }


        /// <summary>
        ///     Installs the specified plugin, returning the plugin instance.
        ///     Convenience method, same as plugin.Install(Config.Current).
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public IPlugin Install(IPlugin plugin)
        {
            return plugin.Install(c);
        }

        /// <summary>
        ///     Attempts uninstallation of the specified plugin, returning true if successful.
        ///     Convenience method, same as plugin.Uninstall(Config.Current).
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public bool Uninstall(IPlugin plugin)
        {
            return plugin.Uninstall(c);
        }


        protected SafeList<ICurrentConfigProvider> configProviders = null;

        /// <summary>
        ///     Currently registered ICurrentConfigProviders.
        /// </summary>
        public SafeList<ICurrentConfigProvider> ConfigProviders => configProviders;


        protected SafeList<BuilderExtension> imageBuilderExtensions = null;

        /// <summary>
        ///     Currently registered set of ImageBuilderExtensions.
        /// </summary>
        public SafeList<BuilderExtension> ImageBuilderExtensions => imageBuilderExtensions;

        protected SafeList<IEncoder> imageEncoders = null;

        /// <summary>
        ///     Currently registered IEncoders.
        /// </summary>
        public SafeList<IEncoder> ImageEncoders => imageEncoders;

        protected SafeList<IAsyncTyrantCache> cachingSystems = null;

        /// <summary>
        ///     Currently registered ICache instances
        /// </summary>
        public SafeList<IAsyncTyrantCache> CachingSystems => cachingSystems;

        protected SafeList<IQuerystringPlugin> querystringPlugins = null;

        /// <summary>
        ///     Plugins which accept querystring arguments are registered here.
        /// </summary>
        public SafeList<IQuerystringPlugin> QuerystringPlugins => querystringPlugins;


        protected SafeList<IFileExtensionPlugin> fileExtensionPlugins = null;

        /// <summary>
        ///     Plugins which accept new file extensions (in the URL) are registered here.
        /// </summary>
        public SafeList<IFileExtensionPlugin> FileExtensionPlugins => fileExtensionPlugins;


        protected SafeList<IVirtualImageProvider> virtualProviderPlugins = null;

        /// <summary>
        ///     Plugins which provide virtual files are registered here.
        /// </summary>
        public SafeList<IVirtualImageProvider> VirtualProviderPlugins => virtualProviderPlugins;

        protected SafeList<ISettingsModifier> settingsModifierPlugins = null;

        /// <summary>
        ///     Plugins which modify image processing settings.
        /// </summary>
        public SafeList<ISettingsModifier> SettingsModifierPlugins => settingsModifierPlugins;


        private SafeList<IPluginModifiesRequestCacheKey> modifiesRequestCacheKeyPlugins = null;

        /// <summary>
        ///     Plugins which modify image processing settings.
        /// </summary>
        public SafeList<IPluginModifiesRequestCacheKey> ModifiesRequestCacheKeyPlugins => modifiesRequestCacheKeyPlugins;

            
        protected SafeList<IPlugin> allPlugins = null;

        /// <summary>
        ///     All plugins should be registered here. Used for diagnostic purposes.
        /// </summary>
        public SafeList<IPlugin> AllPlugins => allPlugins;


        public IEncoderProvider EncoderProvider => this;

        protected ILogManager _logManager = null;

        /// <summary>
        ///     Returns the most recently registered Logging plugin, or null.
        /// </summary>
        public ILogManager LogManager
        {
            get => _logManager;
            set
            {
                _logManager = value;
                if (LoggingAvailable != null && value != null) LoggingAvailable(value);
            }
        }

        public delegate void LoggingAvailableEvent(ILogManager logger);

        public LoggingAvailableEvent LoggingAvailable;

        /// <summary>
        ///     Returns an instance of the first encoder that claims to be able to handle the specified settings.
        ///     Returns null if no encoders are available.
        /// </summary>
        /// <param name="settings">Request settings, like format, quality, colors, dither, etc.</param>
        /// <param name="original">
        ///     May be a Drawing.Image instance, a path, or null. To provide both, set Image.tag to the path. Helps the encoder
        ///     detect the original format if the format was not specified.
        ///     May also be used for palette generation hinting by some encoders.
        /// </param>
        /// <returns></returns>
        public IEncoder GetEncoder(ResizeSettings settings, object original)
        {
            foreach (var e in ImageEncoders)
            {
                var result = e.CreateIfSuitable(settings, original);
                if (result != null) return result;
            }

            return null;
        }


        protected void remove_plugins_by_name(string name)
        {
            var t = FindPluginType(name);
            if (t == null) return; //Failed to acquire type
            foreach (var p in GetPlugins(t))
                if (!p.Uninstall(c))
                    AcceptIssue(new Issue(
                        "Plugin " + t.FullName + " reported a failed uninstall attempt triggered by a <remove name=\"" +
                        name + "\" />.", IssueSeverity.Error));
        }

        protected void add_plugin_by_name(string name, NameValueCollection args)
        {
            var p = CreatePluginByName(name, args);
            if (p == null) return; //failed

            //Don't allow duplicates
            if (!(p is IMultiInstancePlugin) && HasPlugin(p.GetType()))
            {
                AcceptIssue(new Issue(
                    "An instance of the specified plugin (" + p.GetType().ToString() +
                    ") has already been added. Implement IMultiInstancePlugin if the plugin supports multiple instances.",
                    IssueSeverity.ConfigurationError));
                return;
            }

            p.Install(c);
        }

        /// <summary>
        ///     Returns null on failure. Check GetIssues() for causes.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pluginConfig"></param>
        /// <returns></returns>
        public IPlugin AddPluginByName(string name, NameValueCollection pluginConfig = null)
        {
            var p = CreatePluginByName(name, pluginConfig);
            if (p == null) return null; //failed

            //Don't allow duplicates
            if (!(p is IMultiInstancePlugin) && HasPlugin(p.GetType()))
            {
                AcceptIssue(new Issue(
                    "An instance of the specified plugin (" + p.GetType().ToString() +
                    ") has already been added. Implement IMultiInstancePlugin if the plugin supports multiple instances.",
                    IssueSeverity.ConfigurationError));
                return null;
            }

            p.Install(c);
            return p;
        }

        protected void clear_plugins_by_type(string type)
        {
            Type t = null;
            if ("encoders".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(IEncoder);
            if ("caches".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(IAsyncTyrantCache);
            if ("extensions".Equals(type, StringComparison.OrdinalIgnoreCase)) t = typeof(BuilderExtension);
            if ("all".Equals(type, StringComparison.OrdinalIgnoreCase) || type == null) t = typeof(IPlugin);
            if (t == null)
            {
                AcceptIssue(new Issue("Unrecognized type value \"" + type + "\" in <clear type=\"" + type + "\" />.",
                    IssueSeverity.ConfigurationError));
            }
            else
            {
                var results = GetPlugins(t);
                foreach (var p in results)
                    if (!p.Uninstall(c))
                        AcceptIssue(new Issue(
                            "Plugin " + p.GetType().FullName +
                            " reported a failed uninstall attempt triggered by a <clear type=\"" + type + "\" />.",
                            IssueSeverity.Error));
            }
        }

        /// <summary>
        ///     This is called to get a sorted list of plugins based on their likelihood of having the plugin.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        protected List<string> GetOptimizedAssemblyList(string assemblyName, string pluginName)
        {
            var assemblies = new List<string>();
            //1) If an assembly was specified, search it first
            if (assemblyName != null) assemblies.Add(assemblyName);
            //2) Follow by searching the Core, the currently executing assembly
            assemblies.Add(""); // Defaults to current assembly
            //3) Add ImageResizer.Plugins.X if it has no dot.
            if (!pluginName.Contains(".")) assemblies.Add("ImageResizer.Plugins." + pluginName);

            //4) Next, add all assemblies that have "ImageResizer" in their name 


            var otherAssemblies = new List<string>();
            //Add ImageResizer-related assemblies first
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                // AppDomain.CurrentDomain.GetAssemblies()
                var aname = a.FullName;
                if (aname.IndexOf("ImageResizer", StringComparison.OrdinalIgnoreCase) > -1)
                    assemblies.Add(", " + aname);
                else
                    otherAssemblies.Add(", " + aname);
            }

            //5) Last, add all remaining assemblies
            assemblies.AddRange(otherAssemblies);
            return assemblies;
        }

        private Tuple<string, string> ParseName(string typeName)
        {
            var commaAt = typeName.IndexOf(',');
            var assemblySearchName = commaAt > -1 ? typeName.Substring(commaAt + 1).Trim() : null;
            assemblySearchName = string.IsNullOrEmpty(assemblySearchName) ? null : assemblySearchName;
            var name = commaAt > -1 ? typeName.Substring(0, commaAt).Trim() : typeName.Trim();
            return new Tuple<string, string>(name, assemblySearchName);
        }

        /// <summary>
        ///     Searches all loaded assemblies for the specified type, applying rules and prefixes to resolve the namespace and
        ///     assembly.
        ///     Returns null if it could not find the type, and logs an issue.
        /// </summary>
        /// <param name="searchNameString"></param>
        /// <returns></returns>
        public Type FindPluginType(string searchNameString)
        {
            Type t = null;
            var attemptedExpansions = new List<string>();
            try
            {
                var searchName = ParseName(searchNameString);
                var preloadAssemblies = false;

                //Load the assembly if specified.
                if (preloadAssemblies & (searchName.Item2 != null))
                    Assembly.Load(searchName.Item2);

                var dotAt = searchName.Item1.IndexOf('.');

                //If there is a dot or period, try the exact name first.
                if (dotAt > -1 || searchName.Item2 != null)
                {
                    attemptedExpansions.Add(searchNameString);
                    t = Type.GetType(searchNameString, false, true);
                }

                if (t != null) return t;

                var nameVariations =
                    new[]
                    {
                        searchName.Item1, searchNameString, searchName.Item1.Replace("Plugin", ""),
                        searchNameString.Replace("Plugin", "")
                    }.Distinct(StringComparer.OrdinalIgnoreCase);

                var officialExpansions = new List<string>();
                foreach (var nameVariant in nameVariations)
                {
                    var expansions = hints.GetExpansions(nameVariant);
                    if (expansions != null)
                        officialExpansions.AddRange(expansions);
                }

                if (officialExpansions.Count > 0)
                {
                    foreach (var s in officialExpansions)
                    {
                        attemptedExpansions.Add(s);
                        var parsedName = ParseName(s);
                        if (preloadAssemblies && parsedName.Item2 != null)
                        {
                            Debug.WriteLine("PluginConfig is loading assembly " + parsedName.Item2 + " to try " +
                                            parsedName.Item1);
                            var a = Assembly.Load(parsedName.Item2);
                            t = a.GetType(parsedName.Item1, false, true);
                        }
                        else
                        {
                            Debug.WriteLine("Trying " + s);
                            t = Type.GetType(s, false, true);
                        }

                        if (t != null)
                        {
                            Debug.WriteLine("Success!");
                            return t;
                        }
                    }

                    var searchedAssemblies = officialExpansions
                        .Select((s) => s.IndexOf(',') > -1 ? s.Substring(s.IndexOf(',') + 1).Trim() : "").Distinct();


                    var attempts = new StringBuilder();
                    foreach (var s in attemptedExpansions)
                        attempts.Append(", \"" + s + "\"");

                    var assemblyNames = new StringBuilder();
                    foreach (var s in searchedAssemblies)
                        assemblyNames.Append("\"" + s + "\" ");
                    AcceptIssue(new Issue("Failed to load plugin by name \"" + searchName + "\"",
                        "Verify that " + assemblyNames.ToString() + " is located in /bin. \n" +
                        "Attempted using \"" + searchName + "\"" + attempts.ToString() + ".",
                        IssueSeverity.ConfigurationError));
                }
                else if (searchName.Item2 != null)
                {
                    AcceptIssue(new Issue("Failed to load plugin by name \"" + searchName + "\"",
                        "Verify that \"" + searchName.Item2 +
                        "\".dll is located in /bin, and that the name is spelled correctly.",
                        IssueSeverity.ConfigurationError));
                }
                else
                {
                    var possibleExpansions = new List<string>();
                    //Split the name  apart
                    var name = searchName.Item1;
                    var hasDot = name.IndexOf('.') > -1;

                    var alternateNames = new List<string>();
                    //ImageResizer.Plugins.Basic.DefaultEncoder
                    if (hasDot)
                    {
                        alternateNames.Add(name);
                    }
                    else
                    {
                        if (name.EndsWith("Plugin"))
                        {
                            //Standard syntax
                            alternateNames.Add("ImageResizer.Plugins." + name.Substring(0, name.Length - 6).Trim('.') +
                                               "." + name.Trim('.'));
                        }
                        else
                        {
                            //Standard syntax
                            alternateNames.Add("ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.') +
                                               "Plugin");
                            //For the deprecated (but still used)convention of naming the plugin namespace and class the same.
                            alternateNames.Add("ImageResizer.Plugins." + name.Trim('.') + "." + name.Trim('.'));
                        }

                        //PluginWithNoNamespace
                        alternateNames.Add(name);
                    }

                    //Get a list of assemblies, sorted by likelihood of a match
                    var assemblies = GetOptimizedAssemblyList(searchName.Item2, name);
                    //Now multiply - For each assembly, try each namespace-qualified class name.
                    foreach (var assemblyName in assemblies)
                    foreach (var className in alternateNames)
                        possibleExpansions.Add(className + assemblyName);


                    foreach (var s in possibleExpansions)
                    {
                        attemptedExpansions.Add(s);
                        Debug.WriteLine("Trying " + s);
                        t = Type.GetType(s, false, true);
                        if (t != null)
                        {
                            Debug.WriteLine("Success!");
                            return t;
                        }
                    }


                    //Ok, time to log problem.
                    if (t == null)
                    {
                        var attempts = new StringBuilder();
                        foreach (var s in attemptedExpansions)
                            attempts.Append(", \"" + s + "\"");
                        AcceptIssue(new Issue("Failed to load plugin by name \"" + searchName + "\"",
                            "This is not a recognized plugin name. Check spelling. \n" +
                            "Attempted using \"" + searchName + "\"" + attempts.ToString() + ".",
                            IssueSeverity.ConfigurationError));
                    }
                }
            }
            catch (SecurityException sx)
            {
                AcceptIssue(new Issue(
                    "Failed to load plugin \"" + searchNameString + "\" due to ASP.NET trust configuration. ",
                    "You may need to increase the trust level for this plugin to load properly. Error details: \n" +
                    sx.Message + "\n" + sx.StackTrace, IssueSeverity.Error));
                return null;
            }

            return t;
        }


        protected IPlugin CreatePluginByName(string name, NameValueCollection args)
        {
            var t = FindPluginType(name);

            if (t == null) return null;
            return CreatePluginByType(t, args);
        }

        public void LoadNativeDependenciesForType(Type t)
        {
            ndeps.EnsureLoaded(t.Assembly);
        }


        protected IPlugin CreatePluginByType(Type t, NameValueCollection args)
        {
            //TODO - perhaps manually select the constructor ? 
            //ConstructorInfo ci = null;
            var downloadDependencies = false;
            if (args != null && args["downloadNativeDependencies"] != null)
            {
                downloadDependencies =
                    "true".Equals(args["downloadNativeDependencies"], StringComparison.OrdinalIgnoreCase);
                if (args.Count == 2)
                    args = null; //Don't require plugins to have an argument-supporting constructor just for downloadNativeDependencies
                //'name' is included in args, remember.
                ndeps.EnsureLoaded(t.Assembly);
            }


            var hasConstructor = true;
            if (args != null && t.GetConstructor(new[] { typeof(NameValueCollection) }) == null)
            {
                args = null; //The plugin doesn't accept arguments
                AcceptIssue(new Issue("Plugins",
                    "The plugin " + t.ToString() + " doesn't support constructor arguments.",
                    "To support arguments in the <add> element, the plugin must have a public constructor that accepts a NameValueCollection argument.",
                    IssueSeverity.ConfigurationError));
            }
            else if (args == null && t.GetConstructor(Type.EmptyTypes) == null)
            {
                var acceptsArgs = t.GetConstructor(new[] { typeof(NameValueCollection) }) == null;

                if (acceptsArgs)
                {
                    AcceptIssue(new Issue("Plugins",
                        "The plugin " + t.ToString() +
                        " requires arguments in the <add> element. Refer to the plugin documentation for details.",
                        null, IssueSeverity.ConfigurationError));
                }
                else
                {
                    AcceptIssue(new Issue("Plugins",
                        "The plugin " + t.ToString() +
                        " does not have a constructor Constructor() or Constructor(NameValueCollection args)."
                        , "To be compatible with the <plugins> section, a plugin must implement IPlugin and define one or more of the above constructors publicly.",
                        IssueSeverity.Critical));
                    hasConstructor = false;
                }
            }

            if (hasConstructor || Debugger.IsAttached)
            {
                object plugin = null;
                if (args == null)
                    plugin = Activator.CreateInstance(t, false);
                else
                    plugin = Activator.CreateInstance(t, args);

                if (!(plugin is IPlugin))
                    AcceptIssue(new Issue("Plugins",
                        "Specified plugin doesn't implement IPlugin as required: " + t.ToString(), null,
                        IssueSeverity.ConfigurationError));
                return plugin as IPlugin;
            }

            return null;
        }


        /// <summary>
        ///     For use only by plugins during .Uninstall.
        ///     Removes the specified plugin from AllPlugins, QuerystringPlugins, CachingSystems, ImageEncoders, and
        ///     ImageBuilderExtensions, based
        ///     on which interfaces the instance implements.
        ///     Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them
        ///     vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void remove_plugin(object plugin)
        {
            if (plugin is IPlugin) AllPlugins.Remove(plugin as IPlugin);
            if (plugin is IQuerystringPlugin) QuerystringPlugins.Remove(plugin as IQuerystringPlugin);
            if (plugin is IFileExtensionPlugin) FileExtensionPlugins.Remove(plugin as IFileExtensionPlugin);
            if (plugin is IAsyncTyrantCache) CachingSystems.Remove(plugin as IAsyncTyrantCache);
            if (plugin is IEncoder) ImageEncoders.Remove(plugin as IEncoder);
            if (plugin is BuilderExtension) ImageBuilderExtensions.Remove(plugin as BuilderExtension);
            if (plugin is IVirtualImageProvider) VirtualProviderPlugins.Remove(plugin as IVirtualImageProvider);
            if (plugin is ISettingsModifier) SettingsModifierPlugins.Remove(plugin as ISettingsModifier);
            if (plugin is ICurrentConfigProvider) ConfigProviders.Remove(plugin as ICurrentConfigProvider);
            if (plugin is ILogManager && LogManager == plugin) LogManager = null;
            if (plugin is ILicensedPlugin || plugin is ILicenseProvider) FireLicensePluginsChange();
            if (plugin is IPluginModifiesRequestCacheKey)  ModifiesRequestCacheKeyPlugins.Remove(plugin as IPluginModifiesRequestCacheKey);
        }
        

        /// <summary>
        ///     Only for use by plugins during IPlugin.Install. Call Plugin.Install instead of this method, since plugins often
        ///     must perform other initialization actions.
        ///     Adds the specified plugin to AllPlugins, QuerystringPlugins, CachingSystems, ImageEncoders, and
        ///     ImageBuilderExtensions, based
        ///     on which interfaces the instance implements. For ICache and IEncoder, the plugin is inserted at the beginning of
        ///     CachingSystems and ImageEncoders respectively.
        ///     To reiterate, plugins may register event handlers and modify settings - thus you should use the plugin's method to
        ///     uninstall them vs. using this method.
        ///     Will not register a plugin that is already installed, unless it implements IMultiInstancePlugin.
        /// </summary>
        /// <param name="plugin"></param>
        public void add_plugin(IPlugin plugin)
        {
            if (!(plugin is IMultiInstancePlugin) && HasPlugin(plugin.GetType()))
            {
                AcceptIssue(new Issue(
                    "An instance of the specified plugin (" + plugin.GetType().ToString() +
                    ") has already been registered.",
                    "The plugin should implement IMultiInstancePlugin to support multiple instances.",
                    IssueSeverity.Error));
                return;
            }

            AllPlugins.Add(plugin);
            if (plugin is IQuerystringPlugin) QuerystringPlugins.Add(plugin as IQuerystringPlugin);
            if (plugin is IFileExtensionPlugin) FileExtensionPlugins.Add(plugin as IFileExtensionPlugin);
            if (plugin is IAsyncTyrantCache) CachingSystems.AddFirst(plugin as IAsyncTyrantCache);
            if (plugin is IEncoder) ImageEncoders.AddFirst(plugin as IEncoder);
            if (plugin is BuilderExtension) ImageBuilderExtensions.Add(plugin as BuilderExtension);
            if (plugin is IVirtualImageProvider) VirtualProviderPlugins.Add(plugin as IVirtualImageProvider);
            if (plugin is ISettingsModifier) SettingsModifierPlugins.Add(plugin as ISettingsModifier);
            if (plugin is ICurrentConfigProvider) ConfigProviders.Add(plugin as ICurrentConfigProvider);
            if (plugin is IPluginModifiesRequestCacheKey)
                ModifiesRequestCacheKeyPlugins.Add(plugin as IPluginModifiesRequestCacheKey);
            if (plugin is ILogManager) LogManager = plugin as ILogManager;
            if (plugin is ILicensedPlugin || plugin is ILicenseProvider) FireLicensePluginsChange();
        }

        /// <summary>
        ///     Removes all plugins, of every kind. Logs any errors encountered. (Not all plugins support uninstallation)
        /// </summary>
        public void RemoveAll()
        {
            //Follow uninstall protocol
            foreach (var p in AllPlugins)
                if (!p.Uninstall(c))
                    AcceptIssue(new Issue("Uninstall of " + p.ToString() + " reported failure.", IssueSeverity.Error));

            var collections = new IList[]
            {
                AllPlugins.GetCollection(), QuerystringPlugins.GetCollection(), FileExtensionPlugins.GetCollection(),
                CachingSystems.GetCollection(), ImageEncoders.GetCollection(), ImageBuilderExtensions.GetCollection(),
            };
            //Then check all collections, logging an issue if they aren't empty.
            foreach (var coll in collections)
                if (coll.Count > 0)
                {
                    var items = "";
                    foreach (var item in coll)
                        items += item.ToString() + ", ";

                    AcceptIssue(new Issue(
                        "Collection " + coll.ToString() + " was not empty after RemoveAllPlugins() executed!",
                        "Remaining items: " + items, IssueSeverity.Error));
                }
        }


        public override IEnumerable<IIssue> GetIssues()
        {
            var issues = new List<IIssue>(base.GetIssues());
            //Verify all plugins are registered as IPlugins also.


            //Verify there is something other than NoCache registered
            if (c.Plugins.CachingSystems.First is NoCache)
                issues.Add(new Issue("NoCache is only for development usage, and cannot scale to production use.",
                    "Add DiskCache or CloudFront for production use", IssueSeverity.Warning));

            //Verify NoCache is registered
            if (!c.Plugins.Has<NoCache>())
                issues.Add(new Issue("NoCache should not be removed from the plugins collection.",
                    "Simply add the new ICache plugin later for it to take precedence. NoCache is still required as a fallback by most caching plugins.",
                    IssueSeverity.Error));


            if (c.Plugins.ImageEncoders.First == null)
                issues.Add(new Issue(
                    "No encoders are registered! Without an image encoder, the pipeline cannot function.",
                    IssueSeverity.Error));

            issues.AddRange(ndeps.GetIssues());

            return issues;
        }


        public delegate void LicensingChangeEvent(object sender, Config forConfig);

        /// <summary>
        ///     There has been a change in licensed or licensing plugins
        /// </summary>
        public event LicensingChangeEvent LicensePluginsChange;

        /// <summary>
        ///     Fires the LicensingChange event
        /// </summary>
        public void FireLicensePluginsChange()
        {
            LicensePluginsChange?.Invoke(this, c);
        }

        /// <summary>
        ///     Register the provided license. Will apply the Plugins.LicenseScope value.
        /// </summary>
        /// <param name="license"></param>
        public void AddLicense(string license)
        {
            StaticLicenseProvider.One(license).Install(c);
            FireLicensePluginsChange();
        }

        private LicenseAccess _licenseScope = LicenseAccess.Process;

        /// <summary>
        ///     If this Config should inherit and/or share licenses process-wide, or only use licenses specifically registered.
        /// </summary>
        public LicenseAccess LicenseScope
        {
            get => _licenseScope;
            set
            {
                _licenseScope = value;
                FireLicensePluginsChange();
            }
        }

        /// <summary>
        ///     Should image requests fail or be watermarked when license validation fails?
        /// </summary>
        public LicenseErrorAction LicenseError { get; set; } = LicenseErrorAction.Watermark;

        
        private ConcurrentDictionary<string, string> mappedDomains;

        /// <summary>
        ///     This allows you to map certain local hostnames (such as those without domains: 'localhost', 'localserver') and
        ///     *.local domains like 'webserver.local'.
        ///     Request with these host headers will appear as the licensed domain instead.
        /// </summary>
        /// <param name="localHostname"></param>
        /// <param name="licensedDomain"></param>
        public void AddLicensedDomainMapping(string localHostname, string licensedDomain)
        {
            if (mappedDomains == null) mappedDomains = new ConcurrentDictionary<string, string>();
            var from = localHostname?.Trim().ToLowerInvariant();
            var to = licensedDomain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                throw new ArgumentException("Both localHostname and licensedDomain must be non-null and non-empty.");
            mappedDomains[from] = to;
        }

        public IEnumerable<KeyValuePair<string, string>> GetLicensedDomainMappings()
        {
            return mappedDomains ??
                   Enumerable
                       .Empty<KeyValuePair<string,
                           string>>();
        }
    }

    /// <summary>
    ///     Sharing of license keys.
    /// </summary>
    [Flags()]
    public enum LicenseAccess
    {
        /// <summary>
        ///     Only use licenses added to the instance.
        /// </summary>
        Local = 0,

        /// <summary>
        ///     Reuse but don't share.
        /// </summary>
        ProcessReadonly = 1,

        /// <summary>
        ///     Share but don't reuse
        /// </summary>
        ProcessShareonly = 2,

        /// <summary>
        ///     Share and reuse licenses process-wide
        /// </summary>
        Process = 3
    }

    /// <summary>
    ///     How to notify the user that license validation has failed
    /// </summary>
    [Flags()]
    public enum LicenseErrorAction
    {
        /// <summary>
        ///     Adds a red dot to the bottom-right corner
        /// </summary>
        Watermark = 0,

        /// <summary>
        ///     Raises an exception with http status code
        /// 4
        /// 0
        /// 2
        /// </summary>
        Exception = 1
    }
}