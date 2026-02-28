using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ImageResizer.Plugins.FFmpeg;
using Xunit;

namespace ImageResizer.Plugins.FFmpeg.Tests
{
    public class FFmpegTests
    {
        private const string TestVideoResourceName = "TestVideo.mp4";

        /// <summary>
        /// Extracts the embedded TestVideo.mp4 resource to a temp file and returns the path.
        /// Caller is responsible for deleting the file.
        /// </summary>
        private static string ExtractTestVideo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(x => x.EndsWith(TestVideoResourceName));

            string tempPath = Path.Combine(Path.GetTempPath(), "ffmpeg_test_" + Guid.NewGuid().ToString("N") + ".mp4");
            using (Stream resource = assembly.GetManifestResourceStream(resourceName))
            using (FileStream file = File.Create(tempPath))
            {
                resource.CopyTo(file);
            }
            return tempPath;
        }

        [Fact]
        public void GetSupportedFileExtensions_ContainsExpectedFormats()
        {
            var plugin = new FFmpegPlugin();
            var extensions = plugin.GetSupportedFileExtensions().ToList();

            Assert.Contains("mp4", extensions);
            Assert.Contains("avi", extensions);
            Assert.Contains("mkv", extensions);
            Assert.Contains("mov", extensions);
            Assert.Contains("wmv", extensions);
        }

        [Fact]
        public void GetSupportedQuerystringKeys_ContainsExpectedKeys()
        {
            var plugin = new FFmpegPlugin();
            var keys = plugin.GetSupportedQuerystringKeys().ToList();

            Assert.Contains("ffmpeg.seconds", keys);
            Assert.Contains("ffmpeg.percent", keys);
            Assert.Contains("ffmpeg.skipblankframes", keys);
        }

        [Trait("requiresffmpeg", "true")]
        [Fact]
        public void GetIssues_WhenFFmpegAvailable_ReportsNoIssues()
        {
            var plugin = new FFmpegPlugin();
            var issues = plugin.GetIssues().ToList();

            Assert.Empty(issues);
        }

        [Trait("requiresffmpeg", "true")]
        [Fact]
        public void Execute_ExtractFrameBySeconds_ReturnsValidImage()
        {
            string tempVideo = ExtractTestVideo();
            try
            {
                var job = new FFmpegJob
                {
                    SourcePath = tempVideo,
                    Seconds = 0.5
                };

                var manager = new FFmpegManager();
                bool result = manager.Execute(job);

                Assert.True(result);
                Assert.NotNull(job.Result);

                using (var bitmap = new Bitmap(job.Result))
                {
                    Assert.True(bitmap.Width > 0);
                    Assert.True(bitmap.Height > 0);
                }
            }
            finally
            {
                TryDelete(tempVideo);
            }
        }

        [Trait("requiresffmpeg", "true")]
        [Fact]
        public void Execute_ExtractFrameByPercent_ReturnsValidImage()
        {
            string tempVideo = ExtractTestVideo();
            try
            {
                var job = new FFmpegJob
                {
                    SourcePath = tempVideo,
                    Percent = 50
                };

                var manager = new FFmpegManager();
                bool result = manager.Execute(job);

                Assert.True(result);
                Assert.NotNull(job.Result);

                using (var bitmap = new Bitmap(job.Result))
                {
                    Assert.True(bitmap.Width > 0);
                    Assert.True(bitmap.Height > 0);
                }
            }
            finally
            {
                TryDelete(tempVideo);
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
        }
    }
}
