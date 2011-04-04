using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public interface IIssueProvider {
        IEnumerable<IIssue> GetIssues();
    }
}
