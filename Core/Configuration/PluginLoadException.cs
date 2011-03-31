using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration {
    public class PluginLoadException : Exception {
        public PluginLoadException(string message) : base(message) { }
        public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
}
