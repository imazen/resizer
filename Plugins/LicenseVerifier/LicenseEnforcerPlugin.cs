// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Licensing;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.LicenseVerifier
{
    /// <summary>
    ///     Responsible for displaying a red dot when licensing has failed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider,
        ILicenseDiagnosticsProvider
    {
        Config c;
        Computation cachedResult;
        readonly ILicenseManager mgr;
        ILicenseClock Clock { get; } = new RealClock();

        Computation Result
        {
            get {
                if (cachedResult?.ComputationExpires != null &&
                    cachedResult.ComputationExpires.Value < Clock.GetUtcNow()) {
                    cachedResult = null;
                }
                return cachedResult = cachedResult ??
                                      new Computation(c, ImazenPublicKeys.Production, c.configurationSectionIssues, mgr,
                                          Clock);
            }
        }

        public LicenseEnforcer() : this(LicenseManagerSingleton.Singleton) { }
        public LicenseEnforcer(ILicenseManager mgr) { this.mgr = mgr; }

        public string ProvideDiagnostics() => LicenseDiagnosticsBanner + Result.ProvideDiagnostics();

        public IEnumerable<IIssue> GetIssues()
        {
            var cache = Result;
            return cache == null ? mgr.GetIssues() : mgr.GetIssues().Concat(cache.GetIssues());
        }

        public string ProvidePublicText() => LicenseDiagnosticsBanner + Result.ProvidePublicDiagnostics();

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
            if (Result == null) {
                throw new ApplicationException("Failed to populate license result");
            }

            c.Plugins.add_plugin(this);

            // And don't forget a cache-breaker
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        bool ShouldDisplayDot(Config c, ImageState s)
        {
#pragma warning disable 0162
            // Only unreachable when compiled in DRM mode. 
            if (!EnforcementEnabled) {
                return false;
            }
#pragma warning restore 0162

            // For now, we only add dots during an active HTTP request. 
            if (c == null || c.configurationSectionIssues == null || HttpContext.Current == null) {
                return false;
            }

            return !Result.LicensedForRequestUrl(HttpContext.Current?.Request?.Url);
        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            // Server-side cachebreaker
            if (e.QueryString["red_dot"] != "true" && ShouldDisplayDot(c, null)) {
                e.QueryString["red_dot"] = "true";
            }
        }

        protected override RequestedAction PreFlushChanges(ImageState s)
        {
            if (s.destBitmap != null && ShouldDisplayDot(c, s)) {
                int w = s.destBitmap.Width, dot_w = 3, h = s.destBitmap.Height, dot_h = 3;
                //Don't duplicate writes.
                if (s.destBitmap.GetPixel(w - 1, h - 1) != Color.Red) {
                    if (w > dot_w && h > dot_h) {
                        for (var y = 0; y < dot_h; y++)
                        for (var x = 0; x < dot_w; x++) {
                            s.destBitmap.SetPixel(w - 1 - x, h - 1 - y, Color.Red);
                        }
                    }
                }
            }
            return RequestedAction.None;
        }
    }
}
