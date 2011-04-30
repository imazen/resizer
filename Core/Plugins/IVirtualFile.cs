using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins {
    /// <summary>
    /// A virtual file.
    /// </summary>
    public interface IVirtualFile {
        string VirtualPath { get; }
        Stream Open();
    }
}
