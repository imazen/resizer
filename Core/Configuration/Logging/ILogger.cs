/* Copyright (c) 2014 Imazen See license.txt */

using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Logging {
    public interface ILogger {
        /// <summary>
        /// Writes the diagnostic message at the specified level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Log(string level, string message);

        /// <summary>
        /// Writes the diagnostic message at the Trace level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Trace(string message);
        void Trace(string message, params object[] args);

        /// <summary>
        /// Writes the diagnostic message at the Debug level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Debug(string message);
        void Debug(string message, params object[] args);

        /// <summary>
        /// Writes the diagnostic message at the Info level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Info(string message);
        void Info(string message, params object[] args);

        /// <summary>
        /// Writes the diagnostic message at the Warn level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Warn(string message);
        void Warn(string message, params object[] args);

        /// <summary>
        /// Writes the diagnostic message at the Error level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Error(string message);
        void Error(string message, params object[] args);

        /// <summary>
        /// Writes the diagnostic message at the Fatal level.
        /// </summary>
        /// <param name="message">A <see langword="string" /> to be written.</param>
        void Fatal(string message);
        void Fatal(string message, params object[] args);

        /// <summary>
        /// Checks if the specified log level is enabled.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A value indicating whether the specified log level is enabled.</returns>
        bool IsEnabled(string level);

        /// <summary>
        /// Gets a value indicating whether the Trace level is enabled.
        /// </summary>
        bool IsTraceEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Debug level is enabled.
        /// </summary>
       
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Info level is enabled.
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Warn level is enabled.
        /// </summary>
        bool IsWarnEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Error level is enabled.
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Fatal level is enabled.
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Gets or sets the logger name.
        /// </summary>
        string LoggerName { get; set; }
    }
}
