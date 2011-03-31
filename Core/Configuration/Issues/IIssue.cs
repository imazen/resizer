using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration.Issues {
    public interface IIssue {
        string Source { get; }
        string Summary { get; }
        string Details { get; }
        IssueSeverity Severity { get; }
    }
}
