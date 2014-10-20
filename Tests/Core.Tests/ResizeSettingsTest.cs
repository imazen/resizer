using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Drawing;
using ImageResizer.Util;
using System.Web;

namespace ImageResizer.Core.Tests {
   
    public class ResizeSettingsTest {
        [Fact]
        public void TestCropValues() {
            ResizeSettings s = new ResizeSettings();
            s.CropBottomRight = new System.Drawing.PointF(50, 50);
            s.CropTopLeft = new System.Drawing.PointF(0, 0);
            Assert.Equal<string>( "?crop=0,0,50,50", s.ToString());
            s.CropMode = CropMode.Auto;
            Assert.Equal<string>("?crop=auto", s.ToString());
        }


        [Fact]
        public void TestIntParsingOfInvalidSyntax() {
            ResizeSettings s = new ResizeSettings("maxwidth=100px&maxheight=50.6");
            Assert.Equal<int>(-1, s.MaxWidth);
            Assert.Equal<int>(-1,s.MaxHeight);
        }

        [Theory]
        [InlineData("red","red")]
        [InlineData("#44550099", "44550099")]
        [InlineData("666", "666666")]
        [InlineData("6663", "66666630")]
        [InlineData("green", "Green")]
        [InlineData("fefefe", "fefefe")]
        [InlineData("#fefefecc", "fefefecc")]
        [InlineData("gggaeee", "transparent")]
        public void TestBgColor(string from, string to) {
            ResizeSettings s = new ResizeSettings("bgcolor=" + HttpUtility.UrlEncode(from));
            s.BackgroundColor = s.BackgroundColor;
            Assert.Equal(to, s["bgcolor"], StringComparer.OrdinalIgnoreCase);
            
        }

        [Theory]
        [InlineData("red", "red")]
        [InlineData("#44550099", "44550099")]
        [InlineData("#fefefecc", "fefefecc")]
        public void TestRoundTripColor(string from, string expected)
        {
            Color? parsed = ParseUtils.ParseColor(from).Value;
            Assert.Equal(expected, ParseUtils.SerializeColor(parsed.Value), StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("a=1", "", "?a=1")]
        [InlineData("", "b=2", "?b=2")]
        [InlineData("a=1", "b=2", "?a=1&b=2")]
        [InlineData("b=1", "b=2", "?b=1")]
        public void TestMergingConstructor(string q, string defaults, string expected)
        {
            ResizeSettings defaultSettings = new ResizeSettings(defaults);
            ResizeSettings mergedSettings = new ResizeSettings(q, defaultSettings);
            ResizeSettings expectedSettings = new ResizeSettings(expected);

            Assert.Equal(expectedSettings.Count, mergedSettings.Count);
            foreach (string key in expectedSettings.AllKeys)
            {
                Assert.Equal(expectedSettings[key], mergedSettings[key]);
            }
        }
    }
}
