using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using ImageResizer.Plugins.FastScaling.internal_use_only;

namespace ImageResizer.Plugins.FastScaling.Tests
{
    public class ErrorHandlingTests
    {

        [Theory]
        [InlineData("lanczos", InterpolationFilter.Filter_Lanczos)]
        [InlineData("cubicfast", InterpolationFilter.Filter_CubicFast)]
        public void TestEnumParsing(string name, InterpolationFilter value){
            Assert.Equal(ImageResizer.ExtensionMethods.EnumExtensions.Parse<InterpolationFilter>(name), value);
        }

        [Theory]
        [InlineData("linear", Workingspace.Floatspace_linear)]
        [InlineData("srgb", Workingspace.Floatspace_srgb)]
        public void TestColorspaceEnumParsing(string name, Workingspace value)
        {
            Assert.Equal(ImageResizer.ExtensionMethods.EnumExtensions.Parse<Workingspace>(name), value);
        }

        [Fact]
        public void TestManagedRenderer(){
            using (var context = new ExecutionContext())
            using(var source = new Bitmap(50,50, PixelFormat.Format32bppArgb))
            using (var canvas = new Bitmap(20,10, PixelFormat.Format32bppArgb)){
                var  sourceOptions = new BitmapOptions();
                sourceOptions.AllowSpaceReuse = false;
                sourceOptions.AlphaMeaningful = true;
                sourceOptions.Bitmap = source;
                sourceOptions.Crop = new Rectangle(5,5,45,45);

                var canvasOptions = new BitmapOptions();
                canvasOptions.Compositing = BitmapCompositingMode.Blend_with_self;
                canvasOptions.Crop = new Rectangle(5,5 , 15, 5);
                canvasOptions.Bitmap = canvas;

                var renderOptions = new RenderOptions();

                renderOptions.FlipVertical = true;
                renderOptions.FlipHorizontal = true;
                renderOptions.Filter = 2;
                
                var renderer = new ManagedRenderer(context, sourceOptions, canvasOptions, renderOptions, null);

                renderer.Render();
            }
        }
    }
}
