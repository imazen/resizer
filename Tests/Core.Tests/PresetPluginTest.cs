using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using System.Linq;

namespace ImageResizer.Tests {
   
    public class PresetPluginTest {
        [Theory]
        [InlineData("preset=p;width=50;height=50","width=50;height=100","width=100","height=100")]
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

            EqualIgnoreOrder<KeyValuePair<string, string>>(expectedDict, dict, (pair) => (pair.Key == null ? "null" : pair.Key) + "|" + (pair.Value == null ? "null" : pair.Value));
        

        }


        [Theory]
        [InlineData("preset=p;width=50;height=50", "width=50;height=100", "width=100", "height=100")]
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

            EqualIgnoreOrder<KeyValuePair<string, string>>(expectedDict, dict, (pair) => (pair.Key == null ? "null" : pair.Key) + "|" + (pair.Value == null ? "null" : pair.Value));
        }

        public void EqualIgnoreOrder<T>(IEnumerable<T> a, IEnumerable<T> b, Func<T,string> stringify) 
        {
            var la = a.OrderBy(stringify).ToList();
            var lb = b.OrderBy(stringify).ToList();
            Assert.Equal(la, lb);
        }
    }
}
