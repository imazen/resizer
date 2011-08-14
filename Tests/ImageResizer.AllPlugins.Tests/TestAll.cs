using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Watermark;
using ImageResizer.Plugins.PrettyGifs;
using ImageResizer.Plugins.PsdReader;
using ImageResizer.Plugins.AnimatedGifs;
using ImageResizer.Plugins.AdvancedFilters;
using ImageResizer.Plugins.RemoteReader;
using ImageResizer.Plugins.SeamCarving;
using ImageResizer.Plugins.SimpleFilters;
using ImageResizer.Plugins.WhitespaceTrimmer;

namespace ImageResizer.AllPlugins.Tests {
    [TestFixture]
    public class TestAll {

        public Config GetConfig() {
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

            new VirtualFolder("~/", "..\\..\\..\\Samples\\Images").Install(c);
            new Gradient().Install(c);
            new PrettyGifs().Install(c);
            new PsdReader().Install(c);
            new AnimatedGifs().Install(c);
            new AdvancedFilters().Install(c);
            RemoteReaderPlugin rrp = new RemoteReaderPlugin(); rrp.Install(c);
            rrp.AllowRemoteRequest += delegate(object sender, RemoteRequestEventArgs args) {
                args.DenyRequest = false;
            };

            new SeamCarvingPlugin().Install(c);
            new SimpleFilters().Install(c);
            new DropShadow().Install(c);
            new WhitespaceTrimmerPlugin().Install(c);
            //s3reader
            //sqlreader

            return c;

        }


        [Test]
        [Row("width=100&height=100")]
        [Row("a.sobel=true")]
        [Row("trim.threshold=255&trim.percentpadding=5")]
        public void TestCombinations(string query) {
            Config c = GetConfig();

            c.CurrentImageBuilder.Build("~/red-leaf.jpg", new ResizeSettings(query)).Dispose();

        }
    }
}
