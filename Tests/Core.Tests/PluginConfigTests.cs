using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
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
    [TestFixture]
    public class PluginConfigTests {
        [Test]
        [Row("DefaultEncoder",typeof(DefaultEncoder))]
        [Row(".DefaultEncoder", typeof(DefaultEncoder))]
        [Row("ImageResizer.Plugins.Basic.DefaultEncoder", typeof(DefaultEncoder))]
        [Row("DefaultEncoder234", null)]
        [Row("ImageResizer.Plugins.PluginA", typeof(PluginA))]
        [Row("ImageResizer.Plugins.PluginA", typeof(PluginA))]
        [Row("PluginB", typeof(PluginB))]
        [Row("ImageResizer.Plugins.PluginB.PluginB", typeof(PluginB))]
        [Row("PluginC", typeof(PluginCPlugin))]
        [Row("ImageResizer.Plugins.PluginC.PluginCPlugin", typeof(PluginCPlugin))]
        [Row("PluginCPlugin", typeof(PluginCPlugin))]
        [Row("SampleNamespace.PluginD", typeof(PluginD))]
        [System.Security.Permissions.ReflectionPermission(System.Security.Permissions.SecurityAction.Deny)]

        public void get_plugin_type(string name, Type type) {
            PluginConfig c = new PluginConfig(new Config(new ResizerSection()));
            Type t = c.FindPluginType(name);
            Debug.WriteLine(new List<IIssue>(c.GetIssues())[0]);
            Assert.AreEqual<Type>(type, t);
        }


        [Test]
        [Row("<resizer><plugins><clear type='all' /> <add name='defaultencoder' /><add name='nocache' /></plugins></resizer>")]
        [Row("<resizer><plugins><remove name='defaultencoder' /><add name='defaultencoder' /><remove name='nocache' /><add name='nocache' /></plugins></resizer>")]
        [Row("<resizer><plugins><clear type='caches' /><add name='nocache' /></plugins></resizer>")]
        [Row("<resizer><plugins><clear type='extensions' /></plugins></resizer>")]
        [System.Security.Permissions.ReflectionPermission(System.Security.Permissions.SecurityAction.Deny)]
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

            if (problems) Assert.Fail("There were errors processing the xml plugin configuration");
        }

        [Test]
        [Row("<add name='defaultencoder' /><add name='nocache' /><add name='nocache' />", typeof(IPlugin), 2)]
        [Row("<add name='defaultencoder' /><add name='nocache' />", typeof(IEncoder), 1)]
        [Row("<add name='defaultencoder' /><add name='nocache' />", typeof(ICache), 1)]
        [Row("<add name='SizeLimiting' />", typeof(BuilderExtension), 1)]
        [System.Security.Permissions.ReflectionPermission(System.Security.Permissions.SecurityAction.Deny)]
        public void GetPluginsByType(string startingXML, Type kind, int expectedCount) {
            PluginConfig pc = new Config(new ResizerSection("<resizer><plugins>" + startingXML + "</plugins></resizer>")).Plugins;
            pc.RemoveAll();
            pc.LoadPlugins(); //Then load from xml
            Assert.AreEqual<int>(expectedCount, pc.GetPlugins(kind).Count);
        }
    }
}
