// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
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
            if (i["down.filter"] == null) i["down.filter"] = "cubicfast";
            if (i["down.colorspace"] == null) i["down.colorspace"] = "linear";

            var c = new Config();
            new FastScalingPlugin().Install(c);

            var job = new ImageJob(source, typeof(Bitmap), i);

            c.Build(job);
            return job.Result as Bitmap;
        }

        [Fact]
        public void CheckForBlurredEdges()
        {
            var  background = Color.FromArgb(255, 0, 5, 40);
            var inner = Color.FromArgb(255, 240, 189, 35);

            var b = CreateRingedBitmap(200, 86, background, inner, 12);
            var i = new Instructions() { Width = 50 };
            i["down.filter"] = "catrom";
            using (var result = BuildWithFastScaling(b,i))
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
        [InlineData("down.window=0.2", Side.Right | Side.Bottom)]
        [InlineData("down.window=0.2", Side.Left | Side.Top)]
        [InlineData("down.window=0.6", Side.Right | Side.Bottom)]
        [InlineData("down.window=0.6", Side.Left | Side.Top)]
        [InlineData("down.window=1", Side.Right | Side.Bottom)]
        [InlineData("down.window=1", Side.Left | Side.Top)]
        [InlineData("down.window=2", Side.Right | Side.Bottom)]
        [InlineData("down.window=2", Side.Left | Side.Top)]
        public void CheckForLostBorder(string instructions, Side toCheck)
        {
            var background = Color.FromArgb(255, 255, 0, 0);
            var border = Color.FromArgb(255, 0,255,0);
            var b = CreateRingedBitmap(200, 86, border, background, 3);

            var i = new Instructions(instructions);
            i["down.filter"] = "cubic";
            i["down.colorspace"] = "srgb";
            i.Width = 100;
            using (var result = BuildWithFastScaling(b, i))
            {
                AssertBorderColor(border, result,toCheck);
                AssertBorderColor(background, result, 3, 3, result.Width - 7, result.Height - 7, toCheck);
            }
        }


        [Fact]
        public void CheckForRoundingErrorsSimple()
        {

            var b = new Bitmap(200, 200);
            var i = new Instructions("?maxwidth=100");
            using (var result = BuildWithFastScaling(b, i))
            {

            }
        }
        [Theory]
        [InlineData(1310,1041, "?maxwidth=1200&maxheight=1200&crop=80,77.33333,488.480,464&cropxunits=584&cropyunits=464")]
        //[InlineData(255,197, "?crop=12,-2.842170943040401e-14,169.60000000000002,197&cropxunits=255&cropyunits=197&maxHeight=126&maxWidth=146")]
        //[InlineData(256, 197, "?crop=12,-2.842170943040401e-14,169.60000000000002,197&cropxunits=255&cropyunits=197&maxHeight=126&maxWidth=146")]
        public void CheckForRoundingErrors(int w, int h, string query)
        {

            var b = new Bitmap(w, h);
            var i = new Instructions(query);
            using (var result = BuildWithFastScaling(b, i))
            {
                
            }
        }

        [Theory]
        [InlineData("cubicfast", "0.5", "srgb","rings2.png", 500, 40,40,40, 1)]
        [InlineData("cubicfast", "2","srgb", "rings2.png", 500, 40, 40, 40, 1)]

        [InlineData("lanczos", null, "srgb", "rings2.png", 400, 15,15,30,1)] //lanczos3 windowed, in srgb colorspace, is very close to GDI
        //down.cs=srgb&down.f=lanczos


        public void CompareToGDI(string filter, string window, string colorspace, string image, int width, double threshold_r,  double threshold_g,  double threshold_b,  double threshold_a  )
        {
            String source = "..\\..\\..\\..\\Samples\\Images\\" + image;
           
            int block_width = 16;
            int block_height = 16;
            int blocks_x = Math.Min(5, Math.Max(2, width / block_width));
            int blocks_y = Math.Min(5, Math.Max(2, width / block_height));
            
            int block_width_padded = width / blocks_x;
            int block_height_padded = width / blocks_y;

            //R, G, B, A error threshold
            var threshold = new Tuple<double, double, double, double>(threshold_r, threshold_g, threshold_b, threshold_a);//on a scale of 0..255;

            var c = new Config();
            var i1 = new Instructions();
            new FastScalingPlugin().Install(c);

            i1["width"] = width.ToString();
            i1["fastscale"] = "false";


            var i2 = new Instructions(i1);
            i2["fastscale"] = "true";
            i2["down.colorspace"] = colorspace;

            i2["down.window"] = window;
            i2["down.filter"] = filter;
            i2["down.speed"] = "-2";
            
            var job1 = new ImageJob(source, typeof(Bitmap), i1);
            c.Build(job1);

            var job2 = new ImageJob(source, typeof(Bitmap), i2);
            c.Build(job2);

            Bitmap b1 = (job1.Result as Bitmap);
            Bitmap b2 = (job2.Result as Bitmap);


            for (int x = 0; x < blocks_x; x++ )
            for (int y = 0; y < blocks_y; y++ )
            {
                var x1 = x * block_width_padded;
                var y1 = y * block_height_padded;
                var x2 = x1 + block_width;
                var y2 = y1 + block_height;
                if (x2 >= b1.Width || y2 >= b1.Height) continue;

                var error = RMS(b1, b2, x1, y1, x2, y2);

                Assert.True(error.Item1 < threshold.Item1 &&
                            error.Item2 < threshold.Item2 &&
                            error.Item3 < threshold.Item3 &&
                            error.Item4 < threshold.Item4,
                            String.Format("Mismatch within region ({0},{1}) ({2},{3}). Root mean squared error ({4}) exceeds threshold {5}", x1, y1, x2, y2, error, threshold));
            }
        }



        private Tuple<double,double,double,double> RMS(Bitmap a, Bitmap b, int x1, int y1, int x2, int y2)
        {
            double err_r = 0, err_g = 0, err_b = 0, err_a = 0;
            long n = 0;
            for (int y = y1; y < y2; y++ )
                for (int x = x1; x < x2; x++)
                {
                    var pixa = a.GetPixel(x, y);
                    var pixb = b.GetPixel(x, y);
                    err_r += (pixb.R - pixa.R) * (pixb.R - pixa.R);
                    err_g += (pixb.G - pixa.G) * (pixb.G - pixa.G);
                    err_b += (pixb.B - pixa.B) * (pixb.B - pixa.B);
                    err_a += (pixb.A - pixa.A) * (pixb.A - pixa.A);
                    n++;
                }

            return new Tuple<double, double, double, double>(Math.Sqrt(err_r / n), Math.Sqrt(err_g / n), Math.Sqrt(err_b / n), Math.Sqrt(err_a / n));

        }


    }
}
