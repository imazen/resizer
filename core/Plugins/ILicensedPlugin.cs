using System.Collections.Generic;

namespace ImageResizer.Plugins
{
    public interface ILicensedPlugin
    {
        IEnumerable<string> LicenseFeatureCodes { get; }
    }
}