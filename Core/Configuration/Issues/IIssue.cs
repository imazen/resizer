/* Copyright (c) 2014 Imazen See license.txt */
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
