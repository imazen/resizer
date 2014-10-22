using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class Stat<T> where T : struct, IConvertible
    {
        public Stat(T min, T max, T sum, long count, string units, string formatString)
        {
            Min = min; Max = max; Sum = sum; Count = count; Units = units; FormatString = formatString;
        }
        public T Min { get; private set; } 
        public T Max { get; private set; }
        public T Sum { get; private set; }
        public T Avg { get { return (dynamic)Sum / (dynamic)Count; } }
        public T Delta { get { return (dynamic)Max - (dynamic)Min; } }

        public T DeltaRatio { get { return (dynamic)Delta / (dynamic)Min; } }

        /// <summary>
        /// A suffix to use when foratting the output (like ms or %)
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// The format string to use (like F or F1 or F2)
        /// </summary>
        public string FormatString { get; private set; }

        public long Count { get; private set; }

        public Stat<U> ConvertTo<U>(Func<T, U> convert, string newUnits, string newFormatString) where U : struct, IConvertible
        {
            return new Stat<U>(convert(Min), convert(Max), convert(Sum), Count, newUnits, newFormatString);
        }
        public Stat<U> ConvertTo<U>(Func<T, U> convert) where U : struct, IConvertible
        {
            return new Stat<U>(convert(Min), convert(Max), convert(Sum), Count, Units, FormatString);
        }

        public Stat<T> Combine(Stat<T> other)
        {
            if (other.Count == 0) return this;
            if (this.Count == 0) return other;
            return new Stat<T>(Math.Min((dynamic)Min, (dynamic)other.Min), Math.Max((dynamic)Max, (dynamic)other.Max), (dynamic)Max + (dynamic)other.Max, Count + other.Count, Units, FormatString);
        }

        /// <summary>
        /// [min][units] or [min] .. [max][units] depending on whether the delta percent exceeds 8%
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString((T)Convert.ChangeType(0.08,typeof(T)));
        }

        /// <summary>
        /// If the delta ratio is less than X (0..1), displays [min][units], otherwise [min] .. [max][units]
        /// </summary>
        /// <param name="DeltaSignificantPercent"></param>
        /// <returns></returns>
        public virtual string ToString(T DeltaSignificantRatio)
        {
            return (dynamic)DeltaRatio > (dynamic)DeltaSignificantRatio ? string.Format("{0:" + FormatString + "} .. {1:" + FormatString + "}{2}", Min, Max, Units) : string.Format("{0:" + FormatString + "}{1}", Min, Units);
        }

    }

}
