using System.Collections.Generic;
using System.Text;
using Xunit;
using ImageResizer.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Net;
using ImageResizer.Tests;


namespace ImageResizer.Core.Tests
{

    public class ImageJobTests
    {
        Config c = new Config();
        public ImageJobTests()
        {
        }

        [Fact]
        public void TestImageJob()
        {
            var ms = new MemoryStream();
            var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), ms, new Instructions("width=50;format=jpg"));
            c.CurrentImageBuilder.Build(j);
            Assert.Equal(j.SourceWidth, 100);
            Assert.Equal(j.SourceHeight, 200);
            Assert.Equal(j.ResultFileExtension, "jpg");
            Assert.Equal(j.ResultMimeType, "image/jpeg");


        }

        [Fact]
        public void TestImageInfo()
        {
           var j = new ImageJob(ImageBuilderTest.GetBitmap(100, 200), null);
           c.CurrentImageBuilder.Build(j);
           Assert.Equal(j.SourceWidth, 100);
           Assert.Equal(j.SourceHeight, 200);
           Assert.Equal(j.ResultFileExtension, "jpg");
           Assert.Equal(j.ResultMimeType, "image/jpeg");
        }


        [Fact]
        public void TestReplaceFileInPalce()
        {
            var path = Path.GetTempFileName();
            ImageBuilderTest.GetBitmap(100, 200).Save(path,ImageFormat.Jpeg);

            var j = new ImageJob(path,path, new Instructions("width=50;format=jpg"));
            c.CurrentImageBuilder.Build(j);
            
        }

    }
}
