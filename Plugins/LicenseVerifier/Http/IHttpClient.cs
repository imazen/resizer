using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.LicenseVerifier.Http {
    public interface IHttpClient {
        HttpResponse Send(HttpRequest httpRequest);
    }
}
