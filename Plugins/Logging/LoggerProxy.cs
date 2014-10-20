using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Logging;
using NLog;

namespace ImageResizer.Plugins.Logging {
    public class LoggerProxy : ILogger {
        private static readonly Logger DefaultLogger = LogManager.CreateNullLogger();

        private Logger logger = DefaultLogger;
        private string loggerName = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the Trace level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsTraceEnabled {
            get { return this.logger.IsTraceEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the Debug level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsDebugEnabled {
            get { return this.logger.IsDebugEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the Info level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsInfoEnabled {
            get { return this.logger.IsInfoEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the Warn level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsWarnEnabled {
            get { return this.logger.IsWarnEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the Error level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsErrorEnabled {
            get { return this.logger.IsErrorEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the Fatal level is enabled.
        /// </summary>
        /// <value></value>
        public bool IsFatalEnabled {
            get { return this.logger.IsFatalEnabled; }
        }

        /// <summary>
        /// Gets or sets the logger name.
        /// </summary>
        /// <value></value>
        public string LoggerName {
            get {
                return this.loggerName;
            }

            set {
                this.loggerName = value;
                this.logger = LogManager.GetLogger(value);
            }
        }

        /// <summary>
        /// Writes the diagnostic message at the specified level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Log(string level, string message) {
            this.logger.Log(LogLevel.FromString(level), message);
        }

        /// <summary>
        /// Writes the diagnostic message at the Trace level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Trace(string message) {
            this.logger.Trace(message);
        }

        public void Trace(string message, params object[] args) {
            this.logger.Trace(message,args);
        }

        /// <summary>
        /// Writes the diagnostic message at the Debug level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Debug(string message) {
            this.logger.Debug(message);
        }

        public void Debug(string message, params object[] args) {
            this.logger.Debug(message,args);
        }

        /// <summary>
        /// Writes the diagnostic message at the Info level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Info(string message) {
            this.logger.Info(message);
        }

        public void Info(string message,params object[] args) {
            this.logger.Info(message,args);
        }

        /// <summary>
        /// Writes the diagnostic message at the Warn level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Warn(string message) {
            this.logger.Warn(message);
        }

        public void Warn(string message, params object[] args) {
            this.logger.Warn(message,args);
        }

        /// <summary>
        /// Writes the diagnostic message at the Error level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Error(string message) {
            this.logger.Error(message);
        }

        public void Error(string message, params object[] args) {
            this.logger.Error(message,args);
        }

        /// <summary>
        /// Writes the diagnostic message at the Fatal level.
        /// </summary>
        /// <param name="message">A <see langword="string"/> to be written.</param>
        public void Fatal(string message) {
            this.logger.Fatal(message);
        }

        public void Fatal(string message, params object[] args) {
            this.logger.Fatal(message,args);
        }

        /// <summary>
        /// Checks if the specified log level is enabled.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>
        /// A value indicating whether the specified log level is enabled.
        /// </returns>
        public bool IsEnabled(string level) {
            return this.logger.IsEnabled(LogLevel.FromString(level));
        }
    }
}