using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins {
    /// <summary>
    /// A virtual file to support IVirtualImageProvider
    /// </summary>
    public interface IVirtualFile {
        /// <summary>
        /// The virtual path of the file (relative to the domain, like /app/folder/file.ext)
        /// </summary>
        string VirtualPath { get; }
        /// <summary>
        /// Returns an opened stream to the file contents.
        /// </summary>
        /// <returns></returns>
        Stream Open();
    }
}
