using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ImageResizer.Util;


namespace ImageResizer.Core.Tests
{
  
    public class SeekableStreamWrapperTests
    {
        [Fact]
        public void DoesntWrapSeekableStream()
        {
            var s = new TestStream(true, "some data");
            using (var actual = SeekableStreamWrapper.FromStream(s))
            {
                Assert.Same(s, actual);
                Assert.True(actual.CanSeek);
            }
        }

        [Fact]
        public void WrapsNonSeekableStream()
        {
            var s = new TestStream(false, "some data");
            using (var actual = SeekableStreamWrapper.FromStream(s))
            {
                Assert.NotSame(s, actual);
                Assert.IsAssignableFrom(typeof(SeekableStreamWrapper), actual);
                Assert.True(actual.CanSeek);
                Assert.False(actual.CanWrite);
            }
        }

        private class TestStream : MemoryStream
        {
            private bool seekable;
            public TestStream(bool seekable, string data)
                : base(UTF8Encoding.UTF8.GetBytes(data), false)
            {
                this.seekable = seekable;
            }

            public override bool CanSeek { get { return this.seekable; } }
        }
    }
}
