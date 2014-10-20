/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Logging;

namespace ImageResizer.Plugins.DiskCache {
    /// <summary>
    /// Handles writing a Web.Config to disk that uses Url Authorization to prevent visitors 
    /// from accessing the files directly. Alternative Web.config content can be specified, this is a general-purpose implementation. Uses UTF-8 encoding.
    /// Also provides methods for efficient verification that the file still exists.
    /// Thread-safe.
    /// </summary>
    public class WebConfigWriter {

        public WebConfigWriter(ILoggerProvider lp, string physicalDirPath)
            : this(lp,physicalDirPath, null) {
        }
        public WebConfigWriter(ILoggerProvider lp, string physicalDirPath, string alternateWebConfigContents) {
            this.physicalDirPath = physicalDirPath;
            this.lp = lp;
            if (alternateWebConfigContents != null) this.webConfigContents = alternateWebConfigContents;
        }
        protected ILoggerProvider lp = null;
        protected string physicalDirPath = null;

        /// <summary>
        /// This string contains the contents of a web.conig file that sets URL authorization to "deny all" inside the current directory.
        /// </summary>
        protected string webConfigContents =
            "<?xml version=\"1.0\"?>" +
            "<configuration xmlns=\"http://schemas.microsoft.com/.NetConfiguration/v2.0\">" +
            "<system.web><authorization>" +
            "<deny users=\"*\" />" +
            "</authorization></system.web></configuration>";

        protected virtual string getNewWebConfigContents() {
            return webConfigContents;
        }


        private readonly object _webConfigSyncObj = new object();

        private DateTime _lastCheckedWebConfig = DateTime.MinValue;
        private volatile bool _checkedWebConfigOnce = false;
        /// <summary>
        /// Verifies a Web.config file is present in the directory every 5 minutes that the function is called, including the first time.
        /// </summary>
        /// <returns></returns>
        public void CheckWebConfigEvery5() {
            if (_lastCheckedWebConfig < DateTime.UtcNow.Subtract(new TimeSpan(0, 5, 0))) {
                lock (_webConfigSyncObj) {
                    if (_lastCheckedWebConfig < DateTime.UtcNow.Subtract(new TimeSpan(0, 5, 0)))
                        _checkWebConfig();
                }
            }
        }


        /// <summary>
        /// If CheckWebConfig has never executed, it is executed immediately, but only once. 
        /// Verifies a Web.config file is present in the directory, and creates it if needed.
        /// </summary>
        public void CheckWebConfigOnce() {
            if (_checkedWebConfigOnce) return;
            lock (_webConfigSyncObj) {
                if (_checkedWebConfigOnce) return;
                _checkWebConfig();
            }
        }
        /// <summary>
        /// Verifies a Web.config file is present in the directory, and creates it if needed.
        /// </summary>
        public void CheckWebConfig() {
            lock (_webConfigSyncObj) {
                _checkWebConfig();
            }
        }
        /// <summary>
        /// Should only be called inside a lock. Creates the cache dir and the web.config file if they are missing. Updates
        /// _lastCheckedWebConfig and _checkedWebConfigOnce
        /// </summary>
        protected void _checkWebConfig() {
            try {
                string webConfigPath = physicalDirPath.TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar + "Web.config";
                if (lp.Logger != null) lp.Logger.Debug("Verifying Web.config exists in cache directory.");
                if (System.IO.File.Exists(webConfigPath)) return; //Already exists, quit


                //Web.config doesn't exist? make sure the directory exists!
                if (!System.IO.Directory.Exists(physicalDirPath))
                    System.IO.Directory.CreateDirectory(physicalDirPath);

                if (lp.Logger != null) lp.Logger.Info("Creating missing Web.config in cache directory.");
                //Create the Web.config file
                System.IO.File.WriteAllText(webConfigPath, getNewWebConfigContents(),UTF8Encoding.UTF8);

            } finally {
                _lastCheckedWebConfig = DateTime.UtcNow;
                _checkedWebConfigOnce = true;
            }
        }


    }
}
