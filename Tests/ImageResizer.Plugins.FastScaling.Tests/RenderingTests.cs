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
    class RenderingTests
    {

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
        void CompositingTest()
        {
            String imgdir = "..\\..\\..\\..\\Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "premult-test.png");
            Bitmap output = BuildFast(input, "fastscale=true&width=256&bgcolor=blue");

            Color px = output.GetPixel(5, 5);
            Color tst = Color.FromArgb(255, 0, 188, 187);
            Assert.True(px == tst, "Expected: " + tst.ToString() + " Got: " + px.ToString());
        }

        
     


        [Fact]
        void AlphaMultTest()
        {
            String imgdir = "..\\..\\..\\..\\Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "premult-test.png");
            Bitmap output = BuildFast(input, "fastscale=true&width=256&");

             Color px = output.GetPixel(5, 5);
            Color tst = Color.FromArgb(128, 0, 255, 0);
            
            Assert.True(px == tst, "Expected: " + tst.ToString() + " Got: " + px.ToString());
        }

        [Fact]
        void GammaTest()
        {
            String imgdir = "..\\..\\..\\..\\Samples\\Images\\";
            Bitmap input = new Bitmap(imgdir + "gamma-test.png");
            Bitmap output = BuildFast(input, "fastscale=true&width=256");

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
                  throw new ArgumentOutOfRangeException("filter", "filter produced an invalid value, either NaN, or a peak other than x=0");
                }
                b.SetPixel(j, y, Color.Black);
              }

              b.Save(String.Format("..\\..\\..\\..\\Tests\\ImageResizer.Plugins.FastScaling.Tests.Cpp\\PlotFilter{0}.png", i));
              
            }

        }

    }
}

