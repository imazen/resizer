// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using ImageResizer.Configuration.Issues;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    /// Responsible for displaying a red dot when licensing has failed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider, ILicenseDiagnosticsProvider
    {
        private ILicenseManager mgr;
        private Config c = null;
        private Computation cachedResult = null;
        ILicenseClock Clock { get; set; } = new RealClock();

        public LicenseEnforcer() : this(LicenseManagerSingleton.Singleton) { }
        public LicenseEnforcer(ILicenseManager mgr) { this.mgr = mgr; }

        private Computation Result
        {
            get
            {
                if (cachedResult?.ComputationExpires != null && cachedResult.ComputationExpires.Value < Clock.GetUtcNow())
                {
                    cachedResult = null;
                }
                return cachedResult = cachedResult ?? new Computation(this.c, ImazenPublicKeys.Production, c.configurationSectionIssues, this.mgr, Clock);
            }
        }

        private bool ShouldDisplayDot(Config c, ImageState s)
        {
#pragma warning disable 0162
            // Only unreachable when compiled in DRM mode. 
            if (!EnforcementEnabled) return false;
#pragma warning restore 0162

            // For now, we only add dots during an active HTTP request. 
            if (c == null || c.configurationSectionIssues == null || System.Web.HttpContext.Current == null) return false;

            return !Result.LicensedForRequestUrl(System.Web.HttpContext.Current?.Request?.Url);
        }

        public IPlugin Install(Config c)
        {
            this.c = c;

            // Ensure the LicenseManager can respond to heartbeats and license/licensee plugin additions for the config
            mgr.MonitorLicenses(c);
            mgr.MonitorHeartbeat(c);

            // Ensure our cache is appropriately invalidated when new licenses arrive, or when new licensed plugins are installed
            cachedResult = null;
            mgr.AddLicenseChangeHandler(this, (me, manager) => me.cachedResult = null);

            // And repopulated, so that errors show up.
            if (Result == null) throw new ApplicationException("Failed to populate license result");

            c.Plugins.add_plugin(this);

            // And don't forget a cache-breaker
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            return this;
        }

        private void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e)
        {
            // Server-side cachebreaker
            if (e.QueryString["red_dot"] != "true" && ShouldDisplayDot(this.c, null))
            {
                e.QueryString["red_dot"] = "true";
            }

        }

        protected override RequestedAction PreFlushChanges(ImageState s)
        {
            if (s.destBitmap != null && ShouldDisplayDot(c, s))
            {
                int w = s.destBitmap.Width, dot_w = 3, h = s.destBitmap.Height, dot_h = 3;
                //Don't duplicate writes.
                if (s.destBitmap.GetPixel(w - 1, h - 1) != Color.Red)
                {
                    if (w > dot_w && h > dot_h)
                    {
                        for (int y = 0; y < dot_h; y++)
                            for (int x = 0; x < dot_w; x++)
                                s.destBitmap.SetPixel(w - 1 - x, h - 1 - y, Color.Red);
                    }
                }
            }
            return RequestedAction.None;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        public string ProvideDiagnostics()
        {
            return LicenseDiagnosticsBanner + Result.ProvideDiagnostics();
        }
        public string ProvidePublicText()
        {
            return LicenseDiagnosticsBanner + Result.ProvidePublicDiagnostics();
        }

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Result;
            return cache == null ? mgr.GetIssues() : mgr.GetIssues().Concat(cache.GetIssues());
        }
    }

}
