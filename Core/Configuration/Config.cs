using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Encoding;
using fbs.ImageResizer.Resizing;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Plugins;

namespace fbs.ImageResizer.Configuration {
    public class Config : IEncoderProvider{

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

        //Generic read/write configuration access

        

        protected void Init() {
            imageBuilderExtensions = new SafeList<ImageBuilderExtension>();
            imageEncoders = new SafeList<IImageEncoder>();

            cachingSystems = new SafeList<ICache>();
            urlModifyingPlugins = new SafeList<IUrlPlugin>();
            allPlugins = new SafeList<IPlugin>();


            _imageBuilder = new ImageBuilder();
            imageBuilderExtensions.Changed += new SafeList<ImageBuilderExtension>.ChangedHandler(imageBuilderExtensions_Changed);
            imageEncoders.Changed += new SafeList<IImageEncoder>.ChangedHandler(imageEncoders_Changed);


            urlModifyingPlugins.Changed += new SafeList<IUrlPlugin>.ChangedHandler(urlModifyingPlugins_Changed);

        }

        void urlModifyingPlugins_Changed(SafeList<IUrlPlugin> sender) {
            InvalidateUrlData();
        }

        void imageEncoders_Changed(SafeList<IImageEncoder> sender) {
            InvalidateImageBuilder(); //Why? Nothing has changed
        }

        void imageBuilderExtensions_Changed(SafeList<ImageBuilderExtension> sender) {
            InvalidateImageBuilder();
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

        protected volatile List<string> _supportedDirectives = null;
        protected volatile List<string> _supportedExtensions = null;
       

        protected object _cachedUrlDataSync = new object();
        protected void InvalidateUrlData() {
            lock (_cachedUrlDataSync) {
                _supportedDirectives = null;
                _supportedExtensions = null;
            }
        }
        protected void CacheUrlData() {
            lock (_cachedUrlDataSync) {
                List<string> directives = new List<string>(24);
                List<string> exts = new List<string>(24);
                foreach (IUrlPlugin p in UrlModifyingPlugins) {
                    exts.AddRange(p.GetSupportedFileExtensions());
                    directives.AddRange(p.GetSupportedQuerystringKeys());
                }
                directives.Sort(StringComparer.OrdinalIgnoreCase);
                exts.Sort(StringComparer.OrdinalIgnoreCase);
                _supportedDirectives = directives;
                _supportedExtensions = exts;
            }
        }
        
        

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
        public IImageEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettingsCollection settings) {
           return new DefaultEncoder().CreateIfSuitable(originalImage, settings);
        }



        //URL rewriting hooks
        //header rewrite hooks
        //cache control?

        //collection: accepted image extensions
        //collection: accpeted querystring arguments


        //IPlugin collection, keyed by shortname

    }
}
