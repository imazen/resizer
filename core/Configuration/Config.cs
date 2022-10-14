// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Collections;
using Imazen.Common.Issues;
using ImageResizer.Configuration.Performance;
using ImageResizer.Configuration.Xml;
using ImageResizer.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Resizing;

namespace ImageResizer.Configuration
{
    public class Config
    {
        #region Singleton code, .Current,

        private static volatile Config _singleton = null;
        private static object _singletonLock = new object();

        /// <summary>
        ///     Gets the current (app-wide) config instance.
        /// </summary>
        /// <returns></returns>
        public static Config Current
        {
            get
            {
                if (_singleton == null)
                    lock (_singletonLock)
                    {
                        if (_singleton == null)
                            _singleton =
                                new Config(
                                    null); //Null lets configuration be loaded lazily, although this feature isn't really used.
                    }


                foreach (var p in _singleton.Plugins.ConfigProviders)
                {
                    var c = p.GetCurrentConfig();
                    if (c != null) return c;
                }

                return _singleton;
            }
        }

        #endregion


        public Config() : this(new ResizerSection(), false)
        {
        }

        public Config(ResizerSection config) : this(config, HostingEnvironment.ApplicationPhysicalPath != null)
        {
        }

        public Config(ResizerSection config, bool addAspNetPlugins)
        {
            configuration = config;

            //Init plugins module
            plugins = new PluginConfig(this);

            //Whenever the extensions change, the image builder instance has to be replaced.
            plugins.ImageBuilderExtensions.Changed += delegate(SafeList<BuilderExtension> s)
            {
                InvalidateImageBuilder();
            };

            //Relies on plugins, must init second
            pipeline = new PipelineConfig(this);


            //Load default plugins
            new DefaultEncoder().Install(this);
            new NoCache().Install(this);
            new ClientCache().Install(this);
            new WebConfigLicenseReader().Install(this);

            if (addAspNetPlugins)
            {
                new Diagnostic()
                    .Install(this); //2017-04-04 - this plugin only sets the HTTP handler; adds no other functionality.
                new SizeLimiting().Install(this);
                new MvcRoutingShimPlugin().Install(this);
                new LicenseDisplay().Install(this);
            }

            //Load plugins immediately. Lazy plugin loading causes problems.
            plugins.LoadPlugins();

            pipeline.FireHeartbeat();
        }


        protected PluginConfig plugins = null;

        /// <summary>
        ///     Access and modify plugins
        /// </summary>
        public PluginConfig Plugins => plugins;

        private PipelineConfig pipeline = null;

        /// <summary>
        ///     Access and modify settings related to the HttpModule pipeline. Register URL rewriting hooks, etc.
        /// </summary>
        public PipelineConfig Pipeline => pipeline;

        #region ImageBuilder singleton code .CurrentImageBuilder .UpgradeImageBuilder .InvalidateImageBuilder

        [CLSCompliant(false)] protected volatile ImageBuilder _imageBuilder = null;
        [CLSCompliant(false)] protected volatile object _imageBuilderSync = new object();

        /// <summary>
        ///     Allows subclasses to be used instead of ImageBuilder. Replacements must override the Create method and call their
        ///     own constructor instead.
        /// </summary>
        /// <param name="replacement"></param>
        public void UpgradeImageBuilder(ImageBuilder replacement)
        {
            lock (_imageBuilderSync)
            {
                _imageBuilder = replacement.Create(plugins.ImageBuilderExtensions, plugins, pipeline, pipeline);
            }
        }

        /// <summary>
        ///     Returns a shared instance of ImageManager, (or a subclass if it has been upgraded).
        ///     Instances change whenever ImageBuilderExtensions change.
        /// </summary>
        /// <returns></returns>
        public ImageBuilder CurrentImageBuilder
        {
            get
            {
                if (_imageBuilder == null)
                    lock (_imageBuilderSync)
                    {
                        if (_imageBuilder == null)
                            _imageBuilder = new ImageBuilder(plugins.ImageBuilderExtensions, plugins, pipeline,
                                pipeline, pipeline?.MaxConcurrentJobs);
                    }

                return _imageBuilder;
            }
        }

        public ImageJob Build(ImageJob job)
        {
            return CurrentImageBuilder.Build(job);
        }

        /// <summary>
        ///     Shortcut to CurrentImageBuilder.Build (Useful for COM clients). Also creates a destination folder if needed, unlike
        ///     the normal .Build() call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        public void BuildImage(object source, object dest, string settings)
        {
            if (dest is string)
            {
                var d = dest as string;
                //If it's not a virtual path, make sure the directory exists.
                if (!string.IsNullOrEmpty(d) && !d.StartsWith("~") && !d.Contains("/") && d.Contains("\\"))
                {
                    d = Path.GetDirectoryName(d);
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }
            }

            CurrentImageBuilder.Build(source, dest, new ResizeSettings(settings));
        }


        protected void InvalidateImageBuilder()
        {
            lock (_imageBuilderSync)
            {
                if (_imageBuilder != null)
                    _imageBuilder = _imageBuilder.Create(plugins.ImageBuilderExtensions, plugins, pipeline, pipeline);
            }
        }

        #endregion


        private volatile ResizerSection configuration;
        private object configurationLock = new object();

        /// <summary>
        ///     The ResizeConfigurationSection is not thread safe, and should not be modified
        ///     Dynamically loads the ResizerSection from web.config when accessed for the first time.
        ///     If the resizer node doesn't exist, an empty configuration object is created with just the root resizer node.
        /// </summary>
        protected ResizerSection cs
        {
            get
            {
                if (configuration == null)
                    lock (configurationLock)
                    {
                        if (configuration == null)
                        {
                            var section = ConfigurationManager.GetSection("resizer") as ResizerSection;
                            configuration = section != null ? section : new ResizerSection();
                        }
                    }

                return configuration;
            }
        }

        public IssueSink configurationSectionIssues => cs.IssueSink;

        /// <summary>
        ///     Returns a list of all issues reported by the resizing core, as well as by all the plugins
        /// </summary>
        public IIssueProvider AllIssues => new IssueGatherer(this);

        public string get(string selector, string defaultValue)
        {
            return cs.getAttr(selector, defaultValue);
        }

        public int get(string selector, int defaultValue)
        {
            int i;
            var s = cs.getAttr(selector, defaultValue.ToString(NumberFormatInfo.InvariantInfo));
            if (int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i)) return i;
            else
                configurationSectionIssues.AcceptIssue(
                    new Issue("Invalid integer value in <resizer> configuration section, " + selector + ":" + s,
                        IssueSeverity.ConfigurationError));
            return defaultValue;
        }
        
        public long get(string selector, long defaultValue)
        {
            long i;
            var s = cs.getAttr(selector, defaultValue.ToString(NumberFormatInfo.InvariantInfo));
            if (long.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i)) return i;
            else
                configurationSectionIssues.AcceptIssue(
                    new Issue("Invalid integer value in <resizer> configuration section, " + selector + ":" + s,
                        IssueSeverity.ConfigurationError));
            return defaultValue;
        }

        public bool get(string selector, bool defaultValue)
        {
            var s = cs.getAttr(selector, defaultValue.ToString(NumberFormatInfo.InvariantInfo));

            if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
            else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                     "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false;
            else
                configurationSectionIssues.AcceptIssue(
                    new Issue("Invalid boolean value in imageresizer configuration section, " + selector + ":" + s,
                        IssueSeverity.ConfigurationError));
            return defaultValue;
        }

        public T get<T>(string selector, T defaultValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");

            var value = get(selector, null);
            if (value == null) return defaultValue;
            else value = value.Trim();
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (ArgumentException)
            {
                //Build a list of valid names for the enumeration
                var validNames = Enum.GetNames(typeof(T));
                var valid = "";
                for (var i = 0; i < validNames.Length; i++)
                    valid += i == validNames.Length - 1
                        ? ", and " + validNames[i]
                        : (i != 0 ? ", " : "") + validNames[i];

                configurationSectionIssues.AcceptIssue(new Issue(
                    "Failed to parse " + selector + ". Invalid value \"" + value + "\".", "Valid values are " + valid,
                    IssueSeverity.ConfigurationError));
                return defaultValue;
            }
        }

        /// <summary>
        ///     Returns a deep copy of the specified node
        /// </summary>
        /// <param name="selector"></param>
        public Node getNode(string selector)
        {
            return cs.getCopyOfNode(selector);
        }

        /// <summary>
        ///     Returns a deep copy if the current state of the configuration tree (starting with the 'resizer' element as the
        ///     root)
        /// </summary>
        public Node getConfigXml()
        {
            return cs.getCopyOfRootNode();
        }

        /// <summary>
        ///     Replaces the configuration tree with the specified alternative
        /// </summary>
        /// <param name="n"></param>
        public void setConfigXml(Node n)
        {
            cs.replaceRootNode(n);
        }

        /// <summary>
        ///     Replaces the configuration tree with the specified alternative
        /// </summary>
        /// <param name="xml"></param>
        public void setConfigXmlText(string xml)
        {
            cs.replaceRootNode(Node.FromXmlFragment(xml, cs.IssueSink));
        }

        /// <summary>
        ///     Writes a diagnostic page to the specified physical path
        /// </summary>
        /// <param name="path"></param>
        public void WriteDiagnosticsTo(string path)
        {
            File.WriteAllText(path, GetDiagnosticsPage());
        }

        /// <summary>
        ///     Returns a string of the diagnostics page
        /// </summary>
        /// <returns></returns>
        public string GetDiagnosticsPage()
        {
            return new DiagnosticsReport(this, HttpContext.Current).Generate();
        }

        /// <summary>
        ///     Returns a string of the public licenses page
        /// </summary>
        /// <returns></returns>
        public string GetLicensesPage()
        {
            return LicenseDisplay.GetPageText(this);
        }
    }
}