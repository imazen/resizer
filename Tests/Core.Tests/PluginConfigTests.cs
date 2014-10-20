using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins;
using ImageResizer.Plugins.PluginB;
using SampleNamespace;
using ImageResizer.Configuration.Issues;
using System.Diagnostics;
using ImageResizer.Plugins.PluginC;
using ImageResizer.Encoding;
using ImageResizer.Caching;
using ImageResizer.Resizing;
namespace ImageResizer.Plugins {
    public class PluginA { }
}
namespace ImageResizer.Plugins.PluginB {
    public class PluginB { }
}
namespace ImageResizer.Plugins.PluginC {
    public class PluginCPlugin { }
}

namespace SampleNamespace {
    public class PluginD { }
}

namespace ImageResizer.Tests {
    
    public class PluginConfigTests {
        [Theory]
        [InlineData("DefaultEncoder",typeof(DefaultEncoder))]
        [InlineData("ImageResizer.Plugins.Basic.DefaultEncoder", typeof(DefaultEncoder))]
        [InlineData("DefaultEncoder234", null)]
        [InlineData("ImageResizer.Plugins.PluginA", typeof(PluginA))]
        [InlineData("ImageResizer.Plugins.PluginA", typeof(PluginA))]
        [InlineData("PluginB", typeof(PluginB))]
        [InlineData("ImageResizer.Plugins.PluginB.PluginB", typeof(PluginB))]
        [InlineData("PluginC", typeof(PluginCPlugin))]
        [InlineData("ImageResizer.Plugins.PluginC.PluginCPlugin", typeof(PluginCPlugin))]
        [InlineData("PluginCPlugin", typeof(PluginCPlugin))]
        [InlineData("SampleNamespace.PluginD", typeof(PluginD))]
        public void get_plugin_type(string name, Type type) {
            PluginConfig c = new PluginConfig(new Config(new ResizerSection()));
            Type t = c.FindPluginType(name);
            Debug.WriteLine(new List<IIssue>(c.GetIssues())[0]);
            Assert.Equal<Type>(type, t);
        }


        [Theory]
        [InlineData("<resizer><plugins><clear type='all' /> <add name='defaultencoder' /><add name='nocache' /></plugins></resizer>")]
        [InlineData("<resizer><plugins><remove name='defaultencoder' /><add name='defaultencoder' /><remove name='nocache' /><add name='nocache' /></plugins></resizer>")]
        [InlineData("<resizer><plugins><clear type='caches' /><add name='nocache' /></plugins></resizer>")]
        [InlineData("<resizer><plugins><clear type='extensions' /></plugins></resizer>")]
        public void LoadPlugins(string xml) {
            PluginConfig pc = new Config(new ResizerSection(xml)).Plugins;
            List<IIssue> oldIssues = new List<IIssue>(pc.GetIssues());
            pc.LoadPlugins();
            List<IIssue> issues = new List<IIssue>(pc.GetIssues());
            bool problems = false;
            foreach (IIssue i in issues) {
                if (!oldIssues.Contains(i)) {
                    Debug.WriteLine(i.ToString());
                    problems = true;
                }
            }

            Assert.False(problems,"There were errors processing the xml plugin configuration");
        }

        [Theory]
        [InlineData("<add name='defaultencoder' /><add name='nocache' /><add name='nocache' />", typeof(IPlugin), 2)]
        [InlineData("<add name='defaultencoder' /><add name='nocache' />", typeof(IEncoder), 1)]
        [InlineData("<add name='defaultencoder' /><add name='nocache' />", typeof(ICache), 1)]
        [InlineData("<add name='SizeLimiting' />", typeof(BuilderExtension), 1)]
        public void GetPluginsByType(string startingXML, Type kind, int expectedCount) {
            PluginConfig pc = new Config(new ResizerSection("<resizer><plugins>" + startingXML + "</plugins></resizer>")).Plugins;
            pc.RemoveAll();
            pc.ForceLoadPlugins(); //Then load from xml
            Assert.Equal<int>(expectedCount, pc.GetPlugins(kind).Count);
        }
    }
}
