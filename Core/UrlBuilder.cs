using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Collections.Specialized;
using ImageResizer.Util;

namespace ImageResizer {
    public class UrlBuilder {

        private Dictionary<string, UrlOptions> configs;
        private object syncLock = new object();

        private Config c;
        public UrlBuilder(Config c) {
            this.c = c;
            this.configs = ParseFrom(c);
        }

        private Dictionary<string, UrlOptions> ParseFrom(Config c) {
        }

        public UrlBuilder(IDictionary<string, UrlOptions> configurations) {
            this.configs = new Dictionary<string, UrlOptions>(configurations, StringComparer.OrdinalIgnoreCase);
        }

        public static UrlBuilder Current { get { return Config.Current.UrlBuilder; } }

        /// <summary>
        /// Generates a url using the default url options for the current site
        /// </summary>
        /// <param name="path"></param>
        /// <param name="commands"></param>
        /// <param name="urlOptions"></param>
        /// <returns></returns>
        public string Default(string path, Instructions commands = null, UrlOptions urlOptions = null) {
            return Url(null, path, commands, urlOptions);
        }
        /// <summary>
        /// Generates a url using a named set of url options 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="path"></param>
        /// <param name="commands"></param>
        /// <param name="urlOptions"></param>
        /// <returns></returns>
        public string Url(string config, string path, Instructions commands = null, UrlOptions urlOptions = null) {
            //Allow null url options and commands
            if (urlOptions == null) urlOptions = new UrlOptions();
            if (commands == null) commands = new Instructions(); 

            var pre = (List<IUrlPreFilter>)c.Plugins.GetAll<IUrlPreFilter>();
            pre.Sort(delegate(IUrlPreFilter a, IUrlPreFilter b){
                return a.PreFilterOrderHint.CompareTo(b.PreFilterOrderHint); 
            });
            NameValueCollection q = commands;

            foreach (var f in pre) f.PreFilterUrl(ref path, ref q, ref urlOptions);

            //Join query to path
            if (urlOptions.Semicolons)
                path += PathUtils.BuildSemicolonQueryString(q, true);
            else
                path += PathUtils.BuildQueryString(q, true);
            
            var post = (List<IUrlPostFilter>)c.Plugins.GetAll<IUrlPostFilter>();
            post.Sort(delegate(IUrlPostFilter a, IUrlPostFilter b) {
                return a.PostFilterOrderHint.CompareTo(b.PostFilterOrderHint);
            });

            foreach (var f in post) f.PostFilterUrl(ref path, ref urlOptions);

            return path;
        }


    }
}
