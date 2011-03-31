using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration.Issues {
    public interface IIssue {
        string Summary { get; }
        string Details { get; }
        int Importance { get; }
    }
}
