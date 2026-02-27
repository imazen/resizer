using System;
using Xunit;
using ImageResizer.Plugins.BatchZipper;

namespace ImageResizer.Plugins.BatchZipper.Tests {
    public class BatchResizeItemTests {

        [Fact]
        public void Constructor_SetsProperties() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            Assert.Equal(@"C:\images\photo.jpg", item.PhysicalPath);
            Assert.Equal("output", item.TargetFilename);
            Assert.Equal("width=100", item.ResizeQuerystring);
        }

        [Fact]
        public void Constructor_AllowsNullTargetFilename() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", null, "width=100");
            Assert.Null(item.TargetFilename);
        }

        [Fact]
        public void Constructor_AllowsNullQuerystring() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", null);
            Assert.Null(item.ResizeQuerystring);
        }

        [Fact]
        public void Properties_AreMutableByDefault() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.PhysicalPath = @"C:\other\file.png";
            item.TargetFilename = "newname";
            item.ResizeQuerystring = "height=200";
            Assert.Equal(@"C:\other\file.png", item.PhysicalPath);
            Assert.Equal("newname", item.TargetFilename);
            Assert.Equal("height=200", item.ResizeQuerystring);
        }

        [Fact]
        public void SetImmutable_PreventsPhysicalPathChange() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.SetImmutable();
            Assert.Throws<InvalidOperationException>(() => item.PhysicalPath = "other");
        }

        [Fact]
        public void SetImmutable_PreventsTargetFilenameChange() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.SetImmutable();
            Assert.Throws<InvalidOperationException>(() => item.TargetFilename = "other");
        }

        [Fact]
        public void SetImmutable_PreventsResizeQuerystringChange() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.SetImmutable();
            Assert.Throws<InvalidOperationException>(() => item.ResizeQuerystring = "other");
        }

        [Fact]
        public void SetImmutable_AllowsReadingProperties() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.SetImmutable();
            Assert.Equal(@"C:\images\photo.jpg", item.PhysicalPath);
            Assert.Equal("output", item.TargetFilename);
            Assert.Equal("width=100", item.ResizeQuerystring);
        }

        [Fact]
        public void Copy_CreatesMutableCopy() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            item.SetImmutable();
            var copy = item.Copy();
            // Copy inherits immutability status
            Assert.Equal(@"C:\images\photo.jpg", copy.PhysicalPath);
            Assert.Equal("output", copy.TargetFilename);
        }

        [Fact]
        public void ToString_ContainsProperties() {
            var item = new BatchResizeItem(@"C:\images\photo.jpg", "output", "width=100");
            string s = item.ToString();
            Assert.Contains("photo.jpg", s);
            Assert.Contains("output", s);
            Assert.Contains("width=100", s);
        }
    }
}
