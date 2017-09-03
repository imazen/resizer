// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Web;
using System.Web.Configuration;
using ImageResizer.Configuration.Performance;

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
    /// Can be overridden by adding in the &lt;resizer&gt; section &lt;diagnostics enableFor="None|AllHosts|LocalHost" /&gt;
    /// 
    /// </summary>
    public class Diagnostic : EndpointPlugin{

        public Diagnostic()
        {
            this.EndpointMatchMethod = EndpointMatching.FilePathEndsWithOrdinalIgnoreCase;
            this.Endpoints = new[] {"/resizer.debug", "/resizer.debug.ashx"};
        }


        string DisabledNotice(Config c)
        {
            var sb = new StringBuilder();

            //Figure out CustomErrorsMode
            var mode =
            (WebConfigurationManager
                .OpenWebConfiguration(null)
                .GetSection("system.web/customErrors") as CustomErrorsSection)?.Mode ?? CustomErrorsMode.RemoteOnly;

            //What is diagnostics enableFor set to?
            var dmode = c.get("diagnostics.enableFor", DiagnosticMode.None);
            //Is it set at all?
            var diagDefined = c.get("diagnostics.enableFor", null) != null;
            //Is it available from localhost.
            var availLocally = (!diagDefined && mode == CustomErrorsMode.RemoteOnly) ||
                                (dmode == DiagnosticMode.Localhost);


            sb.Append("The ImageResizer diagnostics page is " +
                          (availLocally ? "only available from localhost." : "disabled."));
            sb.Append("\n\nThis is because ");
            sb.Append(diagDefined
                ? $"<diagnostics enableFor=\"{dmode}\" />.\n"
                : $"<customErrors mode=\"{mode}\" />.\n");
            sb.Append(
                "\n\nTo override for localhost access, add <diagnostics enableFor=\"localhost\" /> in the <resizer> section of Web.config.\n\n" +
                "To override for remote access, add <diagnostics enableFor=\"allhosts\" /> in the <resizer> section of Web.config.\n\n");
            return sb.ToString();

        }

        protected override string GenerateOutput(HttpContext context, Config c)
        {
            return AllowResponse(context, c) ? new DiagnosticsReport(c, context).Generate() : DisabledNotice(c);
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
        public bool AllowResponse(HttpContext context, Config c) {
            switch (c.get("diagnostics.enableFor", context.IsCustomErrorEnabled ? DiagnosticMode.None : DiagnosticMode.AllHosts)) {
                case DiagnosticMode.AllHosts:
                    return true;

                case DiagnosticMode.Localhost:
                    return context.Request.IsLocal;

                default:
                    return false;
            }
        }
    }
}
