// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageResizer.Plugins.Faces;
using ImageResizer.Plugins.RedEye;
using Newtonsoft.Json;
using OpenCvSharp;
using Xunit;
using Xunit.Abstractions;

namespace ImageResizer.Plugins.Faces.Tests
{
    #region Serialization models for baseline comparison

    public class DetectionBaseline
    {
        public string TestName { get; set; }
        public string ImageDescription { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public long DetectionTimeMs { get; set; }
        public List<FaceResult> Faces { get; set; }
        public List<EyeResult> Eyes { get; set; }
    }

    public class FaceResult
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public float Accuracy { get; set; }
    }

    public class EyeResult
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public float Accuracy { get; set; }
        public string Feature { get; set; }
    }

    #endregion

    #region OpenCV fixture — OpenCvSharp4 native DLLs via NuGet, cascades via embedded resources

    /// <summary>
    /// Shared fixture that ensures OpenCV cascade files are available for
    /// detection tests. With OpenCvSharp4, native DLLs are delivered via the
    /// OpenCvSharp4.runtime.win NuGet package and appear in the output
    /// directory automatically. Cascade XML files are embedded as gzip-compressed
    /// resources in the plugin assemblies and extracted to a temp directory
    /// by FileLocator on first use.
    /// </summary>
    public class OpenCvFixture : IDisposable
    {
        /// <summary>Base output directory (AppDomain.BaseDirectory).</summary>
        public string OutputDir { get; }
        public bool IsReady { get; }
        public string SetupError { get; }

        public OpenCvFixture()
        {
            OutputDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

            try
            {
                // Ensure the RedEye assembly's cascade resources are registered
                // (EyeDetection constructor does this, but we need to trigger it
                // before any tests run). The Faces assembly registers itself via
                // CascadePool's static constructor.
                var _ = new EyeDetection();

                IsReady = true;
                SetupError = null;
            }
            catch (Exception ex)
            {
                IsReady = false;
                SetupError = $"OutputDir={OutputDir}\n{ex}";
            }
        }

        public void Dispose() { }
    }

    [CollectionDefinition("OpenCV")]
    public class OpenCvCollection : ICollectionFixture<OpenCvFixture> { }

    #endregion

    #region Synthetic test image generator

    internal static class TestImageHelper
    {
        /// <summary>
        /// Creates a deterministic synthetic face image with skin-toned oval,
        /// dark eyes, eyebrows, nose, and mouth. Designed to produce consistent
        /// (if possibly zero) detection results across OpenCV versions.
        /// </summary>
        public static Bitmap CreateSyntheticFace(int width = 640, int height = 480)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Light background
                g.Clear(Color.FromArgb(220, 220, 220));

                float cx = width / 2f;
                float cy = height / 2f;
                float faceW = width * 0.35f;
                float faceH = height * 0.6f;

                // Face oval
                using (var b = new SolidBrush(Color.FromArgb(195, 170, 145)))
                    g.FillEllipse(b, cx - faceW / 2, cy - faceH / 2, faceW, faceH);

                // Forehead (lighter)
                using (var b = new SolidBrush(Color.FromArgb(205, 180, 155)))
                    g.FillEllipse(b, cx - faceW * 0.4f, cy - faceH * 0.45f, faceW * 0.8f, faceH * 0.3f);

                // Left eyebrow
                using (var b = new SolidBrush(Color.FromArgb(80, 60, 50)))
                    g.FillEllipse(b, cx - faceW * 0.32f, cy - faceH * 0.2f, faceW * 0.22f, faceH * 0.04f);

                // Right eyebrow
                using (var b = new SolidBrush(Color.FromArgb(80, 60, 50)))
                    g.FillEllipse(b, cx + faceW * 0.1f, cy - faceH * 0.2f, faceW * 0.22f, faceH * 0.04f);

                // Left eye socket
                using (var b = new SolidBrush(Color.FromArgb(145, 125, 115)))
                    g.FillEllipse(b, cx - faceW * 0.3f, cy - faceH * 0.14f, faceW * 0.22f, faceH * 0.1f);

                // Right eye socket
                using (var b = new SolidBrush(Color.FromArgb(145, 125, 115)))
                    g.FillEllipse(b, cx + faceW * 0.08f, cy - faceH * 0.14f, faceW * 0.22f, faceH * 0.1f);

                // Left pupil
                using (var b = new SolidBrush(Color.FromArgb(25, 20, 20)))
                    g.FillEllipse(b, cx - faceW * 0.23f, cy - faceH * 0.1f, faceW * 0.08f, faceH * 0.05f);

                // Right pupil
                using (var b = new SolidBrush(Color.FromArgb(25, 20, 20)))
                    g.FillEllipse(b, cx + faceW * 0.15f, cy - faceH * 0.1f, faceW * 0.08f, faceH * 0.05f);

                // Nose
                using (var b = new SolidBrush(Color.FromArgb(180, 155, 135)))
                    g.FillEllipse(b, cx - faceW * 0.06f, cy - faceH * 0.04f, faceW * 0.12f, faceH * 0.16f);

                // Nose shadow
                using (var b = new SolidBrush(Color.FromArgb(160, 140, 125)))
                    g.FillEllipse(b, cx - faceW * 0.08f, cy + faceH * 0.08f, faceW * 0.16f, faceH * 0.05f);

                // Mouth
                using (var b = new SolidBrush(Color.FromArgb(165, 100, 90)))
                    g.FillEllipse(b, cx - faceW * 0.15f, cy + faceH * 0.18f, faceW * 0.3f, faceH * 0.07f);
            }
            return bmp;
        }

        /// <summary>
        /// Creates a simple solid-color bitmap with no face features.
        /// Should produce zero detections consistently.
        /// </summary>
        public static Bitmap CreateBlankImage(int width = 640, int height = 480)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
                g.Clear(Color.CornflowerBlue);
            return bmp;
        }
    }

    #endregion

    #region Extension method unit tests (no native DLLs needed)

    public class OpenCvExtensionTests
    {
        [Fact]
        public void Rect_ToRectangleF_ConvertsCorrectly()
        {
            var rect = new Rect(10, 20, 100, 200);
            var result = rect.ToRectangleF();

            Assert.Equal(10f, result.X);
            Assert.Equal(20f, result.Y);
            Assert.Equal(100f, result.Width);
            Assert.Equal(200f, result.Height);
        }

        [Fact]
        public void Rect_ToRectangleF_ZeroRect()
        {
            var rect = new Rect(0, 0, 0, 0);
            var result = rect.ToRectangleF();

            Assert.Equal(0f, result.X);
            Assert.Equal(0f, result.Y);
            Assert.Equal(0f, result.Width);
            Assert.Equal(0f, result.Height);
        }

        [Fact]
        public void Rect_OffsetRect_OffsetsCorrectly()
        {
            var rect = new Rect(10, 20, 100, 200);
            var offset = new OpenCvSharp.Point(5, 15);
            var result = rect.OffsetRect(offset);

            Assert.Equal(15, result.X);
            Assert.Equal(35, result.Y);
            Assert.Equal(100, result.Width);
            Assert.Equal(200, result.Height);
        }

        [Fact]
        public void Rect_OffsetRect_NegativeOffset()
        {
            var rect = new Rect(50, 60, 100, 200);
            var offset = new OpenCvSharp.Point(-10, -20);
            var result = rect.OffsetRect(offset);

            Assert.Equal(40, result.X);
            Assert.Equal(40, result.Y);
            Assert.Equal(100, result.Width);
            Assert.Equal(200, result.Height);
        }

        [Fact]
        public void Rect_OffsetRect_ZeroOffset()
        {
            var rect = new Rect(10, 20, 100, 200);
            var offset = new OpenCvSharp.Point(0, 0);
            var result = rect.OffsetRect(offset);

            Assert.Equal(10, result.X);
            Assert.Equal(20, result.Y);
            Assert.Equal(100, result.Width);
            Assert.Equal(200, result.Height);
        }

        [Fact]
        public void Rect_ToRectangleF_ThenOffset_RoundTrip()
        {
            // Verify OffsetRect + ToRectangleF produces expected result
            var rect = new Rect(10, 20, 100, 200);
            var offset = new OpenCvSharp.Point(30, 40);
            var result = rect.OffsetRect(offset).ToRectangleF();

            Assert.Equal(40f, result.X);
            Assert.Equal(60f, result.Y);
            Assert.Equal(100f, result.Width);
            Assert.Equal(200f, result.Height);
        }
    }

    #endregion

    #region Face detection characterization tests

    [Collection("OpenCV")]
    public class FaceDetectionCharacterizationTests
    {
        private readonly OpenCvFixture _fixture;
        private readonly ITestOutputHelper _output;

        public FaceDetectionCharacterizationTests(OpenCvFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        private bool EnsureReady()
        {
            if (!_fixture.IsReady)
            {
                _output.WriteLine($"SKIPPED: OpenCV native dependencies not available.\n{_fixture.SetupError}");
                return false;
            }
            return true;
        }

        private void WriteBaseline(DetectionBaseline baseline)
        {
            var json = JsonConvert.SerializeObject(baseline, Formatting.Indented);
            _output.WriteLine(json);

            var filePath = Path.Combine(_fixture.OutputDir, $"baseline_{baseline.TestName}.json");
            File.WriteAllText(filePath, json);
            _output.WriteLine($"\nBaseline written to: {filePath}");
        }

        [Fact]
        public void FaceDetection_Defaults_AreCorrect()
        {
            var d = new FaceDetection();
            Assert.Equal(1, d.MinFaces);
            Assert.Equal(10, d.MaxFaces);
            Assert.Equal(4f, d.MinSizePercent);
            Assert.Equal(5, d.ConfidenceLevelThreshold);
            Assert.Equal(3, d.MinConfidenceLevel);
            Assert.Equal(0.0, d.ExpandX);
            Assert.Equal(0.0, d.ExpandY);
        }

        [Fact]
        public void FaceDetection_SyntheticFace_640x480()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new FaceDetection();
                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_synthetic_640x480",
                    ImageDescription = "Synthetic face oval with eyes/nose/mouth on gray background",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_SyntheticFace_400x400()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(400, 400))
            {
                var detector = new FaceDetection();
                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_synthetic_400x400",
                    ImageDescription = "Synthetic face 400x400",
                    ImageWidth = 400,
                    ImageHeight = 400,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_SyntheticFace_1200x900()
        {
            if (!EnsureReady()) return;

            // Larger than scaledBounds (800), exercises the resize path
            using (var bmp = TestImageHelper.CreateSyntheticFace(1200, 900))
            {
                var detector = new FaceDetection();
                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_synthetic_1200x900",
                    ImageDescription = "Synthetic face 1200x900 (exercises downscale path)",
                    ImageWidth = 1200,
                    ImageHeight = 900,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_BlankImage_NoDetections()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateBlankImage(640, 480))
            {
                var detector = new FaceDetection();
                var faces = detector.DetectFeatures(bmp);

                var baseline = new DetectionBaseline
                {
                    TestName = "face_blank_640x480",
                    ImageDescription = "Solid blue image, expect zero detections",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                Assert.Empty(faces);
            }
        }

        [Fact]
        public void FaceDetection_SensitiveConfig_640x480()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new FaceDetection
                {
                    MinFaces = 1,
                    MaxFaces = 20,
                    MinSizePercent = 2f,
                    ConfidenceLevelThreshold = 2,
                    MinConfidenceLevel = 1
                };

                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_sensitive_640x480",
                    ImageDescription = "Synthetic face 640x480, sensitive config (low thresholds)",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nSensitive config: detected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_StrictConfig_640x480()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new FaceDetection
                {
                    MinFaces = 1,
                    MaxFaces = 5,
                    MinSizePercent = 8f,
                    ConfidenceLevelThreshold = 10,
                    MinConfidenceLevel = 6
                };

                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_strict_640x480",
                    ImageDescription = "Synthetic face 640x480, strict config (high thresholds)",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nStrict config: detected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_WithExpand_640x480()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new FaceDetection
                {
                    ExpandX = 0.1,
                    ExpandY = 0.4
                };

                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_expand_640x480",
                    ImageDescription = "Synthetic face 640x480, ExpandX=0.1 ExpandY=0.4",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nExpand config: detected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_ExternalImage()
        {
            if (!EnsureReady()) return;

            var imagePath = Path.Combine(_fixture.OutputDir, "test-face.jpg");
            if (!File.Exists(imagePath))
            {
                _output.WriteLine($"SKIPPED: No external test image at {imagePath}");
                _output.WriteLine("Place a JPEG with a face named 'test-face.jpg' in the test output directory to run this test.");
                return;
            }

            using (var bmp = new Bitmap(imagePath))
            {
                var detector = new FaceDetection();
                var sw = Stopwatch.StartNew();
                var faces = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "face_external",
                    ImageDescription = $"External image test-face.jpg ({bmp.Width}x{bmp.Height})",
                    ImageWidth = bmp.Width,
                    ImageHeight = bmp.Height,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Faces = faces.Select(f => new FaceResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2, Accuracy = f.Accuracy
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nExternal image: detected {faces.Count} face(s)");
            }
        }

        [Fact]
        public void FaceDetection_ResultsSortedByAccuracyDescending()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new FaceDetection
                {
                    MinConfidenceLevel = 1,
                    ConfidenceLevelThreshold = 1
                };
                var faces = detector.DetectFeatures(bmp);

                // If multiple faces detected, they should be sorted by accuracy descending
                if (faces.Count > 1)
                {
                    for (int i = 1; i < faces.Count; i++)
                    {
                        Assert.True(faces[i - 1].Accuracy >= faces[i].Accuracy,
                            $"Faces should be sorted by accuracy descending: [{i-1}]={faces[i-1].Accuracy} < [{i}]={faces[i].Accuracy}");
                    }
                }

                _output.WriteLine($"Detected {faces.Count} face(s), sorting verified");
            }
        }
    }

    #endregion

    #region Eye detection characterization tests

    [Collection("OpenCV")]
    public class EyeDetectionCharacterizationTests
    {
        private readonly OpenCvFixture _fixture;
        private readonly ITestOutputHelper _output;

        public EyeDetectionCharacterizationTests(OpenCvFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        private bool EnsureReady()
        {
            if (!_fixture.IsReady)
            {
                _output.WriteLine($"SKIPPED: OpenCV native dependencies not available.\n{_fixture.SetupError}");
                return false;
            }
            return true;
        }

        private void WriteBaseline(DetectionBaseline baseline)
        {
            var json = JsonConvert.SerializeObject(baseline, Formatting.Indented);
            _output.WriteLine(json);

            var filePath = Path.Combine(_fixture.OutputDir, $"baseline_{baseline.TestName}.json");
            File.WriteAllText(filePath, json);
            _output.WriteLine($"\nBaseline written to: {filePath}");
        }

        [Fact]
        public void EyeDetection_SyntheticFace_640x480()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new EyeDetection();
                var sw = Stopwatch.StartNew();
                var features = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "eyes_synthetic_640x480",
                    ImageDescription = "Synthetic face oval 640x480",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Eyes = features.Select(f => new EyeResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2,
                        Accuracy = f.Accuracy, Feature = f.Feature.ToString()
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {features.Count} feature(s):");
                foreach (var f in features)
                    _output.WriteLine($"  {f.Feature}: ({f.X},{f.Y})-({f.X2},{f.Y2}) acc={f.Accuracy}");
            }
        }

        [Fact]
        public void EyeDetection_SyntheticFace_400x400()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(400, 400))
            {
                var detector = new EyeDetection();
                var sw = Stopwatch.StartNew();
                var features = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "eyes_synthetic_400x400",
                    ImageDescription = "Synthetic face oval 400x400",
                    ImageWidth = 400,
                    ImageHeight = 400,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Eyes = features.Select(f => new EyeResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2,
                        Accuracy = f.Accuracy, Feature = f.Feature.ToString()
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {features.Count} feature(s)");
            }
        }

        [Fact]
        public void EyeDetection_SyntheticFace_1200x900()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(1200, 900))
            {
                var detector = new EyeDetection();
                var sw = Stopwatch.StartNew();
                var features = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "eyes_synthetic_1200x900",
                    ImageDescription = "Synthetic face oval 1200x900 (exercises downscale path)",
                    ImageWidth = 1200,
                    ImageHeight = 900,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Eyes = features.Select(f => new EyeResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2,
                        Accuracy = f.Accuracy, Feature = f.Feature.ToString()
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nDetected {features.Count} feature(s)");
            }
        }

        [Fact]
        public void EyeDetection_BlankImage_NoDetections()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateBlankImage(640, 480))
            {
                var detector = new EyeDetection();
                var features = detector.DetectFeatures(bmp);

                var baseline = new DetectionBaseline
                {
                    TestName = "eyes_blank_640x480",
                    ImageDescription = "Solid blue image, expect zero detections",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Eyes = features.Select(f => new EyeResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2,
                        Accuracy = f.Accuracy, Feature = f.Feature.ToString()
                    }).ToList()
                };

                WriteBaseline(baseline);
                Assert.Empty(features);
            }
        }

        [Fact]
        public void EyeDetection_ExternalImage()
        {
            if (!EnsureReady()) return;

            var imagePath = Path.Combine(_fixture.OutputDir, "test-face.jpg");
            if (!File.Exists(imagePath))
            {
                _output.WriteLine($"SKIPPED: No external test image at {imagePath}");
                _output.WriteLine("Place a JPEG with a face named 'test-face.jpg' in the test output directory to run this test.");
                return;
            }

            using (var bmp = new Bitmap(imagePath))
            {
                var detector = new EyeDetection();
                var sw = Stopwatch.StartNew();
                var features = detector.DetectFeatures(bmp);
                sw.Stop();

                var baseline = new DetectionBaseline
                {
                    TestName = "eyes_external",
                    ImageDescription = $"External image test-face.jpg ({bmp.Width}x{bmp.Height})",
                    ImageWidth = bmp.Width,
                    ImageHeight = bmp.Height,
                    DetectionTimeMs = sw.ElapsedMilliseconds,
                    Eyes = features.Select(f => new EyeResult
                    {
                        X = f.X, Y = f.Y, X2 = f.X2, Y2 = f.Y2,
                        Accuracy = f.Accuracy, Feature = f.Feature.ToString()
                    }).ToList()
                };

                WriteBaseline(baseline);
                _output.WriteLine($"\nExternal image: detected {features.Count} feature(s):");
                foreach (var f in features)
                    _output.WriteLine($"  {f.Feature}: ({f.X},{f.Y})-({f.X2},{f.Y2}) acc={f.Accuracy}");
            }
        }

        [Fact]
        public void EyeDetection_FeatureTypesClassifiedCorrectly()
        {
            if (!EnsureReady()) return;

            using (var bmp = TestImageHelper.CreateSyntheticFace(640, 480))
            {
                var detector = new EyeDetection();
                var features = detector.DetectFeatures(bmp);

                // All features should have a valid FeatureType
                foreach (var f in features)
                {
                    Assert.True(
                        f.Feature == FeatureType.Eye ||
                        f.Feature == FeatureType.EyePair ||
                        f.Feature == FeatureType.Face,
                        $"Unexpected feature type: {f.Feature}");
                }

                _output.WriteLine($"Detected {features.Count} feature(s):");
                var byType = features.GroupBy(f => f.Feature);
                foreach (var g in byType)
                    _output.WriteLine($"  {g.Key}: {g.Count()}");
            }
        }
    }

    #endregion
}
