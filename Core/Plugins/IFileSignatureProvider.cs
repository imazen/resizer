using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins
{
    public interface IFileSignatureProvider
    {

        IEnumerable<FileSignature> GetSignatures();
    }
}
