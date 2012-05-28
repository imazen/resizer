using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Tests {
    [TestFixture]
    public class PresetPluginTest {
        [Test]
        [Row("preset=p;width=50;height=50","width=50;height=100","width=100","height=100")]
        public void Test(string original, string expected, string defaults, string overrides ) {
            Config c = new Config();
            var defs = new Dictionary<string, ResizeSettings>();
            defs.Add("p", new ResizeSettings(defaults));
            var sets = new Dictionary<string, ResizeSettings>();
            sets.Add("p", new ResizeSettings(overrides));
            new Presets(defs,sets,false).Install(c);

            var e = new UrlEventArgs("/image.jpg",new ResizeSettings(original));
            c.Pipeline.FireRewritingEvents(null, null, e);
            Dictionary<string,string> expectedDict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var ex = new ResizeSettings(expected);
            foreach(string k in ex.Keys)
                expectedDict[k] = ex[k];
            Dictionary<string,string> dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(string k in e.QueryString.Keys)
                dict[k] = e.QueryString[k];

            Assert.AreElementsEqualIgnoringOrder<KeyValuePair<string, string>>(expectedDict,dict);


        }


        [Test]
        [Row("preset=p;width=50;height=50", "width=50;height=100", "width=100", "height=100")]
        public void TestModifiySettings(string original, string expected, string defaults, string overrides) {
            var defs = new Dictionary<string, ResizeSettings>();
            defs.Add("p", new ResizeSettings(defaults));
            var sets = new Dictionary<string, ResizeSettings>();
            sets.Add("p", new ResizeSettings(overrides));

            ResizeSettings result = new Presets(defs, sets, false).Modify(new ResizeSettings(original));

            Dictionary<string, string> expectedDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var ex = new ResizeSettings(expected);
            foreach (string k in ex.Keys)
                expectedDict[k] = ex[k];
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string k in result.Keys)
                dict[k] = result[k];

            Assert.AreElementsEqualIgnoringOrder<KeyValuePair<string, string>>(expectedDict, dict);
        }
    }
}
