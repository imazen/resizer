using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Core;
using MbUnit.Framework.ContractVerifiers;
using ImageResizer.Configuration;
using System.Drawing;
using System.IO;

namespace ImageResizer.Tests {
    [TestFixture]
    [Factory("GetConfigurations")]
    public class ImageBuilderTest {

        private Config c;
        public ImageBuilderTest(Config c){
            this.c  = c;
        }

        public static IEnumerable<Config> GetConfigurations(){
            return new Config[]{new Config(new ResizerConfigurationSection())}; //TODO - add a variety of configuration options in here.
        }

        private Bitmap GetBitmap(int width, int height) {
            Bitmap b = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.DrawString("Hello!", new Font(FontFamily.GenericSansSerif, 1), new SolidBrush(Color.Beige), new PointF(0, 0));
                g.Flush();
            }
            return b;
        }
        [Test]
        [Row(50,50,"format=jpg&quality=100")]
        [Row(1,1,"format=jpg&quality=100")]
        [Row(50,50,"format=jpg&quality=-300")]
        [Row(50,50,"format=png")]
        [Row(50,50,"format=gif")]
        public void EncodeImage(int width, int height, string query){
            using (MemoryStream ms = new MemoryStream(8000)){
                c.CurrentImageBuilder.Build(GetBitmap(width,height),ms,new ResizeSettings(query));
            }
        }

        [Test]
        [Row(50, 50, "format=jpg&quality=100")]
        [Row(1, 1, "format=jpg&quality=100")]
        [Row(50, 50, "format=jpg&quality=-300")]
        [Row(50, 50, "format=png")]
        [Row(50, 50, "format=gif")]
        public void LoadImageStream(int width, int height, string encodeQuery) {
            using (MemoryStream ms = new MemoryStream(8000)) {
                c.CurrentImageBuilder.Build(GetBitmap(width,height), ms, new ResizeSettings(encodeQuery));
                ms.Seek(0, SeekOrigin.Begin);
                //Now we test the loading

                using (Bitmap b = c.CurrentImageBuilder.LoadImage(ms, new ResizeSettings())) {
                    Assert.AreEqual<Size>(new Size(width,height), b.Size);
                }
            }
        }

        [Test]
        [Row(200,200,50,50,"?width=50&height=50")]
        public void TestBitmapSize(int originalWidth, int originalHeight, int expectedWidth, int expectedHeight, string query) {
            ResizerConfigurationSection config = new ResizerConfigurationSection();
            Config c = new Config(config);
            using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
                Assert.AreEqual<Size>(b.Size,new Size(expectedWidth,expectedHeight));
            }
        }
        /// <summary>
        /// Verifies GetFinalSize() and Build() always agree
        /// </summary>
        /// <param name="c"></param>
        /// <param name="original"></param>
        /// <param name="query"></param>
        [Test]
        [CombinatorialJoin]
        [Row(100,1, "?width=20")]
        [Row(20,100, "?height=30")]
        public void GetFinalSize(int originalWidth,int originalHeight, string query){
            using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
                Assert.AreEqual<Size>(b.Size,c.CurrentImageBuilder.GetFinalSize(new Size(originalWidth,originalHeight),new ResizeSettings(query)));
            }
        }

        [Test]
        [Row(200,200,100,100,100,100,"rotate=90")]
        [Row(200,200,100,100,50,50, "width=100")]
        [Row(200,200,100,100,50,10, "width=100&height=20&stretch=fill")]
        public void TranslatePoints(int imgWidth, int imgHeight, float x, float y, float expectedX, float expectedY, string query) {
            PointF result = c.CurrentImageBuilder.TranslatePoints(new PointF[] { new PointF(x,y) }, new Size(imgWidth,imgHeight), new ResizeSettings(query))[0];
            Assert.AreEqual<PointF>(new PointF(expectedX,expectedY), result );
        }
    }
}
