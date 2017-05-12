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
    // ReSharper disable once UnusedTypeParameter
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IDiagnosticsProvider, IIssueProvider,
        ILicenseDiagnosticsProvider
    {
        readonly ILicenseManager mgr;
        readonly WatermarkRenderer watermark = new WatermarkRenderer();
        Config c;
        Computation cachedResult;
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

        public IEnumerable<IIssue> GetIssues() => mgr.GetIssues().Concat(Result.GetIssues());

        public string ProvidePublicText() => LicenseDiagnosticsBanner + Result.ProvidePublicDiagnostics();

        public IPlugin Install(Config config)
        {
            c = config;

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

        public bool Uninstall(Config config)
        {
            config.Plugins.remove_plugin(this);
            config.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        bool ShouldWatermark()
        {
#pragma warning disable 0162
            // Only unreachable when compiled in DRM mode. 
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (!EnforcementEnabled) {
                // ReSharper disable once HeuristicUnreachableCode
                return false;
            }
#pragma warning restore 0162

            // For now, we only add dots during an active HTTP request, and when configurationSectionIssues != null
            if (c?.configurationSectionIssues == null || HttpContext.Current == null) {
                return false;
            }

            return !Result.LicensedForRequestUrl(HttpContext.Current?.Request.Url);
        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            // Server-side cachebreaker
            if (e.QueryString["red_dot"] != "true" && ShouldWatermark()) {
                e.QueryString["red_dot"] = "true";
            }
        }

        protected override RequestedAction PreFlushChanges(ImageState s)
        {
            if (s.destBitmap == null || !ShouldWatermark()) {
                return RequestedAction.None;
            }
            watermark.EnsureDrawn(s.destBitmap);
            return RequestedAction.None;
        }
    }

    class WatermarkRenderer
    {
        const int DotWidth = 3;
        const int DotHeight = 3;
        public Color DotColor { get; } = Color.Red;


        public void EnsureDrawn(Bitmap b)
        {
            if (b == null) {
                return;
            }
            var w = b.Width;
            var h = b.Height;
            //Don't duplicate writes; don't write to images <= 3x3
            if (w <= DotWidth || h <= DotHeight || b.GetPixel(w - 1, h - 1) == DotColor) {
                return;
            }

            for (var y = 0; y < DotHeight; y++) {
                for (var x = 0; x < DotWidth; x++) {
                    b.SetPixel(w - 1 - x, h - 1 - y, DotColor);
                }
            }
        }
    }
}
