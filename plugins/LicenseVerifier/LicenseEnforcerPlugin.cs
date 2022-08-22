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
    partial class LicenseEnforcer<T> : BuilderExtension, IPlugin, IIssueProvider,
        IDiagnosticsProviderFactory
    {
        readonly ILicenseManager mgr;
        readonly WatermarkRenderer watermark = new WatermarkRenderer();
        readonly IReadOnlyCollection<RSADecryptPublic> trustedKeys = ImazenPublicKeys.Production;
        Config c;
        Computation cachedResult;
        ILicenseClock Clock { get; } = new RealClock();
        /// <summary>
        /// If null, c.configurationSectionIssues is used
        /// </summary>
        IIssueReceiver PermanentIssueSink { get; set; }
        public Func<Uri> GetCurrentRequestUrl { get; } = () => HttpContext.Current?.Request.Url;

        Computation Result
        {
            get {
                if (cachedResult?.ComputationExpires != null &&
                    cachedResult.ComputationExpires.Value < Clock.GetUtcNow()) {
                    cachedResult = null;
                }
                return cachedResult = cachedResult ??
                                      new Computation(c, trustedKeys, PermanentIssueSink ?? c.configurationSectionIssues, mgr,
                                          Clock, EnforcementEnabled());
            }
        }

        public LicenseEnforcer() : this(LicenseManagerSingleton.Singleton) { }

        internal LicenseEnforcer(ILicenseManager mgr)
        {
            this.mgr = mgr;

        }
        
        internal LicenseEnforcer(LicenseManagerSingleton mgr, IIssueReceiver permanentIssueSink, Func<Uri> getCurrentRequestUrl)
        {
            this.mgr = mgr;
            this.GetCurrentRequestUrl = getCurrentRequestUrl;
            Clock = mgr.Clock;
            PermanentIssueSink = permanentIssueSink;
            trustedKeys = mgr.TrustedKeys;

        }
        internal LicenseEnforcer(ILicenseManager mgr, Func<Uri> getCurrentRequestUrl, ILicenseClock clock, IReadOnlyCollection<RSADecryptPublic> trusted ) {
            this.mgr = mgr;
            this.Clock = clock;
            this.GetCurrentRequestUrl = getCurrentRequestUrl;
            trustedKeys = trusted;
        }

        public IEnumerable<IIssue> GetIssues() => mgr.GetIssues().Concat(Result.GetIssues());


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

        bool enforcementEnabled = false;
       
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public bool EnforcementEnabled() => Enforce || enforcementEnabled;

        public LicenseEnforcer<T> EnableEnforcement()
        {
            enforcementEnabled = true;
            return this;
        }

        /// <summary>
        /// Raises an exception if LicenseError == LicenseErrorAction.FailRequest
        /// </summary>
        /// <returns></returns>
        bool ShouldWatermark()
        {
            if (!EnforcementEnabled()) {
                return false;
            }

            // Skip when configurationSectionIssues != null
            if (c?.configurationSectionIssues == null) {
                return false;
            }
            var requestUrl = GetCurrentRequestUrl();

            var isLicensed = Result.LicensedForRequestUrl(requestUrl);
            if (isLicensed) {
                return false;
            }

            if (c.Plugins.LicenseError == LicenseErrorAction.Exception) {

                if (requestUrl == null && Result.LicensedForSomething()) {
                    return false;
                }
                throw new LicenseException(
                    "ImageResizer cannot validate your license; visit /resizer.debug to troubleshoot.");
            }

            // We only add dots during an active HTTP request (but we'll raise an exception anywhere)
            return requestUrl != null;
        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            // Server-side cachebreaker
            if (e.QueryString["red_dot"] != "true" && ShouldWatermark()) {
                e.QueryString["red_dot"] = "true";
            }
        }


        protected override void PreLoadImage(ref object source, ref string path, ref bool disposeSource,
                                             ref ResizeSettings settings)
        {
            if (c.Plugins.LicenseError == LicenseErrorAction.Exception) {
                ShouldWatermark(); // Fail early
            }
        }

        /// <summary>
        /// Process.5(Render).18: Changes have been flushed to the bitmap, but the final bitmap has not been flipped yet.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction PostFlushChanges(ImageState s)
        {
            if (s.destBitmap == null || !ShouldWatermark())
            {
                return RequestedAction.None;
            }
            watermark.EnsureDrawn(s.destBitmap);
            return RequestedAction.None;
        }

        public object GetDiagnosticsProvider() => Result;
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
