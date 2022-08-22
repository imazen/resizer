using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ImageResizer.Plugins.FastScaling.internal_use_only;


namespace ImageResizer.Plugins.FastScaling.Tests
{
    public class RenderingTests
    {

        static string root
        {
            get
            {
                return "..\\..\\..\\..\\";
            }
        }

        static Bitmap BuildFast(Bitmap source, String i)
        {
            Config c = new Config();

            FastScalingPlugin fs = new FastScalingPlugin();
            fs.Install(c);

            Stream dest = new MemoryStream();
            ImageJob j = new ImageJob();
            j.InstructionsAsString = i;
            j.Source = source;
            j.Dest = typeof(Bitmap);

            c.Build(j);
            return (Bitmap)j.Result;
        }

        [Fact]
        void DirectoryExists()
        {
            Assert.True(Directory.Exists(root + "Samples\\Images"));
        }



        [Fact]
        void CompositingTest()
        {
            String imgdir = root + "Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "premult-test.png");
            Bitmap output = BuildFast(input, "fastscale=true&width=256&bgcolor=blue&down.colorspace=linear&down.filter=cubicfast");

            Color px = output.GetPixel(5, 5);
            Color tst = Color.FromArgb(255, 0, 188, 187);
            Assert.True(px == tst, "Expected: " + tst.ToString() + " Got: " + px.ToString());
        }

        
     


        [Fact]
        void AlphaMultTest()
        {
            String imgdir = root + "Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "premult-test.png");
            Bitmap output = BuildFast(input, "fastscale=true&width=256&down.filter=cubicfast");

             Color px = output.GetPixel(5, 5);
            Color tst = Color.FromArgb(128, 0, 255, 0);
            
            Assert.True(px == tst, "Expected: " + tst.ToString() + " Got: " + px.ToString());
        }

        [Fact]
        void GammaTest()
        {
            String imgdir = root + "Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "gamma-test.jpg");
            Bitmap output = BuildFast(input, "fastscale=true&width=256&down.colorspace=linear&down.filter=cubicfast");

             Color px = output.GetPixel(90,70);
            Color tst = Color.FromArgb(255, 188, 188, 188);
            
            Assert.True(px == tst, "Expected: " + tst.ToString() + " Got: " + px.ToString());
        }

        
    

        [Fact]
        void PlotFunctions(){

            int width = 320;
            int height = 200;
            var buffer =  new double[320];

            //double* buffer = (double *) calloc(width, sizeof(double));
            double window = 3.2;

            for (int i = 0; i < 30; i++){

              WeightingFilter f = WeightingFilter.CreateIfValid(i);
              if (f == null) continue;
              f.SampleFilter(-1 * window, window, buffer, width);

              double vscale = (2 * height / 3) * -1 / buffer[width / 2];
              int x_axis_y = 2 * height / 3;
              int y_axis_x = width / 2;

              Bitmap b = new Bitmap(width, height);;

              Graphics g = Graphics.FromImage(b);

              g.DrawLine(Pens.LightGray, 0, x_axis_y, width, x_axis_y);
              g.DrawLine(Pens.LightGray, y_axis_x, 0, y_axis_x, height);

              //Plot integers of X
              for (int j = 0; j <= Math.Ceiling(window); j++){
                int offset =(int)( (width / 2.0) / window * (double)j);
                g.DrawLine(Pens.Red, y_axis_x + (int)offset + 2, x_axis_y - 8, y_axis_x + (int)offset - 2, x_axis_y + 8);
                g.DrawLine(Pens.Red, y_axis_x - (int)offset - 2, x_axis_y - 8, y_axis_x - (int)offset + 2, x_axis_y + 8);
              }
              //Plot ideal window bounds
              double filter_window = (width / 2.0) / window * f.window;
              g.DrawLine(Pens.Blue, y_axis_x + (int)filter_window, 0, y_axis_x + (int)filter_window, height - 1);
              g.DrawLine(Pens.Blue, y_axis_x - (int)filter_window, 0, y_axis_x - (int)filter_window, height - 1);

              //Plot filter weights 
              for (int j = 0; j < width; j++){
                int y = (int)Math.Round(buffer[j] * vscale) + x_axis_y;
                if (Double.IsNaN(buffer[j]) || buffer[j] > buffer[width / 2]){
                  throw new ArgumentOutOfRangeException("filter", "filter produced an invalid value (" + buffer[j].ToString() + "), either NaN, or a peak other than x=0");
                }
                if ( y >= 0 && y < b.Height) b.SetPixel(j, y, Color.Black);
              }

              b.Save(String.Format(root + "Tests\\ImageResizer.Plugins.FastScaling.Tests\\Plot_{0}.png", (InterpolationFilter)i));
              
            }

        }


        [Theory]
        /*
        [InlineData(0, 0, 0)]
        [InlineData(2,0,1)]

        [InlineData(-2, 1, -1.2)]

        [InlineData(-2, 1, -1.5)]


        [InlineData(2, -1, 1)]




        [InlineData(-4, 1, -4)]
        [InlineData(-3, 1, -3)]

        [InlineData(20, 0, 1)]

        [InlineData(-3, 1, -1.2)]

        */
        [InlineData(1,0,0,0)]
        [InlineData(2,2,1,1)]
        [InlineData(2,-2,1,-1.2)]
        [InlineData(2,-2,1,-1.5)]


        [InlineData(3, -2, 1, -1.5)]



        [InlineData(2, -2, 1, -3)]

        [InlineData(2, -3, 1.5, -3)]

        [InlineData(2, -3, 1.2, -3)]
        [InlineData(2, -3, .9, -3)]

        [InlineData(2, -3, 1.9, -3)]

        [InlineData(2, 3, -2.3, 3)]


        [InlineData(2, -3, 2.1, -3)]



        [InlineData(2, 3, 0.9, 3)]

        [InlineData(2, 3, 0.9, 4)]


        [InlineData(2,-3,1,-3)]

        [InlineData(6, 1,3,0)]
        [InlineData(6, 1,3,0.5)]

        [InlineData(10, 0,0.5,0.6)]
        [InlineData(10, 0.5,0.5,0.6)]
        [InlineData(10, 1,0.5,0.6)]

        void PlotColorspaces(Workingspace mode, float a, float b, float z)
        {

            var c = new ExecutionContext();

            c.UseFloatspace(mode, a, b, z);

            int width = 300;
            int height = 300;
            using (c) 
            using(Bitmap bmp = new Bitmap(width, height))
            using(Graphics g = Graphics.FromImage(bmp)){

                int x_axis = 280;
                int y_axis = 20;
            
                for (int i = 0; i < 256; i++)
                {
                    byte srgb = (byte)Math.Min(255,Math.Max(0,i));
                    float floatspace = (((float)i) / 255.0f);

                    bmp.SetPixel(y_axis + i, x_axis - c.FloatspaceToByte(floatspace), Color.Blue);
                    bmp.SetPixel(y_axis + i, x_axis - Math.Min(255, Math.Max(0, (int)(255.0f * c.ByteToFloatspace(srgb)))), Color.Red);
                }
                g.DrawLine(Pens.Gray, y_axis, x_axis, y_axis + 255, x_axis - 255);
                bmp.Save(String.Format(root + "Tests\\ImageResizer.Plugins.FastScaling.Tests\\PlotColorSpace{0}, {1}, {2}, {3}.png", mode, a, b, z));
            }

        }


    }
}

