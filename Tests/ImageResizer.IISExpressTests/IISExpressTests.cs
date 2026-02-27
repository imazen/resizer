// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace ImageResizer.IISExpressTests {

    [Collection("IISExpress")]
    public class IISExpressTests {
        private readonly IISExpressFixture _fixture;

        public IISExpressTests(IISExpressFixture fixture) {
            _fixture = fixture;
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetImage_Returns200_WithJpegContentType() {
            var response = _fixture.Client.GetAsync("/image.jpg").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetResizedImage_Returns200_WithCorrectWidth() {
            var response = _fixture.Client.GetAsync("/image.jpg?width=100").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var bytes = response.Content.ReadAsByteArrayAsync().Result;
            using (var ms = new MemoryStream(bytes))
            using (var img = Image.FromStream(ms)) {
                Assert.Equal(100, img.Width);
            }
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetResizedImageAsPng_Returns200_WithPngContentType() {
            var response = _fixture.Client.GetAsync("/image.jpg?width=100&format=png").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/png", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetNonexistentImage_Returns404() {
            var response = _fixture.Client.GetAsync("/nonexistent.jpg?width=100").Result;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetImage_ReturnsCachingHeaders() {
            var response = _fixture.Client.GetAsync("/image.jpg").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Verify some form of caching header is present (Last-Modified or ETag)
            Assert.True(
                response.Content.Headers.LastModified.HasValue ||
                response.Headers.ETag != null,
                "Expected Last-Modified or ETag header on image response");
        }

        [Fact]
        [Trait("requiresiisexpress", "true")]
        public void GetNonImageFile_PassesThrough() {
            var response = _fixture.Client.GetAsync("/notanimage.txt").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("This is not an image", body);
        }
    }
}
