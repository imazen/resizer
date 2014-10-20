using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic {
    public class DefaultSettings:IPlugin,ISettingsModifier {
        public DefaultSettings() {
            ExplicitSizeScaleMode = ScaleMode.DownscaleOnly;
            MaxSizeScaleMode = ScaleMode.DownscaleOnly;
        }
        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            LoadSettings();
            c.Plugins.add_plugin(this);
            c.Pipeline.PostRewrite += new UrlRewritingEventHandler(Pipeline_PostRewrite);
            return this;
        }
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostRewrite -= new UrlRewritingEventHandler(Pipeline_PostRewrite);
            return true;
        }

        /// <summary>
        /// We duplicate efforts in PostRewrite to ensure that the DiskCache doesn't block configuration changes. 
        /// This won't help CloudFront, however.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            e.QueryString = Modify(new ResizeSettings(e.QueryString));
        }
        /// <summary>
        /// The default scale mode to use when 'width' and/or 'height' are used, and mode is not 'max'.
        /// </summary>
        public ScaleMode ExplicitSizeScaleMode { get; set; }
        /// <summary>
        /// The default scale mode to use when 'maxwidth' and/or 'maxheight' are used (or mode=max). 
        /// </summary>
        public ScaleMode MaxSizeScaleMode { get; set; }


        public void LoadSettings() {
            ExplicitSizeScaleMode = c.get("defaultsettings.explicitSizeScaleMode", ExplicitSizeScaleMode);
            MaxSizeScaleMode = c.get("defaultsettings.maxSizeScaleMode", MaxSizeScaleMode);
        }


        public ResizeSettings Modify(ResizeSettings settings) {
            if (!string.IsNullOrEmpty(settings["scale"])) return settings; //We only provide defaults, we don't override

            bool explicitSize = (!string.IsNullOrEmpty(settings["width"]) || !string.IsNullOrEmpty(settings["height"]) || !string.IsNullOrEmpty(settings["w"]) || !string.IsNullOrEmpty(settings["h"]));
            bool maxSize = (!string.IsNullOrEmpty(settings["maxwidth"]) || !string.IsNullOrEmpty(settings["maxheight"]));
            if (explicitSize && settings.Mode == FitMode.Max) {
                explicitSize = false;
                maxSize = true;
            }

            if (explicitSize) settings.Scale = ExplicitSizeScaleMode;
            else if (maxSize) settings.Scale = MaxSizeScaleMode;


            return settings;
        }
    }
}
