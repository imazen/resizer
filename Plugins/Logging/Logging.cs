using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Logging;
using System.Collections.Specialized;
using NLog;
using NLog.Config;

namespace ImageResizer.Plugins.Logging {
    public class LoggingPlugin:ILogManager, IPlugin {

        public LoggingPlugin() {
        }

        public LoggingPlugin(NameValueCollection args) {
            if (!string.IsNullOrEmpty(args["configFile"]))
                LoadConfigFromFile(args["configFile"]); //TODO: resolve app-relative paths.
        }
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


        /// <summary>
        /// Creates the specified logger object and assigns a LoggerName to it.
        /// </summary>
        /// <param name="loggerName">The name of the logger.</param>
        /// <returns>The new logger instance.</returns>
        public ILogger GetLogger(string loggerName) {
            ILogger logger = new LoggerProxy{
                LoggerName = loggerName
            };

            return logger;
        }

        /// <summary>
        /// Loads NLog configuration from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to load NLog configuration from.</param>
        public void LoadConfigFromFile(string fileName) {
            LogManager.Configuration = new XmlLoggingConfiguration(fileName);
        }
    }
}
