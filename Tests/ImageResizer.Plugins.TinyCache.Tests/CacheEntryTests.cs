using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ImageResizer.Plugins.TinyCache;

namespace ImageResizer.Plugins.TinyCache.Tests
{
    public class CacheEntryTests
    {

        [Fact]
        public void TestCacheEntryWithNoRecentUse()
        {
            var e = new CacheEntry();
            e.recent_reads = new Queue<DateTime>(); //There used to be an InvalidOperationException when this was empty.
            e.loaded = DateTime.UtcNow;
            e.sizeInBytes = 100 * 1000;
            e.written = DateTime.UtcNow;
            e.read_count = 0;
            e.recreated_count = 0;
            e.cost_ms = 100;
            Assert.InRange(e.GetPreservationPriority(), 0.6337f, 0.6338f);
        }
    }
}
