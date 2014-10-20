using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Logging {
    public interface ILoggerProvider {
        ILogger Logger { get; }
    }
}
