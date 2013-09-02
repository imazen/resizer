using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Core;
using MbUnit.Framework.ContractVerifiers;
using ImageResizer.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Net;
using ImageResizer.Tests;


namespace ImageResizer.Core.Tests
{

    [TestFixture]
    public class ImageJobTests
    {
        Config c = new Config();
        public ImageJobTests()
        {
        }

        [Test]
        public void TestImageJob()
        {
            var ms = new MemoryStream();
            var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), ms, new Instructions("width=50;format=jpg"));
            c.CurrentImageBuilder.Build(j);
            Assert.AreEqual(j.SourceWidth, 100);
            Assert.AreEqual(j.SourceHeight, 200);
            Assert.AreEqual(j.ResultFileExtension, "jpg");
            Assert.AreEqual(j.ResultMimeType, "image/jpeg");


        }

        [Test]
        public void TestImageInfo()
        {
           var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), null);
           c.CurrentImageBuilder.Build(j);
           Assert.AreEqual(j.SourceWidth, 100);
           Assert.AreEqual(j.SourceHeight, 200);
           Assert.AreEqual(j.ResultFileExtension, "jpg");
           Assert.AreEqual(j.ResultMimeType, "image/jpeg");
        }

    }
}
