using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Encoding;
using fbs.ImageResizer.Resizing;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Plugins;
using System.Configuration;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Configuration {
    public class Config : IEncoderProvider, ICacheProvider{

        #region Singleton code, .Current,
        private static Config _singleton = null;
        private static object _singletonLock = new object();
        /// <summary>
        /// Gets the current config instance. 
        /// </summary>
        /// <returns></returns>
        public static Config Current {
            get {
                if (_singleton == null)
                    lock (_singletonLock)
                        if (_singleton == null)
                            _singleton = new Config();

                return _singleton;
            }
        }
        #endregion
        public Config() {
            imageBuilderExtensions = new SafeList<ImageBuilderExtension>();
            imageEncoders = new SafeList<IImageEncoder>();

            cachingSystems = new SafeList<ICache>();
            urlModifyingPlugins = new SafeList<IUrlPlugin>();
            allPlugins = new SafeList<IPlugin>();


            //Whenever the extensions change, the image builder instance has to be replaced.
            imageBuilderExtensions.Changed += new SafeList<ImageBuilderExtension>.ChangedHandler(delegate(SafeList<ImageBuilderExtension> s) {
                InvalidateImageBuilder();
            });
            imageEncoders.Changed += new SafeList<IImageEncoder>.ChangedHandler(delegate(SafeList<IImageEncoder> s) {
                //InvalidateImageBuilder();
            });
        }

        private IPipelineConfig pipeline = null;
        /// <summary>
        /// Access and modify settings related to the HttpModule pipline. Register URL rewriting hooks, etc.
        /// </summary>
        public IPipelineConfig Pipeline {
            get { return pipeline; }
            set { pipeline = value; }
        }

        #region ImageBuilder singleton code .CurrentImageBuilder .UpgradeImageBuilder .InvalidateImageBuilder
        protected volatile ImageBuilder _imageBuilder = null;
        protected volatile object _imageBuilderSync = new object();
        /// <summary>
        /// Allows subclasses to be used instead of ImageBuilder. Replacements must override the Create method and call their own constructor instead.
        /// </summary>
        /// <param name="replacement"></param>
        public void UpgradeImageBuilder(ImageBuilder replacement) {
            lock (_imageBuilderSync) _imageBuilder = replacement.Create(imageBuilderExtensions, this);
        }

        /// <summary>
        /// Returns a shared instance of ImageManager, (or a subclass if it has been upgraded).
        /// Instances change whenever ImageBuilderExtensions change.
        /// </summary>
        /// <returns></returns>
        public ImageBuilder CurrentImageBuilder {
            get {
                if (_imageBuilder == null)
                    lock (_imageBuilderSync)
                        if (_imageBuilder == null)
                            _imageBuilder = new ImageBuilder(imageBuilderExtensions,this);

                return _imageBuilder;
            }
        }


        protected void InvalidateImageBuilder() {
            lock (_imageBuilderSync) _imageBuilder = _imageBuilder.Create(imageBuilderExtensions, this);
        }
        #endregion



        protected SafeList<ImageBuilderExtension> imageBuilderExtensions = null;
        /// <summary>
        /// Currently registered set of ImageBuilderExtensions. 
        /// </summary>
        public SafeList<ImageBuilderExtension> ImageBuilderExtensions {get { return imageBuilderExtensions; } }

        protected SafeList<IImageEncoder> imageEncoders = null;
        /// <summary>
        /// Currently registered IImageEncoders. 
        /// </summary>
        public SafeList<IImageEncoder> ImageEncoders {get { return imageEncoders; }}

        protected SafeList<ICache> cachingSystems = null;
        /// <summary>
        /// Currently registered ICache instances
        /// </summary>
        public SafeList<ICache> CachingSystems { get { return cachingSystems; }}

        protected SafeList<IUrlPlugin> urlModifyingPlugins = null;
        /// <summary>
        /// Plugins which accept new querystring arguments or new file extensions are registered here.
        /// </summary>
        public SafeList<IUrlPlugin> UrlModifyingPlugins { get { return urlModifyingPlugins; } }

        protected SafeList<IPlugin> allPlugins = null;
        /// <summary>
        /// All plugins should be registered here. Used for diagnostic purposes.
        /// </summary>
        public SafeList<IPlugin> AllPlugins { get { return allPlugins;}}


        
        //CacheSelector event

        /// <summary>
        /// Returns an instance of the first encoder that claims to be able to handle the specified settings.
        /// The most recently registered encoder is queried first.
        /// </summary>
        /// <param name="originalImage"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IImageEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettings settings) {
           return new DefaultEncoder().CreateIfSuitable(originalImage, settings);
        }




        /// <summary>
        /// Removes the specified plugin from AllPlugins, UrlModifyingPlugins, CachingSystems, ImageEncoders, and ImageBuiderExtensions, based
        /// on which interfaces the instance implements.
        /// Plugins may register event handlers and modify settings - thus you should use the plugin's method to uninstall them vs. using this method.
        /// </summary>
        /// <param name="plugin"></param>
        public void RemovePlugin(IPlugin plugin) {
            AllPlugins.Remove(plugin);
            if (plugin is IUrlPlugin) UrlModifyingPlugins.Remove(plugin as IUrlPlugin);
            if (plugin is ICache) CachingSystems.Remove(plugin as ICache);
            if (plugin is IImageEncoder) ImageEncoders.Remove(plugin as IImageEncoder);
            if (plugin is ImageBuilderExtension) ImageBuilderExtensions.Remove(plugin as ImageBuilderExtension);
        }






        private volatile ResizerConfigurationSection configuration;
        private object configurationLock = new object();
        /// <summary>
        /// The ResizeConfigrationSection is not thread safe, and should not be modified
        /// Dynamically loads the ConfigurationSection from web.config when accessed for the first time. 
        /// </summary>
        protected ResizerConfigurationSection cs {
            get {
                if (configuration == null) {
                    lock (configurationLock) {
                        if (configuration == null) {
                            ResizerConfigurationSection tmpConf = (ResizerConfigurationSection)System.Configuration.ConfigurationManager.GetSection("imageresizer");
                            configuration = tmpConf;
                        }
                    }
                }
                return configuration;
            }
        }



        public string get(string selector, string defaultValue) {
            return cs.get(selector, defaultValue);
        }
        public int get(string selector, int defaultValue) {
            int i;
            string s = cs.get(selector, defaultValue.ToString());
            if (int.TryParse(s, out i)) return i;
            else throw new ConfigurationException("Error in imageresizer configuration section: Invalid integer at " + selector + ":" + s);
        }

        public bool get(string selector, bool defaultValue) {
            string s = cs.get(selector, defaultValue.ToString());

            if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
            else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false;
            else throw new ConfigurationException("Error in imageresizer configuration section: Invalid boolean at " + selector + ":" + s);
        }

        public ICache GetCachingSystem(System.Web.HttpContext context, IResponseArgs args) {
            throw new NotImplementedException();
        }
    }
}
