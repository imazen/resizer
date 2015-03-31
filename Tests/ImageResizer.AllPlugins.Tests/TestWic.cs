using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImageResizer.AllPlugins.Tests
{
    public class TestWic
    {
        [Fact(Skip="Windows WIC is broken")]
        public void TestWicBasic()
        {
            var c = new Config();
            new ImageResizer.Plugins.WicBuilder.WicBuilderPlugin().Install(c);

            string imgdir = "..\\..\\..\\Samples\\Images\\";

            c.CurrentImageBuilder.Build(imgdir + "red-leaf.jpg", "red-leaf-wic.jpg", new ResizeSettings("&builder=wic&width=200"));

        }
    }
}
