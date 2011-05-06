/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Web;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Determines when the diagnostics page can be viewed.
    /// </summary>
    public enum DiagnosticMode {
        /// <summary>
        /// Diagnostics are disabled
        /// </summary>
        None,
        /// <summary>
        /// Only local requests can access diagnostics
        /// </summary>
        Localhost,
        /// <summary>
        /// All hosts can view diagnostics.
        /// </summary>
        AllHosts

    }
    /// <summary>
    /// Provides the incredibly helpful /resizer.debug page we all love.
    /// 
    /// Mimics the behavior of customErrors by default. Not available if CustomErrors=true or retail=true. Available only to localhost if customErrors=RemoteOnly.
    /// Can be overriden by adding in the &lt;resizer&gt; section &lt;diagnostics enableFor="None|AllHosts|LocalHost" /&gt;
    /// 
    /// </summary>
    public class Diagnostic :IPlugin{
        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            
            if (context.Request.FilePath.EndsWith("/resizer.debug", StringComparison.OrdinalIgnoreCase) &&
                AllowResponse(context)) {
                context.RemapHandler(new DiagnosticPageHandler(c));
            } 
        }

        /// <summary>
        /// True if diagnostics can be displayed to the current user.
        /// If &lt;diagnostics enableFor="None" /&gt;, returns false.
        /// If &lt;diagnostics enableFor="Localhost" /&gt;, returns false for remote requests
        /// If &lt;diagnostics enableFor="AllHosts" /&gt;, returns true.
        /// If unspecified, uses the same behavior as ASP.NET Custom Errors.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool AllowResponse(HttpContext context) {
            
            bool detailedErrors = !context.IsCustomErrorEnabled;
            DiagnosticMode def = (detailedErrors) ? DiagnosticMode.AllHosts : DiagnosticMode.None;

            DiagnosticMode mode = c.get<DiagnosticMode>("diagnostics.enableFor", def);
            if (mode == DiagnosticMode.None) return false;
            if (mode == DiagnosticMode.AllHosts) return true;
            return context.Request.IsLocal;
            
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
