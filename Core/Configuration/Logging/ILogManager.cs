/* Copyright (c) 2011 Nathanael Jones. See license.txt */
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
        /// Gets or sets a value indicating whether internal messages should be written to the console.
        /// </summary>
        bool InternalLogToConsole { get; set; }

        /// <summary>
        /// Gets or sets the name of the internal log file.
        /// </summary>
        string InternalLogFile { get; set; }

        /// <summary>
        /// Gets or sets the name of the internal log level.
        /// </summary>
        string InternalLogLevel { get; set; }

        /// <summary>
        /// Creates the specified logger object and assigns a LoggerName to it.
        /// </summary>
        /// <param name="loggerName">Logger name.</param>
        /// <returns>The new logger instance.</returns>
        ILogger GetLogger(string loggerName);
    }
}
