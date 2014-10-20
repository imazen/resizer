using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    /// <summary>
    /// A virtual file to support IVirtualImageProvider
    /// </summary>
    public interface IVirtualFileAsync:IVirtualFile
    {

        /// <summary>
        /// Returns an opened stream to the file contents.
        /// </summary>
        /// <returns></returns>
        Task<Stream> OpenAsync();
    }
}
