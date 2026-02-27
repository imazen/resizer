using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ImageResizer.Configuration.Issues;
using Xunit;
using ImageResizer.Plugins.PdfRenderer;
using ImageResizer.Plugins.PdfRenderer.Ghostscript;

namespace ImageResizer.Plugins.Pdf.Tests
{
    public class PdfRendererTests
    {
        private PdfRendererPlugin _decoder;

        /// <summary>
        ///   Name of embedded PDF test document.
        /// </summary>
        /// <remarks>
        ///   This document was generated in Word and printed to PDF. The page size is 8.5" x 11"
        ///   It has 2 pages:
        ///   Page 1: Portrait, with large letter 'A' in red box with black border centered vertically and horizontally,
        ///   Page 2: Landscape, with large letter 'B' in green box with black border centered vertically and horizontally.
        /// </remarks>
        private const string TestDocumentFileName = "Test.pdf";

        /// <summary>
        ///   Get the embedded resource stream for Test.pdf document.
        /// </summary>
        public static Stream TestDocumentStream
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().Single(x => x.EndsWith(TestDocumentFileName));
                return assembly.GetManifestResourceStream(resourceName);
            }
        }

        public PdfRendererTests()
        {
            _decoder = new PdfRendererPlugin();

            IIssue[] issues = _decoder.GetIssues().ToArray();
            if(issues.Length > 0)
            {
                string issuesMessage = string.Join(Environment.NewLine, issues.Select(x => x.Summary).ToArray());
                Assert.Fail(string.Format("Expecting there are no plugin issues reported: {0}", issuesMessage));
            }
        }

        [Fact]
        public void GetSupportedFileExtensions_ExpectPdf()
        {
            // Act
            IEnumerable<string> supportedFileExtensions = _decoder.GetSupportedFileExtensions();

            // Assert
            Assert.Contains(".pdf", supportedFileExtensions);
        }

        #region Page1 (Portrait)

        [Fact]
        public void DecodeStream_WhenHeightSpecified_ExpectPage1()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["height"] = "400";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(400, bitmap.Height);
            Assert.Equal(309, bitmap.Width);
        }

        [Fact]
        public void DecodeStream_WhenWidthSpecified_ExpectPage1()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["width"] = "400";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(400, bitmap.Width);
            Assert.Equal(518, bitmap.Height);
        }

        [Fact]
        public void DecodeStream_WhenWidthHeightSpecified_ExpectPage1()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["height"] = "400";
            settings["width"] = "400";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(400, bitmap.Height);
            Assert.Equal(309, bitmap.Width);
        }

        #endregion

        #region Page2 (Landscape)

        [Fact]
        public void DecodeStream_WhenHeightSpecified_ExpectPage2()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["height"] = "400";
            settings["page"] = "2";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(400, bitmap.Height);
            Assert.Equal(518, bitmap.Width);
        }

        [Fact]
        public void DecodeStream_WhenWidthSpecified_ExpectPage2()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["width"] = "400";
            settings["page"] = "2";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(400, bitmap.Width);
            Assert.Equal(309, bitmap.Height);
        }

        [Fact]
        public void DecodeStream_WhenWidthHeightSpecified_ExpectPage2()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["height"] = "400";
            settings["width"] = "400";
            settings["page"] = "2";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Equal(309, bitmap.Height);
            Assert.Equal(400, bitmap.Width);
        }

        #endregion

        #region Page3 (Does not exist)

        [Fact]
        public void DecodeStream_WhenInvalidPageSpecified_ExpectNull()
        {
            // Arrange
            ResizeSettings settings = new ResizeSettings();
            settings["page"] = "3";

            // Act
            Bitmap bitmap = _decoder.DecodeStream(TestDocumentStream, settings, TestDocumentFileName);

            // Assert
            Assert.Null(bitmap);
        }

        #endregion
    }
}
