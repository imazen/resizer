// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Web.Hosting;

namespace ImageResizer.Plugins
{
    public class VirtualFileShim : VirtualFile
    {
        private IVirtualFile f;

        public VirtualFileShim(IVirtualFile f) : base(f.VirtualPath)
        {
            this.f = f;
        }

        public override Stream Open()
        {
            return f.Open();
        }
    }
}