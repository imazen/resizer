using System;
using System.Collections.Generic;
using Xunit;
using ImageResizer.Plugins.BatchZipper;

namespace ImageResizer.Plugins.BatchZipper.Tests {
    public class BatchResizeSettingsTests {

        // --- NormalizePathForUseInZipFile ---

        [Fact]
        public void NormalizePath_Null_ReturnsNull() {
            Assert.Null(BatchResizeSettings.NormalizePathForUseInZipFile(null));
        }

        [Fact]
        public void NormalizePath_Empty_ReturnsEmpty() {
            Assert.Equal("", BatchResizeSettings.NormalizePathForUseInZipFile(""));
        }

        [Fact]
        public void NormalizePath_BackslashesToForwardSlashes() {
            Assert.Equal("images/photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile(@"images\photo.jpg"));
        }

        [Fact]
        public void NormalizePath_TrimsVolumeLetter() {
            Assert.Equal("images/photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile(@"C:\images\photo.jpg"));
        }

        [Fact]
        public void NormalizePath_TrimsLeadingSlashes() {
            Assert.Equal("images/photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile("/images/photo.jpg"));
        }

        [Fact]
        public void NormalizePath_TrimsMultipleLeadingSlashes() {
            Assert.Equal("images/photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile("///images/photo.jpg"));
        }

        [Fact]
        public void NormalizePath_RemovesDotSlash() {
            Assert.Equal("images/photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile("./images/photo.jpg"));
        }

        [Fact]
        public void NormalizePath_SimpleName_ReturnsSame() {
            Assert.Equal("photo.jpg", BatchResizeSettings.NormalizePathForUseInZipFile("photo.jpg"));
        }

        // --- FixDuplicateFilenames ---

        [Fact]
        public void FixDuplicateFilenames_NullTargets_UsesPhysicalPathFilename() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\images\photo.jpg", null, "width=100"),
            };
            var settings = new BatchResizeSettings(@"C:\output\archive.zip", Guid.NewGuid(), files);
            settings.FixDuplicateFilenames();
            Assert.Equal("photo", files[0].TargetFilename);
        }

        [Fact]
        public void FixDuplicateFilenames_NoDuplicates_Unchanged() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\images\a.jpg", "first", "width=100"),
                new BatchResizeItem(@"C:\images\b.jpg", "second", "width=100"),
            };
            var settings = new BatchResizeSettings(@"C:\output\archive.zip", Guid.NewGuid(), files);
            settings.FixDuplicateFilenames();
            Assert.Equal("first", files[0].TargetFilename);
            Assert.Equal("second", files[1].TargetFilename);
        }

        [Fact]
        public void FixDuplicateFilenames_Duplicates_AppendsNumber() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\images\a.jpg", "photo", "width=100"),
                new BatchResizeItem(@"C:\images\b.jpg", "photo", "width=200"),
                new BatchResizeItem(@"C:\images\c.jpg", "photo", "width=300"),
            };
            var settings = new BatchResizeSettings(@"C:\output\archive.zip", Guid.NewGuid(), files);
            settings.FixDuplicateFilenames();
            Assert.Equal("photo", files[0].TargetFilename);
            Assert.Equal("photo_1", files[1].TargetFilename);
            Assert.Equal("photo_2", files[2].TargetFilename);
        }

        [Fact]
        public void FixDuplicateFilenames_CustomPrefix() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\images\a.jpg", "img", ""),
                new BatchResizeItem(@"C:\images\b.jpg", "img", ""),
            };
            var settings = new BatchResizeSettings(@"C:\output\archive.zip", Guid.NewGuid(), files);
            settings.FixDuplicateFilenames("-");
            Assert.Equal("img", files[0].TargetFilename);
            Assert.Equal("img-1", files[1].TargetFilename);
        }

        // --- SetImmutable via item ---

        [Fact]
        public void Items_SetImmutable_PreventsModification() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\images\a.jpg", "a", ""),
                new BatchResizeItem(@"C:\images\b.jpg", "b", ""),
            };
            foreach (var f in files) f.SetImmutable();
            Assert.Throws<InvalidOperationException>(() => files[0].PhysicalPath = "other");
            Assert.Throws<InvalidOperationException>(() => files[1].PhysicalPath = "other");
        }

        // --- BuildDict ---

        [Fact]
        public void BuildDict_CreatesCorrectMapping() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\a.jpg", "fileA", ""),
                new BatchResizeItem(@"C:\b.jpg", "fileB", ""),
            };
            var settings = new BatchResizeSettings(@"C:\out.zip", Guid.NewGuid(), files);
            var worker = new BatchResizeWorker(settings);
            var dict = worker.BuildDict(files);
            Assert.Equal(2, dict.Count);
            Assert.Same(files[0], dict["fileA"]);
            Assert.Same(files[1], dict["fileB"]);
        }

        [Fact]
        public void BuildDict_CaseInsensitive() {
            var files = new List<BatchResizeItem> {
                new BatchResizeItem(@"C:\a.jpg", "FileA", ""),
            };
            var settings = new BatchResizeSettings(@"C:\out.zip", Guid.NewGuid(), files);
            var worker = new BatchResizeWorker(settings);
            var dict = worker.BuildDict(files);
            Assert.True(dict.ContainsKey("filea"));
            Assert.True(dict.ContainsKey("FILEA"));
        }

        // --- Constructor ---

        [Fact]
        public void BatchResizeSettings_Constructor_SetsProperties() {
            var files = new List<BatchResizeItem>();
            var jobId = Guid.NewGuid();
            var settings = new BatchResizeSettings(@"C:\output\archive.zip", jobId, files);
            Assert.Equal(@"C:\output\archive.zip", settings.destinationFile);
            Assert.Equal(jobId, settings.jobId);
            Assert.Same(files, settings.files);
        }
    }
}
