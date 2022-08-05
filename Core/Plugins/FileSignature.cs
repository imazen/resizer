// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

namespace ImageResizer.Plugins
{
    public class FileSignature
    {
        public FileSignature(byte[] signature, string ext, string mime)
        {
            Signature = signature;
            PrimaryFileExtension = ext;
            MimeType = mime;
        }

        public byte[] Signature { get; protected set; }
        public string PrimaryFileExtension { get; protected set; }
        public string MimeType { get; protected set; }
    }
}