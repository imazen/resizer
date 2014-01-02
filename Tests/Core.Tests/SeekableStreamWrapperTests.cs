using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gallio.Framework;
using ImageResizer.Util;
using MbUnit.Framework;

namespace ImageResizer.Core.Tests
{
    [TestFixture]
    public class SeekableStreamWrapperTests
    {
        [Test]
        public void DoesntWrapSeekableStream()
        {
            var s = new TestStream(true, "some data");
            using (var actual = SeekableStreamWrapper.FromStream(s))
            {
                Assert.AreSame(s, actual);
                Assert.IsTrue(actual.CanSeek);
            }
        }

        [Test]
        public void WrapsNonSeekableStream()
        {
            var s = new TestStream(false, "some data");
            using (var actual = SeekableStreamWrapper.FromStream(s))
            {
                Assert.AreNotSame(s, actual);
                Assert.IsInstanceOfType(typeof(SeekableStreamWrapper), actual);
                Assert.IsTrue(actual.CanSeek);
                Assert.IsFalse(actual.CanWrite);
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
