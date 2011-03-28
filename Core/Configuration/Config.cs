using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Encoding;

namespace fbs.ImageResizer.Configuration {
    public class Config {
        #region Singleton code, .Current, .Replace
        private static Config _bestInstance = null;
        private static object _bestInstanceSync = new object();
        /// <summary>
        /// Allows subclasses to be used instead of Config
        /// </summary>
        /// <param name="replacement"></param>
        public static void Replace(Config replacement) {
            lock (_bestInstanceSync) _bestInstance = replacement;
        }

        /// <summary>
        /// Returns a shared instance of ImageManager, or a subclass if it has been upgraded
        /// </summary>
        /// <returns></returns>
        public static Config Current {
            get {
                if (_bestInstance == null)
                    lock (_bestInstanceSync)
                        if (_bestInstance == null)
                            _bestInstance = new Config();

                return _bestInstance;
            }
        }
        #endregion

        //Generic read/write configuration access

        //ImageBuilderExtension collection
        //ImageBuilder instance??

        //IImageEncoder collection
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


        //ICache collection
        //CacheSelector event

        //URL rewriting hooks
        //header rewrite hooks
        //cache control?

        //collection: accepted image extensions
        //collection: accpeted querystring arguments


        //IPlugin collection, keyed by shortname

    }
}
