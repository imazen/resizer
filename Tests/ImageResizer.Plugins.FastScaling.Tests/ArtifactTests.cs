using ImageResizer.Configuration;
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

        [Fact]
        public void CheckForBlurredEdges()
        {
            var  background = Color.FromArgb(255, 0, 0, 0);
            var inner = Color.FromArgb(255, 240, 189, 35);
            var w = 200;
            var h = 86;
            var margin = 6;

            var b = new Bitmap(w, h);
            using (var g = Graphics.FromImage(b))
            {
                g.FillRectangle(new SolidBrush(background), 0, 0, w, h);

                g.FillRectangle(new SolidBrush(inner), margin, margin, w - margin - margin, h - margin - margin);
            }

            var c = new Config();
            new FastScalingPlugin().Install(c);

            var job = new ImageJob(b, typeof(Bitmap), new Instructions() { Width = 50 });

            c.Build(job);

            using (var result = job.Result as Bitmap)
            {
                Assert.Equal(background, result.GetPixel(0, 0));
                Assert.Equal(background, result.GetPixel(job.FinalWidth.Value / 2, 0));
                Assert.Equal(background, result.GetPixel(job.FinalWidth.Value - 1, 0));
                Assert.Equal(background, result.GetPixel(job.FinalWidth.Value - 1, job.FinalHeight.Value / 2));
                Assert.Equal(background, result.GetPixel(job.FinalWidth.Value - 1, job.FinalHeight.Value - 1));
                Assert.Equal(background, result.GetPixel(job.FinalWidth.Value /2 , job.FinalHeight.Value - 1));
                Assert.Equal(background, result.GetPixel(0, job.FinalHeight.Value - 1));
                Assert.Equal(background, result.GetPixel(0, job.FinalHeight.Value / 2));
                
                
            }
        }

    }
}
