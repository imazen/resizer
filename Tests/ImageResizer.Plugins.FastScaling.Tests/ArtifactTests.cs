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
        public static void AssertBorderColor(Color expected, Bitmap b){
            AssertBorderColor(expected,b,0,0,b.Width,b.Height);
        }

        public static void AssertBorderColor(Color expected, Bitmap b, int x, int y, int w, int h){
            Assert.Equal(expected, b.GetPixel(x, y));
            Assert.Equal(expected, b.GetPixel(x + w / 2, y));
            Assert.Equal(expected, b.GetPixel(x + w - 1, y));
            Assert.Equal(expected, b.GetPixel(x + w - 1, y + h / 2));
            Assert.Equal(expected, b.GetPixel(x + w - 1, y + h -1));
            Assert.Equal(expected, b.GetPixel(x + w / 2 , y + h -1));
            Assert.Equal(expected, b.GetPixel(x, y + h -1));
            Assert.Equal(expected, b.GetPixel(x, y + h / 2));  
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

        [Fact]
        public void CheckForLostBorder()
        {
            var background = Color.FromArgb(255, 255, 0, 0);
            var border = Color.FromArgb(255, 0,255,0);
            var b = CreateRingedBitmap(200, 86, border, background, 1);

            var i = new Instructions()
            {
                Width = 50
            };
            using (var result = BuildWithFastScaling(b, i))
            {
                AssertBorderColor(border, result);
                AssertBorderColor(background, result, 3, 3, result.Width - 7, result.Height - 7);
            }
        }


    }
}
