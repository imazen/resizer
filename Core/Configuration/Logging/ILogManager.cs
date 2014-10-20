/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Logging {
    public interface ILogManager {
        /// <summary>
        /// Loads NLog configuration from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to load NLog configuration from.</param>
        void LoadConfigFromFile(string fileName);

        /// <summary>
        /// Creates the specified logger object and assigns a LoggerName to it.
        /// </summary>
        /// <param name="loggerName">Logger name.</param>
        /// <returns>The new logger instance.</returns>
        ILogger GetLogger(string loggerName);
    }
}
