using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public interface IIssueReceiver {
        void AcceptIssue(IIssue i);
    }
}
