using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Util;
using System.Globalization;

namespace ImageResizer.Plugins.Watermark {
    /// <summary>
    /// Represents either a number of pixels or a percentage
    /// </summary>
    public class DistanceUnit {
        /// <summary>
        /// Creates DistanceUnit instance based on provided floating point value and type of unit
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public DistanceUnit(double value, Units type) {
            this.Type = type;
            this.Value = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public DistanceUnit(string value) {
            DistanceUnit u = TryParse(value);
            if (u == null) throw new ArgumentException("The specified value \"" + value + "\" could not be parsed.");
            this.Type = u.Type;
            this.Value = u.Value;

        }
        /// <summary>
        ///  var a = new DistanceUnit();
        ///  a.TryParse() 
        ///  DistanceUnit.TryParse("3px") 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DistanceUnit TryParse(string value) {
            if (string.IsNullOrEmpty(value)) return null;
            double val = 0;
            Units type = Units.Pixels;
            if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) {
                value = value.Substring(0, value.Length - 2); type = Units.Pixels;
            } else if (value.EndsWith("%")) {
                value = value.Substring(0, value.Length - 1); type = Units.Percentage;
            } else if (value.EndsWith("percent", StringComparison.OrdinalIgnoreCase)) {
                value = value.Substring(0, value.Length - 7); type = Units.Percentage;
            } else if (value.EndsWith("pct", StringComparison.OrdinalIgnoreCase)) {
                value = value.Substring(0, value.Length - 3); type = Units.Percentage;
            }

            if (!double.TryParse(value, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out val)) return null;
            return new DistanceUnit(val, type);
        }



        public enum Units { Pixels, Percentage }

        protected Units _type = Units.Pixels;
        public Units Type { get { return _type; } set { _type = value; } }

        protected double _value = 0;
        /// <summary>
        /// A number of pixels, or a percentage value between 0 and 100
        /// </summary>
        public double Value { get { return _value; } set { _value = value; } }

        public override string ToString() {
            if (Type == Units.Pixels) return Value.ToString(NumberFormatInfo.InvariantInfo) + "px";
            else return Value.ToString(NumberFormatInfo.InvariantInfo) + "percent";

        }
    }
}
