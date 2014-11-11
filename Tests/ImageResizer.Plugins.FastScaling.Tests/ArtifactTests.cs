using ImageResizer.Configuration;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImageResizer.Plugins.FastScaling.Tests
{
    public class ArtifactTests
    {


        public static IEnumerable<object[]> BlurredEdgesData
        {
            get{
                var l = new List<object[]>();

                l.Add(new object[] { Color.FromArgb(255,0,0,0), Color.FromArgb(255, 0, 0, 0) });

                return l;
            }
        }

        public static Bitmap CreateRingedBitmap(int w, int h, Color background, Color inner, int margin )
        {
            var b = new Bitmap(w, h);
            using (var g = Graphics.FromImage(b))
            {
                g.FillRectangle(new SolidBrush(background), 0, 0, w, h);

                g.FillRectangle(new SolidBrush(inner), margin, margin, w - margin - margin, h - margin - margin);
            }
            return b;
        }
        public static void AssertBorderColor(Color expected, Bitmap b, Side side = Side.All){
            AssertBorderColor(expected,b,0,0,b.Width,b.Height, side);
        }
        [Flags()]
        public enum Side
        {
            None = 0,
            Left = 1, 
            Top= 2, 
            Right = 4, 
            Bottom = 8, 
            All = 15
        }

        public static void AssertBorderColor(Color expected, Bitmap b, int x, int y, int w, int h, Side side= Side.All){

            
            if ((side & Side.Right) > 0) Assert.Equal(expected, b.GetPixel(x + w - 1, y + h / 2));
            if ((side & Side.Bottom) > 0) Assert.Equal(expected, b.GetPixel(x + w / 2, y + h - 1));
            if ((side & Side.Top) > 0) Assert.Equal(expected, b.GetPixel(x + w / 2, y));
            if ((side & Side.Left) > 0) Assert.Equal(expected, b.GetPixel(x, y + h / 2));  


            if ((side & Side.Top) > 0 || (side & Side.Right) > 0) Assert.Equal(expected, b.GetPixel(x + w - 1, y));
            if ((side & Side.Right) > 0 || (side & Side.Bottom) > 0) Assert.Equal(expected, b.GetPixel(x + w - 1, y + h - 1));
            if ((side & Side.Left) > 0 || (side & Side.Bottom) > 0) Assert.Equal(expected, b.GetPixel(x, y + h - 1));
            if ((side & Side.Left) > 0 || (side & Side.Top) > 0) Assert.Equal(expected, b.GetPixel(x, y));
        }

        public static Bitmap BuildWithFastScaling(Bitmap source, Instructions instructions)
        {
            var i = new Instructions(instructions);
            i["fastscale"] = "true";
            var c = new Config();
            new FastScalingPlugin().Install(c);

            var job = new ImageJob(source, typeof(Bitmap), i);

            c.Build(job);
            return job.Result as Bitmap;
        }

        [Fact]
        public void CheckForBlurredEdges()
        {
            var  background = Color.FromArgb(255, 0, 5, 0);
            var inner = Color.FromArgb(255, 240, 189, 35);

            var b = CreateRingedBitmap(200, 86, background, inner, 6);

            using (var result = BuildWithFastScaling(b, new Instructions() { Width = 50 }))
            {
                AssertBorderColor(background, result);
            }
        }

        [Fact]
        public void CheckCropping()
        {
            var croppedAway = Color.FromArgb(255, 0, 0, 0);
            var inner = Color.FromArgb(255, 240, 189, 35);
            var bgcolor = Color.FromArgb(255, 0, 0, 128);
            var b = CreateRingedBitmap(200, 86, croppedAway, inner, 6);

            var i = new Instructions()
            {
                Width = 50,
                Margin = new BoxEdges(10),
                CropRectangle = new double[] { 6, 6, b.Width - 12, b.Height - 12 },
                BackgroundColor = ParseUtils.SerializeColor(bgcolor)
            };
            

            using (var result = BuildWithFastScaling(b,i))
            {
                AssertBorderColor(bgcolor, result);
                AssertBorderColor(inner,result, 10,10,result.Width -21, result.Height - 21);
            }
        }

        [Theory]
        [InlineData("window=0.1", Side.Right | Side.Bottom)]
        [InlineData("window=0.1", Side.Left | Side.Top)]
        [InlineData("window=0.3", Side.Right | Side.Bottom)]
        [InlineData("window=0.3", Side.Left | Side.Top)]
        [InlineData("window=0.5", Side.Right | Side.Bottom)]
        [InlineData("window=0.5", Side.Left | Side.Top)]
        [InlineData("window=1", Side.Right | Side.Bottom)]
        [InlineData("window=1", Side.Left | Side.Top)]
        public void CheckForLostBorder(string instructions, Side toCheck)
        {
            var background = Color.FromArgb(255, 255, 0, 0);
            var border = Color.FromArgb(255, 0,255,0);
            var b = CreateRingedBitmap(200, 86, border, background, 2);

            var i = new Instructions(instructions);
            i.Width = 50;
            using (var result = BuildWithFastScaling(b, i))
            {
                AssertBorderColor(border, result,toCheck);
                AssertBorderColor(background, result, 3, 3, result.Width - 7, result.Height - 7, toCheck);
            }
        }

        [Fact]
        public void CompareToGDI()
        {
            String source = "..\\..\\..\\..\\Samples\\Images\\rings2.png";
            int cmp_size = 3;

            var c = new Config();
            var i1 = new Instructions();
            new FastScalingPlugin().Install(c);

            i1["width"] = "500";
            i1["window"] = "0.5";
            i1["f"] = "0";

            var i2 = new Instructions(i1);
            i2["fastscale"] = "true";
            
            var job1 = new ImageJob(source, typeof(Bitmap), i1);
            c.Build(job1);

            var job2 = new ImageJob(source, typeof(Bitmap), i2);
            c.Build(job2);

            Bitmap b1 = (job1.Result as Bitmap);
            Bitmap b2 = (job2.Result as Bitmap);

            for (int x = 0; x < cmp_size; x++ )
            for (int y = 0; y < cmp_size; y++ )
            {
                Assert.True((b1.GetPixel(x, y) == b2.GetPixel(x, y)),
                            "Mismatch at px " + x.ToString() + ";" + y.ToString() +
                            " got " + b2.GetPixel(x, y).ToString() +
                            " expected " + b1.GetPixel(x, y).ToString());
            }
        }


    }
}
