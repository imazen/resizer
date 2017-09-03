using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    /// <summary>
    /// Min/Max/Avg values for an interval
    /// </summary>
    struct IntervalStat
    {
        public NamedInterval Interval { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Avg { get; set; }
    }
}