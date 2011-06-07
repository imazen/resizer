using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace ImageResizer.Core.Tests {
    [TestFixture]
    public class ResizeSettingsTest {
        [Test]
        public void TestCropValues() {
            ResizeSettings s = new ResizeSettings();
            s.CropBottomRight = new System.Drawing.PointF(50, 50);
            s.CropTopLeft = new System.Drawing.PointF(0, 0);
            Assert.AreEqual<string>( "?crop=(0,0,50,50)", s.ToString());
        }


        [Test]
        public void TestIntParsing() {
            ResizeSettings s = new ResizeSettings("maxwidth=100px&maxheight=50.6");
            Assert.AreEqual<int>(-1, s.MaxWidth);
            Assert.AreEqual<int>(-1,s.MaxHeight);
        }
    }
}
