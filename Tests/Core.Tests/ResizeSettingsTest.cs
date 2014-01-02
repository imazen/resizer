using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using System.Drawing;
using ImageResizer.Util;
using System.Web;

namespace ImageResizer.Core.Tests {
    [TestFixture]
    public class ResizeSettingsTest {
        [Test]
        public void TestCropValues() {
            ResizeSettings s = new ResizeSettings();
            s.CropBottomRight = new System.Drawing.PointF(50, 50);
            s.CropTopLeft = new System.Drawing.PointF(0, 0);
            Assert.AreEqual<string>( "?crop=0,0,50,50", s.ToString());
            s.CropMode = CropMode.Auto;
            Assert.AreEqual<string>("?crop=auto", s.ToString());
        }


        [Test]
        public void TestIntParsingOfInvalidSyntax() {
            ResizeSettings s = new ResizeSettings("maxwidth=100px&maxheight=50.6");
            Assert.AreEqual<int>(-1, s.MaxWidth);
            Assert.AreEqual<int>(-1,s.MaxHeight);
        }

        [Test]
        [Row("red","red")]
        [Row("#44550099", "44550099")]
        [Row("666", "666666")]
        [Row("6663", "66666630")]
        [Row("green", "Green")]
        [Row("fefefe", "fefefe")]
        [Row("#fefefecc", "fefefecc")]
        [Row("gggaeee", "transparent")]
        public void TestBgColor(string from, string to) {
            ResizeSettings s = new ResizeSettings("bgcolor=" + HttpUtility.UrlEncode(from));
            s.BackgroundColor = s.BackgroundColor;
            Assert.AreEqual(to, s["bgcolor"], StringComparison.OrdinalIgnoreCase);
            
        }

        [Test]
        [Row("red", "red")]
        [Row("#44550099", "44550099")]
        [Row("#fefefecc", "fefefecc")]
        public void TestRoundTripColor(string from, string expected)
        {
            Color? parsed = ParseUtils.ParseColor(from).Value;
            Assert.AreEqual(expected, ParseUtils.SerializeColor(parsed.Value), StringComparison.InvariantCultureIgnoreCase);
        }

        [Test]
        [Row("a=1", "", "?a=1")]
        [Row("", "b=2", "?b=2")]
        [Row("a=1", "b=2", "?a=1&b=2")]
        [Row("b=1", "b=2", "?b=1")]
        public void TestMergingConstructor(string q, string defaults, string expected)
        {
            ResizeSettings defaultSettings = new ResizeSettings(defaults);
            ResizeSettings mergedSettings = new ResizeSettings(q, defaultSettings);
            ResizeSettings expectedSettings = new ResizeSettings(expected);

            Assert.AreEqual(expectedSettings.Count, mergedSettings.Count);
            foreach (string key in expectedSettings.AllKeys)
            {
                Assert.AreEqual(expectedSettings[key], mergedSettings[key]);
            }
        }
    }
}
