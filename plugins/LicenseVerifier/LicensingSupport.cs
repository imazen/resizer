using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageResizer.Plugins.Licensing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.LicenseVerifier
{
    class RealClock : ILicenseClock
    {
        public long TicksPerSecond { get; } = Stopwatch.Frequency;

        public long GetTimestampTicks() => Stopwatch.GetTimestamp();

        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        public DateTimeOffset? GetBuildDate()
        {
            try {
                return GetType()
                    .Assembly.GetCustomAttributes(typeof(BuildDateAttribute), false)
                    .Select(a => ((BuildDateAttribute) a).ValueDate)
                    .FirstOrDefault();
            } catch {
                return null;
            }
        }

        public DateTimeOffset? GetAssemblyWriteDate()
        {
            var path = GetType().Assembly.Location;
            try {
                return path != null && File.Exists(path)
                    ? new DateTimeOffset?(File.GetLastWriteTimeUtc(path))
                    : null;
            } catch {
                return null;
            }
        }
    }
}
