/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public interface IIssueProvider {
        IEnumerable<IIssue> GetIssues();
    }
}
