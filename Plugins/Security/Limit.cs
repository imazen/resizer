using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.RequestLimiting {
    public enum Scope {
        IPAddress,
        Session,
        All
    }
    public class Limit {
        TimeSpan period;
        Scope scope;
        long maxRequests;

    }
}
