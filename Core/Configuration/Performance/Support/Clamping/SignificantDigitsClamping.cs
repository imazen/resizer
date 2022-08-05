using System;

namespace ImageResizer.Configuration.Performance
{
    public class SignificantDigitsClamping
    {
        public long MinValue { get; set; } = 0;
        public long MaxValue { get; set; } = long.MaxValue;

        public int SignificantDigits { get; set; } = 2;

        public double RoundPositiveValueToDigits(double n, int count)
        {
            if (n == 0) return n;
            var mult = Math.Pow(10, count - Math.Floor(Math.Log(n) / Math.Log(10)) - 1);
            return Math.Round(Math.Round(n * mult) / mult);
        }

        public long Clamp(long value)
        {
            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            return (long)RoundPositiveValueToDigits(bounded, SignificantDigits);
        }
    }

    public class SignificantDigitsClampingFloat
    {
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = float.MaxValue;

        public int SignificantDigits { get; set; } = 2;

        public double RoundPositiveValueToDigits(double n, int count)
        {
            if (n == 0) return n;
            var mult = Math.Pow(10, count - Math.Floor(Math.Log(n) / Math.Log(10)) - 1);
            return Math.Round(Math.Round(n * mult) / mult);
        }

        public float Clamp(float value)
        {
            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            return (float)RoundPositiveValueToDigits(bounded, SignificantDigits);
        }
    }
}