using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Permits plugins to redact data from the diagnostics page, like passwords
    /// </summary>
    public interface IRedactDiagnostics {
         Node RedactFrom(Node resizer);
    }
}
