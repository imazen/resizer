using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.Basic
{
    public interface ILicenseDiagnosticsProvider
    {

            string ProvidePublicText();
    }
}
