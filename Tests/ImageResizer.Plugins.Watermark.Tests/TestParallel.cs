using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Plugins.Watermark.Tests {
    [TestFixture]
    public class TestParallel {
        [Test]
        public void Test() {
            //
            // TODO: Add test logic here
            //
            Config c = new Config();
            //c.Pipeline.s
            WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
            w.align = System.Drawing.ContentAlignment.BottomLeft;
            w.hideIfTooSmall = false;
            w.keepAspectRatio = true;
            w.valuesPercentages = true;
            w.watermarkDir = "~/"; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
            w.bottomRightPadding = new System.Drawing.SizeF(0, 0);
            w.topLeftPadding = new System.Drawing.SizeF(0, 0);
            w.watermarkSize = new System.Drawing.SizeF(1, 1); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
            //Install the plugin
            w.Install(c);

            new VirtualFolder("~/","..\\..\\..\\Samples\\Images").Install(c);


            c.CurrentImageBuilder.Build("~/red-leaf.jpg", "~/results/red-leaf-watermarked.jpg", new ResizeSettings("watermark=Sun_256.png&width=400"));

        }
    }
}
