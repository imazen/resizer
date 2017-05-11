using ImageResizer.Plugins.Licensing;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.LicenseVerifier
{
    class RealClock : ILicenseClock
    {
        public long TicksPerSecond { get; } = Stopwatch.Frequency;
        public long GetTimestampTicks()
        {
            return Stopwatch.GetTimestamp();
        }
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
        public DateTimeOffset? GetBuildDate()
        {
            try
            {
                return this.GetType().Assembly.GetCustomAttributes(typeof(BuildDateAttribute), false)?.Select(a => ((BuildDateAttribute)a).ValueDate).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
        public DateTimeOffset? GetAssemblyWriteDate()
        {
            var path = this.GetType().Assembly.Location;
            try
            {
                return System.IO.File.Exists(path) ? new DateTimeOffset?(System.IO.File.GetLastWriteTimeUtc(this.GetType().Assembly.Location)) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
