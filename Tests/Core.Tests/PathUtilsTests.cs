using System;
using System.Collections.Specialized;
using Xunit;
using ImageResizer.Util;

namespace ImageResizer.Core.Tests {
    public class PathUtilsTests {

        // --- Extension Methods ---

        [Theory]
        [InlineData("image.jpg", "png", "image.png")]
        [InlineData("image.jpg?width=100", "png", "image.png?width=100")]
        [InlineData("image.jpg.bmp", "png", "image.png")]
        [InlineData("image", "png", "image.png")]
        [InlineData("/path/image.jpg", "png", "/path/image.png")]
        [InlineData("image.jpg#frag", "png", "image.png#frag")]
        [InlineData("image.jpg?q=1#frag", "png", "image.png?q=1#frag")]
        [InlineData("image.jpg", "", "image")]
        [InlineData("image.jpg", ".png", "image.png")]
        public void SetExtension(string path, string newExt, string expected) {
            Assert.Equal(expected, PathUtils.SetExtension(path, newExt));
        }

        [Theory]
        [InlineData("image.jpg.bmp.tiff", "image")]
        [InlineData("image.jpg.bmp.tiff?hi", "image?hi")]
        [InlineData("image", "image")]
        [InlineData("/path/image.jpg", "/path/image")]
        public void RemoveFullExtension(string path, string expected) {
            Assert.Equal(expected, PathUtils.RemoveFullExtension(path));
        }

        [Theory]
        [InlineData("image.jpg.bmp.tiff", "image.jpg.bmp")]
        [InlineData("image.jpg.bmp.tiff?hi", "image.jpg.bmp?hi")]
        [InlineData("image.jpg", "image")]
        [InlineData("image", "image")]
        public void RemoveExtension(string path, string expected) {
            Assert.Equal(expected, PathUtils.RemoveExtension(path));
        }

        [Theory]
        [InlineData("image", "jpg", "image.jpg")]
        [InlineData("image?q=1", "jpg", "image.jpg?q=1")]
        [InlineData("image.png", "jpg", "image.png.jpg")]
        [InlineData("image", ".jpg", "image.jpg")]
        public void AddExtension(string path, string ext, string expected) {
            Assert.Equal(expected, PathUtils.AddExtension(path, ext));
        }

        [Theory]
        [InlineData("image.jpg", ".jpg")]
        [InlineData("image.jpg.bmp", ".jpg.bmp")]
        [InlineData("image.jpg?q=1", ".jpg")]
        [InlineData("image", "")]
        [InlineData("/path/image.jpg", ".jpg")]
        public void GetFullExtension(string path, string expected) {
            Assert.Equal(expected, PathUtils.GetFullExtension(path));
        }

        [Theory]
        [InlineData("image.jpg", ".jpg")]
        [InlineData("image.jpg.bmp", ".bmp")]
        [InlineData("image.jpg?q=1", ".jpg")]
        [InlineData("image", "")]
        public void GetExtension(string path, string expected) {
            Assert.Equal(expected, PathUtils.GetExtension(path));
        }

        // --- Querystring Methods ---

        [Fact]
        public void AddQueryString_ToPathWithoutQuery() {
            Assert.Equal("image.jpg?width=100", PathUtils.AddQueryString("image.jpg", "width=100"));
        }

        [Fact]
        public void AddQueryString_ToPathWithExistingQuery() {
            Assert.Equal("image.jpg?width=100&height=50", PathUtils.AddQueryString("image.jpg?width=100", "height=50"));
        }

        [Fact]
        public void AddQueryString_PreservesFragment() {
            Assert.Equal("image.jpg?width=100#frag", PathUtils.AddQueryString("image.jpg#frag", "width=100"));
        }

        [Fact]
        public void RemoveQueryString_Basic() {
            Assert.Equal("image.jpg", PathUtils.RemoveQueryString("image.jpg?width=100"));
        }

        [Fact]
        public void RemoveQueryString_PreservesFragment() {
            Assert.Equal("image.jpg#frag", PathUtils.RemoveQueryString("image.jpg?width=100#frag"));
        }

        [Fact]
        public void RemoveQueryString_NoQuery() {
            Assert.Equal("image.jpg", PathUtils.RemoveQueryString("image.jpg"));
        }

        // --- Parse Querystring ---

        [Fact]
        public void ParseQueryString_Basic() {
            var result = PathUtils.ParseQueryString("image.jpg?width=100&height=50");
            Assert.Equal("100", result["width"]);
            Assert.Equal("50", result["height"]);
        }

        [Fact]
        public void ParseQueryString_NoQuery_ReturnsEmpty() {
            var result = PathUtils.ParseQueryString("image.jpg");
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public void ParseQueryStringFriendlyAllowSemicolons_SemicolonSyntax() {
            var result = PathUtils.ParseQueryStringFriendlyAllowSemicolons("width=100;height=50");
            Assert.Equal("100", result["width"]);
            Assert.Equal("50", result["height"]);
        }

        [Fact]
        public void ParseQueryStringFriendlyAllowSemicolons_MixedSyntax() {
            var result = PathUtils.ParseQueryStringFriendlyAllowSemicolons(";width=100?height=50&format=png");
            Assert.Equal("100", result["width"]);
            Assert.Equal("50", result["height"]);
            Assert.Equal("png", result["format"]);
        }

        [Fact]
        public void ParseQueryStringFriendlyAllowSemicolons_StripsFragment() {
            var result = PathUtils.ParseQueryStringFriendlyAllowSemicolons("width=100#frag");
            Assert.Equal("100", result["width"]);
            Assert.Equal(1, result.Count);
        }

        [Fact]
        public void ParseQueryStringFriendly_WithoutLeadingQuestion() {
            var result = PathUtils.ParseQueryStringFriendly("width=100&height=50");
            Assert.Equal("100", result["width"]);
            Assert.Equal("50", result["height"]);
        }

        // --- Build Querystring ---

        [Fact]
        public void BuildQueryString_Basic() {
            var nvc = new NameValueCollection();
            nvc["width"] = "100";
            nvc["height"] = "50";
            string result = PathUtils.BuildQueryString(nvc);
            Assert.Contains("width=100", result);
            Assert.Contains("height=50", result);
            Assert.StartsWith("?", result);
        }

        [Fact]
        public void BuildQueryString_Empty() {
            var nvc = new NameValueCollection();
            Assert.Equal("", PathUtils.BuildQueryString(nvc));
        }

        [Fact]
        public void BuildSemicolonQueryString_Basic() {
            var nvc = new NameValueCollection();
            nvc["width"] = "100";
            string result = PathUtils.BuildSemicolonQueryString(nvc, false);
            Assert.Equal(";width=100", result);
        }

        // --- Base64U ---

        [Fact]
        public void Base64U_RoundTrip_Bytes() {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 200, 255 };
            string encoded = PathUtils.ToBase64U(data);
            Assert.DoesNotContain("=", encoded);
            Assert.DoesNotContain("+", encoded);
            Assert.DoesNotContain("/", encoded);
            byte[] decoded = PathUtils.FromBase64UToBytes(encoded);
            Assert.Equal(data, decoded);
        }

        [Fact]
        public void Base64U_RoundTrip_String() {
            string original = "Hello, World! This is a test with special chars: /+=";
            string encoded = PathUtils.ToBase64U(original);
            string decoded = PathUtils.FromBase64UToString(encoded);
            Assert.Equal(original, decoded);
        }

        [Fact]
        public void Base64U_EmptyBytes() {
            byte[] data = new byte[0];
            string encoded = PathUtils.ToBase64U(data);
            byte[] decoded = PathUtils.FromBase64UToBytes(encoded);
            Assert.Empty(decoded);
        }

        // --- Merge Querystrings ---

        [Fact]
        public void MergeOverwriteQueryString_OverwritesExisting() {
            var nvc = new NameValueCollection();
            nvc["width"] = "200";
            string result = PathUtils.MergeOverwriteQueryString("image.jpg?width=100", nvc);
            Assert.Contains("width=200", result);
        }

        [Fact]
        public void MergeQueryString_DoesNotOverwrite() {
            var nvc = new NameValueCollection();
            nvc["width"] = "200";
            nvc["height"] = "50";
            string result = PathUtils.MergeQueryString("image.jpg?width=100", nvc);
            Assert.Contains("width=100", result);
            Assert.Contains("height=50", result);
        }

        // --- ParseQueryOnly ---

        [Fact]
        public void ParseQueryOnly_HandlesValueWithEquals() {
            var result = PathUtils.ParseQueryOnly("?key=value=extra");
            Assert.Equal("value=extra", result["key"]);
        }

        [Fact]
        public void ParseQueryOnly_HandlesKeyWithoutValue() {
            var result = PathUtils.ParseQueryOnly("?key&key2");
            Assert.Equal("", result["key"]);
            Assert.Equal("", result["key2"]);
        }

        // --- RemoveNonMatchingChars ---

        [Fact]
        public void RemoveNonMatchingChars_Basic() {
            Assert.Equal("hi3", PathUtils.RemoveNonMatchingChars("hi YOU 3", "a-z3"));
        }

        [Fact]
        public void RemoveNonMatchingChars_EmptyResult() {
            Assert.Equal("", PathUtils.RemoveNonMatchingChars("ABC", "a-z"));
        }

        // --- ResolveVariablesInPath ---

        [Fact]
        public void ResolveVariablesInPath_Basic() {
            string result = PathUtils.ResolveVariablesInPath("~/uploads/<myvar>.jpg",
                (name) => name == "myvar" ? "test" : null);
            Assert.Equal("~/uploads/test.jpg", result);
        }

        [Fact]
        public void ResolveVariablesInPath_WithFilter() {
            string result = PathUtils.ResolveVariablesInPath("~/uploads/<myvar:a-z>.jpg",
                (name) => name == "myvar" ? "Hello World 123" : null);
            Assert.Equal("~/uploads/elloorld.jpg", result);
        }

        [Fact]
        public void ResolveVariablesInPath_InvalidSyntax_Throws() {
            Assert.Throws<ImageProcessingException>(() =>
                PathUtils.ResolveVariablesInPath("~/uploads/<>.jpg", (name) => "test"));
        }

        [Fact]
        public void ResolveVariablesInPath_UnknownVariable_Throws() {
            Assert.Throws<ImageProcessingException>(() =>
                PathUtils.ResolveVariablesInPath("~/uploads/<unknown>.jpg", (name) => null));
        }

        [Fact]
        public void ResolveVariablesInPath_OrphanedCloseBracket_Throws() {
            Assert.Throws<ImageProcessingException>(() =>
                PathUtils.ResolveVariablesInPath("~/uploads/>.jpg", (name) => "test"));
        }
    }
}
