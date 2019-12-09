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
            Value = result;
            SlotBeginTicks = slotBeginTicks;
        }


        public long SlotBeginTicks { get; }
        public long Value { get; }

            /// <summary>
        /// A zero result is a value of zero, not to be confused with Empty
        /// </summary>
        public static readonly TimeSlotResult ResultZero = new TimeSlotResult(0, 1);

        public override string ToString() =>  $"[{Value}] at {SlotBeginTicks}";
    }
}
