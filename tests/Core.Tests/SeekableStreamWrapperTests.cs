// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using ImageResizer.Util;
using Xunit;

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
                Assert.IsAssignableFrom<SeekableStreamWrapper>(actual);
                Assert.True(actual.CanSeek);
                Assert.False(actual.CanWrite);
            }
        }

        private class TestStream : MemoryStream
        {
            public TestStream(bool seekable, string data)
                : base(System.Text.Encoding.UTF8.GetBytes(data), false)
            {
                this.CanSeek = seekable;
            }

            public override bool CanSeek { get; }
        }
    }
}