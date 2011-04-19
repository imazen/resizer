/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public interface IIssue {
        string Source { get; }
        string Summary { get; }
        string Details { get; }
        IssueSeverity Severity { get; }
    }
}
