using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration.Issues {
    public interface IIssueReceiver {
        void AcceptIssue(IIssue i);
    }
}
