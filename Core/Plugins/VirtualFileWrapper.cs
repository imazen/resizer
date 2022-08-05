// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Web.Hosting;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     Wraps a standard ASP.NET VirtualFile instance in an IVirtualFile-compatible wrapper.
    /// </summary>
    public class VirtualFileWrapper : IVirtualFile
    {
        private VirtualFile _underlyingFile = null;

        /// <summary>
        ///     The VirtualFile instance this class is wrapping
        /// </summary>
        public VirtualFile UnderlyingFile
        {
            get => _underlyingFile;
            set => _underlyingFile = value;
        }

        public VirtualFileWrapper(VirtualFile fileToWrap)
        {
            UnderlyingFile = fileToWrap;
        }


        public string VirtualPath => UnderlyingFile.VirtualPath;

        public Stream Open()
        {
            return UnderlyingFile.Open();
        }
    }
}