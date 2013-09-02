using System;
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
