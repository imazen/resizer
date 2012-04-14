using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Collections.Specialized;
using ImageResizer.Util;

namespace ImageResizer {
    public class UrlBuilder {

        private Config c;
        public UrlBuilder(Config c) {
            this.c = c;
        }
        public static UrlBuilder Current { get { return Config.Current.UrlBuilder; } }

        public string Url(string path, Instructions commands, UrlOptions urlOptions) {
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
