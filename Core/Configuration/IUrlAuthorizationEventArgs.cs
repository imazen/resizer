using System;
namespace ImageResizer.Configuration {
    public interface IUrlAuthorizationEventArgs {
        bool AllowAccess { get; set; }
        System.Collections.Specialized.NameValueCollection QueryString { get; }
        string VirtualPath { get; }
    }
}
