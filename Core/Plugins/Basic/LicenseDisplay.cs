using System.Linq;
using System.Web;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Provides the /resizer.license page
    /// </summary>
    public class LicenseDisplay : EndpointPlugin
    {
        public LicenseDisplay()
        {
            EndpointMatchMethod = EndpointMatching.FilePathEndsWithOrdinalIgnoreCase;
            Endpoints = new[] { "/resizer.license", "/resizer.license.ashx" };
        }

        protected override string GenerateOutput(HttpContext context, Config c)
        {
            return GetPageText(c);
        }

        public static string GetPageText(Config c)
        {
            return string.Join("\n\n",
                c.Plugins.GetAll<ILicenseDiagnosticsProvider>()
                    .Concat(c.Plugins.GetAll<IDiagnosticsProviderFactory>()
                        .Select(f => f.GetDiagnosticsProvider() as ILicenseDiagnosticsProvider))
                    .Where(p => p != null)
                    .Select(p => p.ProvidePublicText())
                    .Distinct());
        }
    }
}