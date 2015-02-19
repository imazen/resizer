// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins
{
    public class FileSignature
    {
        public FileSignature(byte[] signature, string ext, string mime)
        {
            this.Signature = signature;
            this.PrimaryFileExtension = ext;
            this.MimeType = mime;
        }
        public byte[] Signature { get; protected set; }
        public string PrimaryFileExtension { get; protected set;}
        public string MimeType { get;protected set; }
    }
}
