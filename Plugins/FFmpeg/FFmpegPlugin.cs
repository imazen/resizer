using ImageResizer.Configuration;
using ImageResizer.Resizing;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.FFmpeg
{
    public class FFmpegPlugin : IVirtualImageProvider, IPlugin, IFileExtensionPlugin, IQuerystringPlugin

    {

        Config c;
        public IPlugin Install(Config c)
        {
            this.c = c;
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
        private FFmpegManager mgr = new FFmpegManager();

        /// <summary>
        /// Additional file types this plugin adds support for decoding.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedFileExtensions()
        {
            return new string[] {"avi", "mp4","avchd","flv","fla","swf","mpg","mpeg","mpe","mov","m4v","mkv","wmv" };
        }

        /// <summary>
        /// Returns true if the specified file and querystring indicate a PSD composition request
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            return IsPathVideoFileWithOurCommands(virtualPath, queryString) && c.Pipeline.FileExists(virtualPath, new NameValueCollection());
        }

        /// <summary>
        /// Returns a virtual file instance for the specified specified file and querystring, if they indicate a PSD composition request. 
        /// Otherwise, null is returned.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (IsPathVideoFileWithOurCommands(virtualPath, queryString) && c.Pipeline.FileExists(virtualPath, new NameValueCollection()))
                return new FFmpegVirtualFile(virtualPath, queryString, this);
            else
                return null;
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new string[] {"ffmpeg.seconds" ,"ffmpeg.percent","ffmpeg.skipblankframes" };
        }

 

        /// <summary>
        /// True if the file is a .psd.jpeg, .psd.png, etc file.
        /// </summary>
        protected bool IsPathVideoFileWithOurCommands(string virtualPath, NameValueCollection queryString = null)
        {
            var exts = GetSupportedFileExtensions();

            var full = ImageResizer.Util.PathUtils.GetFullExtension(virtualPath).ToLowerInvariant().TrimStart('.');
            var parts = full.Split('.');
            if (parts.Length < 1) return false;
            //Only accept the file if our querystring is on it.
            if (exts.Contains(parts[parts.Length - 1]))
            {
                if (queryString == null) queryString = c.Pipeline.ModifiedQueryString;
                foreach (string s in GetSupportedQuerystringKeys())
                {
                    if (!string.IsNullOrEmpty(queryString[s])) return true;
                }
            }
            return false;
        }


        public Stream GetFrameStream(string virtualPath, NameValueCollection queryString)
        {
           return mgr.GetFrameStream(c,virtualPath, queryString);
        }

    }


    public class FFmpegVirtualFile : IVirtualFile
    {

        private FFmpegPlugin provider;

        private Nullable<bool> _exists = null;
        //private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Returns true if the row exists. 
        /// </summary>
        public bool Exists
        {
            get
            {
                if (_exists == null) _exists = provider.FileExists(this.VirtualPath, this.Query);
                return _exists.Value;
            }
        }

        public FFmpegVirtualFile(string virtualPath, NameValueCollection query, FFmpegPlugin provider)
        {
            this.provider = provider;
            this._virtualPath = virtualPath;
            this._query = query;

        }

        private string _virtualPath = null;

        public string VirtualPath
        {
            get { return _virtualPath; }
        }
        private NameValueCollection _query;

        public NameValueCollection Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Returns a stream of the encoded file bitmap using the current request querystring.
        /// </summary>
        /// <returns></returns>
        public Stream Open() { return provider.GetFrameStream(this.VirtualPath, this.Query); }

    }
}
