// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ImageResizer.Configuration;
using Imazen.Common.Issues;
using ImageResizer.Encoding;
using ImageResizer.Plugins;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.PluginB;
using ImageResizer.Plugins.PluginC;
using ImageResizer.Resizing;
using SampleNamespace;
using Xunit;
using Xunit.Abstractions;

namespace ImageResizer.Plugins
{
    public class PluginA
    {
    }
}

namespace ImageResizer.Plugins.PluginB
{
    public class PluginB
    {
    }
}

namespace ImageResizer.Plugins.PluginC
{
    public class PluginCPlugin
    {
    }
}

namespace SampleNamespace
{
    public class PluginD
    {
    }
}

namespace ImageResizer.Tests
{
    public class PluginConfigTests
    {
        private readonly ITestOutputHelper output;
        
        public PluginConfigTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        [Theory]
        [InlineData("DefaultEncoder", typeof(DefaultEncoder))]
        [InlineData("ImageResizer.Plugins.Basic.DefaultEncoder", typeof(DefaultEncoder))]
        [InlineData("DefaultEncoder234", null)]
        [InlineData("ImageResizer.Plugins.PluginA", typeof(PluginA))]
        [InlineData("PluginB", typeof(PluginB))]
        [InlineData("ImageResizer.Plugins.PluginB.PluginB", typeof(PluginB))]
        [InlineData("PluginC", typeof(PluginCPlugin))]
        [InlineData("ImageResizer.Plugins.PluginC.PluginCPlugin", typeof(PluginCPlugin))]
        [InlineData("PluginCPlugin", typeof(PluginCPlugin))]
        [InlineData("SampleNamespace.PluginD", typeof(PluginD))]
        public void get_plugin_type(string name, Type type)
        {
            var c = new PluginConfig(new Config(new ResizerSection()));
            var t = c.FindPluginType(name);
            Debug.WriteLine(new List<IIssue>(c.GetIssues())[0]);
            Assert.Equal<Type>(type, t);
        }


        [Theory]
        [InlineData(
            "<resizer><plugins><clear type='all' /> <add name='defaultencoder' /><add name='nocache' /></plugins></resizer>")]
        [InlineData(
            "<resizer><plugins><remove name='defaultencoder' /><add name='defaultencoder' /><remove name='nocache' /><add name='nocache' /></plugins></resizer>")]
        [InlineData("<resizer><plugins><clear type='caches' /><add name='nocache' /></plugins></resizer>")]
        [InlineData("<resizer><plugins><clear type='extensions' /></plugins></resizer>")]
        public void LoadPlugins(string xml)
        {
            var pc = new Config(new ResizerSection(xml)).Plugins;
            var oldIssues = new List<IIssue>(pc.GetIssues());
            pc.LoadPlugins();
            var issues = new List<IIssue>(pc.GetIssues());
            var problems = false;
            foreach (var i in issues.Where(i => !oldIssues.Contains(i) && !i.Summary.Contains("cannot scale to production use")))
            {
                output.WriteLine(i.ToString());
                problems = true;
            }

            Assert.False(problems, "There were errors processing the xml plugin configuration");
        }

        [Theory]
        [InlineData("<add name='defaultencoder' /><add name='nocache' /><add name='nocache' />", typeof(IPlugin), 2)]
        [InlineData("<add name='defaultencoder' /><add name='nocache' />", typeof(IEncoder), 1)]
        [InlineData("<add name='defaultencoder' /><add name='nocache' />", typeof(IAsyncTyrantCache), 1)]
        [InlineData("<add name='SizeLimiting' />", typeof(BuilderExtension), 1)]
        public void GetPluginsByType(string startingXML, Type kind, int expectedCount)
        {
            var pc = new Config(new ResizerSection("<resizer><plugins>" + startingXML + "</plugins></resizer>"))
                .Plugins;
            pc.RemoveAll();
            pc.ForceLoadPlugins(); //Then load from xml
            Assert.Equal<int>(expectedCount, pc.GetPlugins(kind).Count);
        }
    }
}