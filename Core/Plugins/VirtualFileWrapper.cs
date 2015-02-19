// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Wraps a standard ASP.NET VirtualFile instance in an IVirtualFile-compatible wrapper.
    /// </summary>
    public class VirtualFileWrapper:IVirtualFile {
        private VirtualFile _underlyingFile = null;
        /// <summary>
        /// The VirtualFile instance this class is wrapping
        /// </summary>
        public VirtualFile UnderlyingFile {
            get { return _underlyingFile; }
            set { _underlyingFile = value; }
        }

        public VirtualFileWrapper(VirtualFile fileToWrap) {
            this.UnderlyingFile = fileToWrap;
        }



        public string VirtualPath {
            get { return UnderlyingFile.VirtualPath; }
        }

        public System.IO.Stream Open() {
            return UnderlyingFile.Open();
        }
    }
}
