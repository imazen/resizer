using System.Drawing;
using ImageResizer.Resizing;
using Xunit;

namespace ImageResizer.Core.Tests
{
    public class ImageLayoutEngineTests
    {
        [Theory]
        [InlineData(1600, 1200, "w=90;h=45;mode=crop;scale=canvas", 90, 45, 90, 45, 0, 200, 1600, 1000)]
        [InlineData(1600, 1200, "w=10;h=10;mode=crop", 10, 10, 10, 10, 200, 0, 1400, 1200)]
        [InlineData(1600, 1200, "w=10;h=10;mode=max", 10, 8, 10, 8, 0, 0, 1600, 1200)]
        public void LayoutImageSize(int width, int height, string query, float expectedWidth, float expectedHeight,
            float expectedCanvasWidth, float expectedCanvasHeight, float cropX, float cropY, float cropX2, float cropY2)
        {
            var ile = new ImageLayoutEngine(new Size(width, height), RectangleF.Empty);
            ile.ApplyInstructions(new Instructions(query));

            Assert.Equal(expectedWidth, ile.CopyToSize.Width);
            Assert.Equal(expectedHeight, ile.CopyToSize.Height);
            Assert.Equal(expectedCanvasWidth, ile.CanvasSize.Width);
            Assert.Equal(expectedCanvasHeight, ile.CanvasSize.Height);

            Assert.Equal(cropX, ile.CopyFrom.X);
            Assert.Equal(cropY, ile.CopyFrom.Y);
            Assert.Equal(cropX2, ile.CopyFrom.Right);
            Assert.Equal(cropY2, ile.CopyFrom.Bottom);
        }
    }
}