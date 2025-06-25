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
using ImageResizer.Resizing;
using Imazen.Common.Licensing;
using System.IO;
using System.Web.Hosting;
using ImageResizer.Plugins.Basic;
using Imazen.Common.Issues;


[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Storage")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.AzureReader2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.HybridCache")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.Imageflow")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.RemoteReader")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.S3Reader2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.Security")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.UploadHelper")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.Core.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageResizer.Plugins.AllPlugins.Tests")]


namespace ImageResizer.Plugins.Licensing
{
    


    /// <summary>
    ///     Responsible for displaying a red dot when licensing has failed
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
#pragma warning disable CS0618
    internal class LicenseEnforcer : BuilderExtension, IPlugin, IIssueProvider,
        ILicenseDiagnosticsProvider, ILicenseConfig, IIssueReceiver, IDiagnosticsHeaderProvider
    {

        public static void EnsureInstalled(Configuration.Config c)
        {
            c.Plugins.GetOrInstall<LicenseEnforcer>();
        }
#pragma warning restore CS0618
        private readonly ILicenseManager _mgr;
        private readonly WatermarkRenderer _watermark = new WatermarkRenderer();
        private readonly IReadOnlyCollection<RSADecryptPublic> _trustedKeys = ImazenPublicKeys.Production;
   
        public event LicenseConfigEvent? LicensingChange;
        public event LicenseConfigEvent? Heartbeat;

        Configuration.Config? c;
        private Computation? _cachedResult;
        ILicenseClock Clock { get; }

        /// <summary>
        /// If null, c.configurationSectionIssues is used
        /// </summary>
        IIssueReceiver? PermanentIssueSink { get; set; }

        public Func<Uri?> GetCurrentRequestUrl { get; }

        Computation Result
        {
            get
            {
                if (_cachedResult?.ComputationExpires != null &&
                    _cachedResult.ComputationExpires.Value < Clock.GetUtcNow())
                {
                    _cachedResult = null;
                }

                return _cachedResult = _cachedResult ??
                                      new Computation(this, _trustedKeys,
                                          PermanentIssueSink ?? c?.configurationSectionIssues ?? new IssueSink("null"), _mgr,
                                          Clock, EnforcementEnabled());
            }
        }

        public LicenseEnforcer() : this(null)
        {
        }


        internal LicenseEnforcer(ILicenseManager mgr = null, Func<Uri> getCurrentRequestUrl = null, ILicenseClock? clock = null, IIssueReceiver? permanentIssueSink = null)
        {
            if (mgr == null)
            {
                var appDataFolder = AppDomain.CurrentDomain.GetData("DataDirectory") as string;

                this._mgr = LicenseManagerSingleton.GetOrCreateSingleton(
                    "resizer_", GetLicenseCacheDirectories());
            }
            else
            {
                this._mgr = mgr;
            }
            this.Clock = clock ?? new RealClock();
            this.PermanentIssueSink = permanentIssueSink;
            this.GetCurrentRequestUrl = getCurrentRequestUrl ?? (() => HttpContext.Current?.Request.Url);
        }


        public IEnumerable<IIssue> GetIssues() => _mgr.GetIssues().Concat(Result.GetIssues());


        public IPlugin Install(Configuration.Config config)
        {
            this.PermanentIssueSink ??= config.configurationSectionIssues;
            c = config;
            c.Plugins.add_plugin(this);
            
            // Ensure the LicenseManager can respond to heartbeats and license/licensee plugin additions for the config
            _mgr.MonitorLicenses(this);
            _mgr.MonitorHeartbeat(this);

            // Ensure our cache is appropriately invalidated when new licenses arrive, or when new licensed plugins are installed
            _cachedResult = null;
            _mgr.AddLicenseChangeHandler(this, (me, manager) => me._cachedResult = null);
            
            // And repopulated, so that errors show up.
            if (Result == null)
            {
                throw new ApplicationException("Failed to populate license result");
            }
            
            // And don't forget a cache-breaker
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;
            c.Plugins.LicensePluginsChange += LicensePluginsChangeHandler;

            MyOpenSourceProjectUrl = null; // We now offer free keys online, no need support this.
            // c.getNode("licenses")?.Attrs["myopensourceprojecturl"]?.Trim();

            return this;
        }

        public bool Uninstall(Configuration.Config config)
        {
            config.Plugins.remove_plugin(this);
            config.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            c.Plugins.LicensePluginsChange -= LicensePluginsChangeHandler;
            return true;
        }

        private string? MyOpenSourceProjectUrl { get; set; }
        
        private List<string>? _licenseKeys { get; set; }

        private List<string> UpdateLicenseKeysFromProviders()
        {
            _licenseKeys = c?.Plugins.GetAll<ILicenseProvider>()
                .SelectMany(p => p.GetLicenses())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList() ?? new List<string>();
            return _licenseKeys;
        }

        private List<KeyValuePair<string, string>>? _domainMappings = null;
        /// <summary>
        /// ImageResizer 5 does not support domain mappings or domain licensing
        /// YET, but we can keep this implemented in case it is needed in the future.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> GetDomainMappings()
        {
            return _domainMappings ?? UpdateDomainMappingsFromProviders();
        }
        private IEnumerable<KeyValuePair<string, string>> UpdateDomainMappingsFromProviders()
        {
            if (c == null)
            {
                // No config, so no mappings
                _domainMappings = new List<KeyValuePair<string, string>>();
                return _domainMappings;
            }
            var fromWebConfig = c.getNode("licenses")?.childrenByName("maphost")
                                    .Select(n => new KeyValuePair<string, string>(
                                        n.Attrs["from"]?.Trim().ToLowerInvariant(), 
                                        n.Attrs["to"]?.Trim().ToLowerInvariant()))
                                ?? Enumerable.Empty<KeyValuePair<string, string>>();
            var fromPluginsConfig = c.Plugins.GetLicensedDomainMappings();
            
            _domainMappings = fromWebConfig.Concat(fromPluginsConfig).ToList();
            return _domainMappings;
        }

        private void LicensePluginsChangeHandler(object o, Configuration.Config c)
        {
            _cachedResult = null;
            UpdateLicenseKeysFromProviders();
            UpdateDomainMappingsFromProviders();

            // Rebuild the result
            _cachedResult = null;
            foreach (var i in Result.GetIssues())
            {
                c.configurationSectionIssues?.AcceptIssue(i);
            }

            LicensingChange?.Invoke(this, this);
        }



        bool RequestNeedsEnforcementAction(HttpRequest? request)
        {
            if (!EnforcementEnabled() || Result.NoLicenseRequired)
            {
                // Open Source Project url set, or nothing licensed
                return false;
            }
            if (Result.LicensedForAll())
            {
                return false;
            }

            var requestUrl = GetCurrentRequestUrl != null ? GetCurrentRequestUrl() : request?.Url;
            if (requestUrl == null)
            {
                // No request URL, but we have a license for something, so we let it go.
                return !Result.LicensedForSomething();
            }
            return !Result.LicensedForRequestUrl(requestUrl);
        }

        bool ShouldWatermark(HttpRequest? request)
        {
            if (this.LicenseEnforcement == LicenseErrorAction.Watermark)
            {
                return RequestNeedsEnforcementAction(request);
            }
            return false;
        }

        void ThrowLicenseException(HttpRequest? request)
        {
            if (this.LicenseEnforcement == LicenseErrorAction.Http402 ||
                this.LicenseEnforcement == LicenseErrorAction.Http422)
            {
                if (RequestNeedsEnforcementAction(request))
                {
                    var statusCode = this.LicenseEnforcement == LicenseErrorAction.Http402
                        ? 402
                        : 422;
                    throw new LicenseException(statusCode,
                        "This request requires a valid license. Please visit " + LicensePurchaseUrl +
                        " to obtain a license for this software. You have set the licenseError to " + c?.Plugins.LicenseError);
                }
            }
        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext? context, ImageResizer.Configuration.IUrlEventArgs e)
        {
            // Server-side cache-breaker
            if (e.QueryString["red_dot"] != "true" && ShouldWatermark(context?.Request))
            {
                e.QueryString["red_dot"] = "true";
            }
            ThrowLicenseException(context?.Request);
            this.FireHeartbeat();
        }


        protected override void PreLoadImage(ref object source, ref string path, ref bool disposeSource,
            ref ResizeSettings settings)
        {
            ThrowLicenseException(HttpContext.Current?.Request);
        }

        /// <summary>
        /// Process.5(Render).18: Changes have been flushed to the bitmap, but the final bitmap has not been flipped yet.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction PostFlushChanges(ImageState s)
        {
            if (s.destBitmap == null || !RequestNeedsEnforcementAction(null))
            {
                // No watermarking requested, or no bitmap to draw on
                return RequestedAction.None;
            }
            _watermark.EnsureDrawn(s.destBitmap);
            return RequestedAction.None;
        }

        public object GetDiagnosticsProvider() => Result;


        public void AcceptIssue(IIssue i)
        {
            PermanentIssueSink?.AcceptIssue(i);
        }

        // Might be able to drop this..
        private List<List<string>> _requiredFeatureCodes = Enumerable.Empty<List<string>>().ToList();

        public IEnumerable<IEnumerable<string>> GetFeaturesUsed()
        {
            // Deep clone to prevent tampering
            return c.Plugins.GetAll<ILicensedPlugin>().Select(p => p.LicenseFeatureCodes.ToList()).Concat(_requiredFeatureCodes.Select(x => x.ToList())).ToList();
        }

        private void AddFeatureCodeRequired(string featureCode)
        {
            if (string.IsNullOrEmpty(featureCode))
            {
                throw new ArgumentNullException(nameof(featureCode));
            }

            _requiredFeatureCodes ??= new List<List<string>>();
            _requiredFeatureCodes.Add(new List<string> { featureCode });
        }

        /// <summary>
        /// One of the provided feature codes must be present in the license
        /// </summary>
        /// <param name="featureCodes"></param>
        private void AddFeatureCodeRequiredOneOf(string[] featureCodes)
        {
            _requiredFeatureCodes ??= new List<List<string>>();
            _requiredFeatureCodes.Add(featureCodes.ToList());
        }

        public IEnumerable<string> GetLicenses()
        {
            return _licenseKeys ?? UpdateLicenseKeysFromProviders();
        }

        public LicenseAccess LicenseScope
        {
            get
            {
                return c.Plugins.LicenseScope switch
                {
                    Configuration.LicenseAccess.Local => LicenseAccess.Local,
                    Configuration.LicenseAccess.ProcessReadonly => LicenseAccess.ProcessReadonly,
                    Configuration.LicenseAccess.ProcessShareonly => LicenseAccess.ProcessShareOnly,
                    Configuration.LicenseAccess.Process => LicenseAccess.Process,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public string ProvidePublicText()
        {
            return Result.ProvidePublicLicensesPage();
        }

        private void FireHeartbeat()
        {
            Heartbeat?.Invoke(this, this);
        }
        private bool EnforcementEnabled()
        {
            return GetLicenses().Any()
                   || string.IsNullOrEmpty(MyOpenSourceProjectUrl);
        }
   
        public LicenseErrorAction LicenseEnforcement
        {
            get
            {
                return c.Plugins.LicenseError switch
                {
                    Configuration.LicenseErrorAction.Watermark =>
                        LicenseErrorAction.Watermark,
                    Configuration.LicenseErrorAction.Exception =>
                        LicenseErrorAction.Http402,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public string EnforcementMethodMessage
        {
            get
            {
                return LicenseEnforcement switch
                {
                    Imazen.Common.Licensing.LicenseErrorAction.Watermark =>
                        "You are using EnforceLicenseWith.RedDotWatermark. If there is a licensing error, an red dot will be drawn on the bottom-right corner of each image. This can be set to EnforceLicenseWith.Http402Error instead (valuable if you are externally caching or storing result images.)",
                    Imazen.Common.Licensing.LicenseErrorAction.Http422 =>
                        "You are using EnforceLicenseWith.Http422Error. If there is a licensing error, HTTP status code 422 will be returned instead of serving the image. This can also be set to EnforceLicenseWith.RedDotWatermark.",
                    Imazen.Common.Licensing.LicenseErrorAction.Http402 =>
                        "You are using EnforceLicenseWith.Http402Error. If there is a licensing error, HTTP status code 402 will be returned instead of serving the image. This can also be set to EnforceLicenseWith.RedDotWatermark.",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        


        public string InvalidLicenseMessage =>
            "ImageResizer cannot validate your license; visit /resizer.debug or /resizer.license to troubleshoot.";
        
        public bool IsImageflow => false;
        public bool IsImageResizer => true;
        public string LicensePurchaseUrl => "https://www.imazen.io/pricing";
        public string AgplCompliantMessage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(MyOpenSourceProjectUrl))
                {
                    return "You have certified that you are complying with the AGPLv3 and have open-sourced your project at the following url:\r\n"
                           + MyOpenSourceProjectUrl;
                }
                else
                {
                    return "";
                }
            } 
        }

        
        private string[] GetLicenseCacheDirectories()
        {
            var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            var appPath = HostingEnvironment.ApplicationPhysicalPath;
            var candidates = (appPath != null
                    ? new[]
                    {
                        Path.Combine(appPath, "imagecache"),
                        dataDirectory,
                        Path.Combine(appPath, "App_Data"),
                        Path.GetTempPath()
                    }
                    : new[] { dataDirectory, Path.GetTempPath() }).Where(s => !string.IsNullOrEmpty(s)).Select(s =>
                    s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Distinct().ToArray();
            return candidates;
        }
        
        public string ProvideDiagnosticsHeader()
        {
            return Result.ProvideDiagnostics();
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