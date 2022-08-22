// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

namespace ImageResizer.Configuration.Logging
{
    public interface ILogManager
    {
        /// <summary>
        ///     Loads NLog configuration from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to load NLog configuration from.</param>
        void LoadConfigFromFile(string fileName);

        /// <summary>
        ///     Creates the specified logger object and assigns a LoggerName to it.
        /// </summary>
        /// <param name="loggerName">Logger name.</param>
        /// <returns>The new logger instance.</returns>
        ILogger GetLogger(string loggerName);
    }
}