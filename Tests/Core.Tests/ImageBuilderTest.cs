using System;
using System.Collections.Generic;
using System.Text; 
using ImageResizer.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Net;
using Xunit;
using System.Linq;

namespace ImageResizer.Tests {

    public class ImageBuilderTest {

        private Config c;
        public ImageBuilderTest(){
            this.c = new Config(new ResizerSection());
        }

        public static IEnumerable<Config> GetConfigurations(){
            return new Config[]{new Config(new ResizerSection())}; //TODO - add a variety of configuration options in here.
        }

        public static Bitmap GetBitmap(int width, int height) {
            Bitmap b = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.DrawString("Hello!", new Font(FontFamily.GenericSansSerif, 1), new SolidBrush(Color.Beige), new PointF(0, 0));
                g.Flush();
            }
            return b;
        }

     


        [Theory]
        [InlineData(50, 50, "format=jpg&quality=100")]
        [InlineData(1, 1, "format=jpg&quality=100")]
        [InlineData(50, 50, "format=jpg&quality=-300")]
        [InlineData(50, 50, "format=png")]
        [InlineData(50, 50, "format=gif")]
        public void EncodeImage(int width, int height, string query){
            using (MemoryStream ms = new MemoryStream(8000)){
                c.CurrentImageBuilder.Build(GetBitmap(width,height),ms,new ResizeSettings(query));
            }
        }

        [Theory]
        [InlineData(50, 50, "format=jpg&quality=100")]
        [InlineData(1, 1, "format=jpg&quality=100")]
        [InlineData(50, 50, "format=jpg&quality=-300")]
        [InlineData(50, 50, "format=png")]
        [InlineData(50, 50, "format=gif")]
        public void LoadImageStream(int width, int height, string encodeQuery) {
            using (MemoryStream ms = new MemoryStream(8000)) {
                c.CurrentImageBuilder.Build(GetBitmap(width,height), ms, new ResizeSettings(encodeQuery));
                ms.Seek(0, SeekOrigin.Begin);
                //Now we test the loading

                using (Bitmap b = c.CurrentImageBuilder.LoadImage(ms, new ResizeSettings())) {
                    Assert.Equal<Size>(new Size(width,height), b.Size);
                }
            }
        }

        [Theory]
        [InlineData(50, 50, "format=jpg&quality=100")]
        [InlineData(1, 1, "format=jpg&quality=100")]
        [InlineData(50, 50, "format=jpg&quality=-300")]
        [InlineData(50, 50, "format=png")]
        [InlineData(50, 50, "format=gif")]
        public void LoadImageBytes(int width, int height, string encodeQuery) {
            using (MemoryStream ms = new MemoryStream(8000)) {
                c.CurrentImageBuilder.Build(GetBitmap(width, height), ms, new ResizeSettings(encodeQuery));
                //Now we test the loading
                using (Bitmap b = c.CurrentImageBuilder.LoadImage(ms.ToArray(), new ResizeSettings())) {
                    Assert.Equal<Size>(new Size(width, height), b.Size);
                }
            }
        }



        [Theory]
        [InlineData(200,200,50,50,"?width=50&height=50")]
        [InlineData(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10&borderColor=green")]
        [InlineData(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10")]
        [InlineData(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10&borderColor=green")]
        [InlineData(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10")]
        public void TestBitmapSize(int originalWidth, int originalHeight, int expectedWidth, int expectedHeight, string query) {
            ResizerSection config = new ResizerSection();
            Config c = new Config(config);
            using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
                Assert.Equal<Size>(new Size(expectedWidth,expectedHeight),b.Size);
            }
        }

        public static T[] ArrayFromRange<T>(T[] originalArray, int startIndex, int length)
        {
          int actualLength = Math.Min(length, originalArray.Length - startIndex);
          T[] copy = new T[actualLength];
          Array.Copy(originalArray, startIndex, copy, 0, actualLength);
          return copy;
        }

        public static List<object[]> Combinatorial(List<object[]> appendTo, object[] prefixed, params IEnumerable<object>[] columns){
            var c = columns.Select( e => new List<object>(e));

            System.Func<int,List<object>,int> f = (a, b) => (int)(a * Math.Max(1,b.Count));
            var combinationCount = c.Aggregate<List<object>,int,int>(1, f, x => x);
            var sets = appendTo == null ? new List<object[]>(combinationCount) : appendTo;
            
            foreach (var val in c.First()){

                var prefix = new object[1 + (prefixed == null ? 0 : prefixed.Length)];
                if (prefixed != null) prefixed.CopyTo(prefix,0);
                prefix[prefix.Length - 1] = val;
                if (c.Count() > 1){
                    Combinatorial(sets,prefix, ArrayFromRange(c.ToArray(),1,c.Count() -1));
                }else{
                    sets.Add(prefix);
                }
            }
            return sets;

        }
        public static IEnumerable<object[]> Combine(params IEnumerable<object>[] columns)
        {
            return Combinatorial(null, null, columns);
        }

        public static IEnumerable<object[]> Combine3(object[] a, object[] b, object[] c)
        {
            return Combinatorial(null, null,a,b,c);
        }

        public static IEnumerable<object[]> Combine5(object[] a, object[] b, object[] c, object[] d, object[] e)
        {
            return Combinatorial(null, null, a, b, c,d,e);
        }

        /// <summary>
        /// Verifies GetFinalSize() and Build() always agree
        /// </summary>
        /// <param name="c"></param>
        /// <param name="original"></param>
        /// <param name="query"></param>
        [Theory]
        [MemberData("Combine3", new object[] { 100, 20 }, new object[] { 1, 100 }, new object[] { "?width=20", "?height=30" })]
        public void GetFinalSize(int originalWidth, int originalHeight, string query){
            using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
                Assert.Equal<Size>(b.Size,c.CurrentImageBuilder.GetFinalSize(new Size(originalWidth,originalHeight),new ResizeSettings(query)));
            }
        }

        [Theory]
        [InlineData(200,200,100,100,100,100,"rotate=90")]
        [InlineData(200,200,100,100,50,50, "width=100")]
        [InlineData(200,200,100,100,50,10, "width=100&height=20&stretch=fill")]
        public void TranslatePoints(int imgWidth, int imgHeight, float x, float y, float expectedX, float expectedY, string query) {
            PointF result = c.CurrentImageBuilder.TranslatePoints(new PointF[] { new PointF(x,y) }, new Size(imgWidth,imgHeight), new ResizeSettings(query))[0];
            Assert.Equal<PointF>(new PointF(expectedX,expectedY), result );
        }

        [Fact]
        public void TestWithWebResponseStream() {
            WebRequest request = WebRequest.Create("http://www.google.com/intl/en_com/images/srpr/logo2w.png");
            WebResponse response = request.GetResponse();

            using(Stream input = response.GetResponseStream())
            using(MemoryStream output = new MemoryStream())
            {

            ResizeSettings rs = new ResizeSettings();

            rs.Height = 100;

            rs.Stretch = StretchMode.Fill;

            rs.Scale = ScaleMode.Both;

            //ImageBuilder.Current.Build(@"C:\Temp\Images\clock.gif", output, rs);

            ImageBuilder.Current.Build(input, output, rs); 
            }

        }

        [Fact]
        public void ResizeInPlace() {
            GetBitmap(100, 100).Save("test-image.png", ImageFormat.Png);
            ImageBuilder.Current.Build("test-image.png", "test-image.png", new ResizeSettings("width=20"));
            File.Delete("test-image.png");


        }


        [Theory]
        [MemberData("Combine5", new[] { true, false }, new[] { true, false }, new[] { true, false }, new[] { true, false }, new[] { true, false })]
        public void TestSourceBitmapDisposed(bool dispose,
                                            bool useDestinationStream,
                                            bool useCorruptedSource,
                                            bool loadTwice,
                                            bool useSourceStream) {
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
                Assert.Equal<bool>(useCorruptedSource,corrupted);
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
            Assert.Equal<bool>(useCorruptedSource,wasCorrupted);

            bool wasDisposed = false;
            try {
                if (source is Bitmap) ((Bitmap)source).Clone();
                if (source is MemoryStream) wasDisposed = !((MemoryStream)source).CanRead;
            }catch (ArgumentException){wasDisposed = true;}

            Assert.Equal<bool>(dispose,wasDisposed);

        }


    }
}
