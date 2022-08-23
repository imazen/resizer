// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Drawing.Imaging;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Tests;
using Xunit;

namespace ImageResizer.Core.Tests
{
    public class ImageJobTests
    {
        private Config c = new Config();

        public ImageJobTests()
        {
        }

        [Fact]
        public void TestImageJob()
        {
            var ms = new MemoryStream();
            var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), ms, new Instructions("width=50;format=jpg"));
            c.CurrentImageBuilder.Build(j);
            Assert.Equal(100, j.SourceWidth);
            Assert.Equal(200, j.SourceHeight);
            Assert.Equal("jpg", j.ResultFileExtension);
            Assert.Equal("image/jpeg", j.ResultMimeType);
        }

        [Fact]
        public void TestImageInfo()
        {
            var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), null);
            c.CurrentImageBuilder.Build(j);
            Assert.Equal(100, j.SourceWidth);
            Assert.Equal(200, j.SourceHeight);
            Assert.Equal("jpg", j.ResultFileExtension);
            Assert.Equal("image/jpeg", j.ResultMimeType);
        }


        [Fact]
        public void TestReplaceFileInPlace()
        {
            var path = Path.GetTempFileName();
            ImageBuilderTest.GetBitmap(100, 200).Save(path, ImageFormat.Jpeg);

            var j = new ImageJob(path, path, new Instructions("width=50;format=jpg"));
            c.CurrentImageBuilder.Build(j);
        }
    }
}