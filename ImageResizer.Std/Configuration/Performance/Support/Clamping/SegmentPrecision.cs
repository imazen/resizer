using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    struct SegmentPrecision
    {
        /// <summary>
        /// Inclusive (microseconds, 1 millionth of a second)
        /// </summary>
        public long Above { get; set; }
        public long Loss { get; set; }
    }

}
