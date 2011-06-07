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
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace ImageResizer.Tests {
    [TestFixture]
    [Factory("GetConfigurations")]
    public class ImageBuilderTest {

        private Config c;
        public ImageBuilderTest(Config c){
            this.c  = c;
        }

        public static IEnumerable<Config> GetConfigurations(){
            return new Config[]{new Config(new ResizerSection())}; //TODO - add a variety of configuration options in here.
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
        [Row(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10&borderColor=green")]
        [Row(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10")]
        [Row(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10&borderColor=green")]
        [Row(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10")]
        public void TestBitmapSize(int originalWidth, int originalHeight, int expectedWidth, int expectedHeight, string query) {
            ResizerSection config = new ResizerSection();
            Config c = new Config(config);
            using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
                Assert.AreEqual<Size>(new Size(expectedWidth,expectedHeight),b.Size);
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


        [Test]
        public void TestSourceBitmapDisposed([Column(true, false)] bool dispose,
                                            [Column(true, false)] bool useDestinationStream,
                                            [Column(true, false)] bool useCorruptedSource,
                                            [Column(true, false)] bool loadTwice,
                                            [Column(true, false)] bool useSourceStream) {
            if (useCorruptedSource) useSourceStream = true;//Required

            object source = null;
            if (!useSourceStream){ //Source is a bitmap here
                source = GetBitmap(10,10);
            }else if (useCorruptedSource){ //A corrupted stream
                
                byte[] randomBytes = new byte[256];
                new Random().NextBytes(randomBytes);
                source = new MemoryStream(randomBytes);
                ((MemoryStream)source).Position = 0;
                ((MemoryStream)source).SetLength(randomBytes.Length);
            }else{ //A png stream
                using(Bitmap b = GetBitmap(10,10)){
                    MemoryStream ms = new MemoryStream();
                    b.Save(ms,ImageFormat.Png);
                    ms.Position = 0;
                    source = ms;
                }
            }
            
            //The destination object, if it exists.
            object dest = useDestinationStream ? new MemoryStream() : null;

            
            if (loadTwice){
                bool corrupted = false;
                try {
                    source = c.CurrentImageBuilder.LoadImage(source, new ResizeSettings());
                } catch (ImageCorruptedException) {
                    corrupted = true;
                    source = null;
                }
                Assert.AreEqual<bool>(useCorruptedSource,corrupted);
            }

            if (source == null) return;

            bool wasCorrupted = false;
            try{
                if (dest != null)
                    c.CurrentImageBuilder.Build(source, dest,new ResizeSettings(""), dispose);
                else
                    using (Bitmap b2 = c.CurrentImageBuilder.Build(source, new ResizeSettings(""), dispose)) { }
            }catch(ImageCorruptedException){
                wasCorrupted = true;
            }
            Assert.AreEqual<bool>(useCorruptedSource,wasCorrupted);

            bool wasDisposed = false;
            try {
                if (source is Bitmap) ((Bitmap)source).Clone();
                if (source is MemoryStream) wasDisposed = !((MemoryStream)source).CanRead;
            }catch (ArgumentException){wasDisposed = true;}

            Assert.AreEqual<bool>(dispose,wasDisposed);

        }


    }
}
