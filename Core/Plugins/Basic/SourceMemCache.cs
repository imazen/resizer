using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins.Basic {
    public class SourceMemCache: IPlugin, IVirtualFileCache {

        public IVirtualFile GetFileIfCached(string virtualPath, System.Collections.Specialized.NameValueCollection queryString, IVirtualFile original) {
            //If cached, serve it. 
            //If not cached, check for memcache=true
            //if set, clear items from the cache until enough space is reclaimed
            //cache file
            //return file
            return null;
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


    }

    public class CachedVirtualFile : IVirtualFile {

        public CachedVirtualFile(string virtualPath, byte[] data) {
            this.virtualPath = virtualPath;
            this.data = data;
        }
        string virtualPath;
        byte[] data;
        public string VirtualPath {
            get { return virtualPath; }
        }

        public System.IO.Stream Open() {
            return new MemoryStream(data, false);
        }
    }
}
