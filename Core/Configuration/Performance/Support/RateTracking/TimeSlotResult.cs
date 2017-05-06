using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    struct TimeSlotResult
    {
        public TimeSlotResult(long result, long slotBeginTicks)
        {
            this.value = result;
            this.SlotBeginTicks = slotBeginTicks;
        }

        public long SlotBeginTicks { get; private set; }

        long value;
        public long? Value
        {
            get
            {
                return SlotBeginTicks > 0 ? value : (long?)null;
            }
        }

        public bool IsEmpty { get { return SlotBeginTicks == 0; } }

        /// <summary>
        /// Empty does not denote a result of any kind; this is like null. 
        /// </summary>
        public static readonly TimeSlotResult Empty = new TimeSlotResult();

        /// <summary>
        /// A zero result is a value of zero, not to be confused with Empty
        /// </summary>
        public static readonly TimeSlotResult ResultZero = new TimeSlotResult(0, 1);

        public override string ToString()
        {
            return IsEmpty ? "(empty)" : string.Format("[{0}] at {1}", value, SlotBeginTicks);
        }
    }
}
