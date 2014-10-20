/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using ImageResizer.Resizing;
using ImageResizer.Caching;
using ImageResizer.Plugins;
using System.Configuration;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Xml;
using System.Web;
using ImageResizer.Collections;
using System.IO;
using System.Globalization;


namespace ImageResizer.Configuration {

   
    public class Config {

        #region Singleton code, .Current,
        private static volatile Config _singleton = null;
        private static object _singletonLock = new object();
        /// <summary>
        /// Gets the current (app-wide) config instance. 
        /// </summary>
        /// <returns></returns>
        public static Config Current {
            get {
                if (_singleton == null)
                    lock (_singletonLock)
                        if (_singleton == null)
                            _singleton = new Config(null); //Null lets configuration be loaded lazily, although this feature isn't really used.


                foreach(ICurrentConfigProvider p in _singleton.Plugins.ConfigProviders){
                    Config c = p.GetCurrentConfig();
                    if (c != null) return c;
                }
                
                return _singleton;
            }
        }
        #endregion


        public Config():this(new ResizerSection()){
        }
        public Config(ResizerSection config) {

            this.configuration = config;

            //Init plugins module
            plugins = new PluginConfig(this);

            //Whenever the extensions change, the image builder instance has to be replaced.
            plugins.ImageBuilderExtensions.Changed += delegate(SafeList<BuilderExtension> s) {
                InvalidateImageBuilder();
            };

            //Relies on plugins, must init second
            pipeline = new PipelineConfig(this);

            bool isAspNet = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath != null;
            //Load default plugins
            new ImageResizer.Plugins.Basic.DefaultEncoder().Install(this);
            new ImageResizer.Plugins.Basic.NoCache().Install(this);
            new ImageResizer.Plugins.Basic.ClientCache().Install(this);
            new ImageResizer.Plugins.Basic.Diagnostic().Install(this);
            if (isAspNet)
            {
                new ImageResizer.Plugins.Basic.SizeLimiting().Install(this);
                new ImageResizer.Plugins.Basic.MvcRoutingShimPlugin().Install(this);
            }

            //Load plugins immediately. Lazy plugin loading causes problems.
            plugins.LoadPlugins();
        }

        

        protected PluginConfig plugins = null;
        /// <summary>
        /// Access and modify plugins
        /// </summary>
        public PluginConfig Plugins {
            get { return plugins; }
        }

        private PipelineConfig pipeline = null;
        /// <summary>
        /// Access and modify settings related to the HttpModule pipline. Register URL rewriting hooks, etc.
        /// </summary>
        public PipelineConfig Pipeline {
            get { return pipeline; }
        }

        #region ImageBuilder singleton code .CurrentImageBuilder .UpgradeImageBuilder .InvalidateImageBuilder
        [CLSCompliant(false)]
        protected volatile ImageBuilder _imageBuilder = null;
        [CLSCompliant(false)]
        protected volatile object _imageBuilderSync = new object();
        /// <summary>
        /// Allows subclasses to be used instead of ImageBuilder. Replacements must override the Create method and call their own constructor instead.
        /// </summary>
        /// <param name="replacement"></param>
        public void UpgradeImageBuilder(ImageBuilder replacement) {
            lock (_imageBuilderSync) _imageBuilder = replacement.Create(plugins.ImageBuilderExtensions, plugins,pipeline, pipeline);
        }

        /// <summary>
        /// Returns a shared instance of ImageManager, (or a subclass if it has been upgraded).
        /// Instances change whenever ImageBuilderExtensions change.
        /// </summary>
        /// <returns></returns>
        public ImageBuilder CurrentImageBuilder {
            get {
                if (_imageBuilder == null)
                    lock (_imageBuilderSync)
                        if (_imageBuilder == null)
                            _imageBuilder = new ImageBuilder(plugins.ImageBuilderExtensions,plugins,pipeline, pipeline);

                return _imageBuilder;
            }
        }

        public ImageJob Build(ImageJob job)
        {
            return CurrentImageBuilder.Build(job);
        }

        /// <summary>
        /// Shortuct to CurrentImageBuilder.Build (Useful for COM clients). Also creates a destination folder if needed, unlike the normal .Build() call.
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="settings"></param>
        public void BuildImage(object source, object dest, string settings) {
            if (dest is string) {
                string d = dest as string;
                //If it's not a virtual path, make sure the directory exists.
                if (!string.IsNullOrEmpty(d) && !d.StartsWith("~") && !(d.Contains("/")) && d.Contains("\\")) {
                    d = Path.GetDirectoryName(d);
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }

            }
            CurrentImageBuilder.Build(source, dest, new ResizeSettings(settings));
        }


        protected void InvalidateImageBuilder() {
            lock (_imageBuilderSync) if (_imageBuilder != null) _imageBuilder = _imageBuilder.Create(plugins.ImageBuilderExtensions, plugins,pipeline,pipeline);
        }
        #endregion


        private volatile ResizerSection configuration;
        private object configurationLock = new object();
        /// <summary>
        /// The ResizeConfigrationSection is not thread safe, and should not be modified
        /// Dynamically loads the ResizerSection from web.config when accessed for the first time. 
        /// If the resizer node doesn't exist, an empty configuration object is created with just the root resizer node.
        /// </summary>
        protected ResizerSection cs {
            get {
                if (configuration == null) {
                    lock (configurationLock) {
                        if (configuration == null) {
                            ResizerSection section = System.Configuration.ConfigurationManager.GetSection("resizer") as ResizerSection;
                            configuration = section != null ? section : new ResizerSection();
                        }
                    }
                }
                return configuration;
            }
        }
        public IssueSink configurationSectionIssues { get { return cs.IssueSink; } }
        /// <summary>
        /// Returns a list of all issues reported by the resizing core, as well as by all the plugins
        /// </summary>
        public IIssueProvider AllIssues { get { return new IssueGatherer(this); } }

        public string get(string selector, string defaultValue) {
            return cs.getAttr(selector, defaultValue);
        }
        public int get(string selector, int defaultValue) {
            int i;
            string s = cs.getAttr(selector, defaultValue.ToString(NumberFormatInfo.InvariantInfo));
            if (int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i)) return i;
            else configurationSectionIssues.AcceptIssue(
                new Issue("Invalid integer value in imageresizer configuration section, " + selector + ":" + s, IssueSeverity.ConfigurationError));
            return defaultValue;
        }

        public bool get(string selector, bool defaultValue) {
            string s = cs.getAttr(selector, defaultValue.ToString(NumberFormatInfo.InvariantInfo));

            if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
            else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false;
           else configurationSectionIssues.AcceptIssue(
                new Issue("Invalid boolean value in imageresizer configuration section, " + selector + ":" + s, IssueSeverity.ConfigurationError));
            return defaultValue;
        }

        public T get<T>(string selector, T defaultValue) where T : struct, IConvertible
        {
            //if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");

            string value = get(selector, null);
            if (value == null) return defaultValue;
            else value = value.Trim();
            try {
                return (T)Enum.Parse(typeof(T), value, true);
            } catch (ArgumentException) {
                //Build a list of valid names for the enumeration
                string[] validNames = Enum.GetNames(typeof(T));
                string valid = "";
                for (int i = 0; i < validNames.Length; i++)
                    valid += (i == validNames.Length -1) ? ", and " + validNames[i] : ((i != 0) ? ", " : "") + validNames[i];

                configurationSectionIssues.AcceptIssue(new Issue("Failed to parse " + selector + ". Invalid value \"" + value + "\".", "Valid values are " + valid, IssueSeverity.ConfigurationError));
                return defaultValue;
            }    
        }

        /// <summary>
        /// Returns a deep copy of the specified node
        /// </summary>
        /// <param name="selector"></param>
        public Node getNode(string selector) {
            return cs.getCopyOfNode(selector);
        }

        /// <summary>
        /// Returns a deep copy if the current state of the configuration tree (starting with the 'resizer' element as the root)
        /// </summary>
        public Node getConfigXml() {
            return cs.getCopyOfRootNode();
        }
        /// <summary>
        /// Replaces the configuration tree with the specified alternative
        /// </summary>
        /// <param name="n"></param>
        public void setConfigXml(Node n) {
            cs.replaceRootNode(n);
        }
        /// <summary>
        /// Replaces the configuration tree with the specified alternative
        /// </summary>
        /// <param name="xml"></param>
        public void setConfigXmlText(String xml) {
            cs.replaceRootNode(Node.FromXmlFragment(xml, cs.IssueSink));
        }

        /// <summary>
        /// Writes a diagnostic page to the specified physical path
        /// </summary>
        /// <param name="path"></param>
        public void WriteDiagnosticsTo(string path) {
            System.IO.File.WriteAllText(path, GetDiagnosticsPage());
        }
        /// <summary>
        /// Returns a string of the diagnostics page
        /// </summary>
        /// <returns></returns>
        public string GetDiagnosticsPage() {
            return new ImageResizer.Plugins.Basic.DiagnosticPageHandler(this).GenerateOutput(HttpContext.Current, this);
        }
    }
}
