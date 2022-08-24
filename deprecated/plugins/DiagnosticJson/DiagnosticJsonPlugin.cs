// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ImageResizer;
using ImageResizer.ExtensionMethods;
using ImageResizer.Resizing;
using ImageResizer.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ImageResizer.Plugins.DiagnosticJson
{
    public enum DiagnosticLevel
    {
        None,
        Layout,
    }

    public class DiagnosticJsonPlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        private const string SettingsKey = "diagnosticjson";

        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            c.Pipeline.PreHandleImage += this.Pipeline_PreHandleImage;
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= this.Pipeline_PreHandleImage;
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new string[] { SettingsKey };
        }

        protected override RequestedAction PrepareDestinationBitmap(ImageState s)
        {
            if (!this.IsDiagnosticRequest(s.settings)) return RequestedAction.None;

            // Rather than allow the normal process, we will throw an
            // AlternateResponseException that contains the data we *really*
            // want to return.
            var info = new LayoutInformation(s);
            var serializer = new JsonSerializer();

            // Check to see if indented JSON has been requested.  This is useful
            // for human-readable output.
            if (s.settings.Get("j.indented", false))
            {
                serializer.Formatting = Formatting.Indented;
            }

            serializer.Converters.Add(new InstructionsConverter());

            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, info);
            var bytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());

            throw new AlternateResponseException(
                "Resizing pipeline was canceled as JSON data was requested instead.",
                "application/json; charset=utf-8",
                bytes);
        }

        /// <summary>
        /// This is where we hijack the resizing process, interrupt it, and send
        /// back the JSON data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e)
        {
            if (!this.IsDiagnosticRequest(e.RewrittenQuerystring)) return;

            AlternateResponseException.InjectExceptionHandler(e as ImageResizer.Caching.ResponseArgs);
        }

        private bool IsDiagnosticRequest(NameValueCollection nvc)
        {
            var level = nvc.Get(SettingsKey, DiagnosticLevel.None);
            return level != DiagnosticLevel.None;
        }
    }
}
