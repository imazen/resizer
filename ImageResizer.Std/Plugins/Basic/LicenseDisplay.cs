using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    /// Provides the /resizer.license page
    /// </summary>
    public class LicenseDisplay : EndpointPlugin
    {
        public LicenseDisplay()
        {
            this.EndpointMatchMethod = EndpointMatching.FilePathEndsWithOrdinalIgnoreCase;
            this.Endpoints = new[] {"/resizer.license", "/resizer.license.ashx"};
        }

        protected override string GenerateOutput(Config c) => GetPageText(c);

        public static string GetPageText(Config c)
        {
            return string.Join("\n\n",
                c.Plugins.GetAll<ILicenseDiagnosticsProvider>()
                .Concat(c.Plugins.GetAll<IDiagnosticsProviderFactory>().Select(f => f.GetDiagnosticsProvider() as ILicenseDiagnosticsProvider))
                .Where(p => p != null)
                .Select(p => p.ProvidePublicText())
                .Distinct());
        }
    }
}
