// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImageResizer.Plugins.DiagnosticJson
{
    public class AlternateResponseException : Exception
    {
        public AlternateResponseException(string message, string contentType, byte[] responseData, int statusCode = 200)
            : base(message)
        {
            this.ContentType = contentType;
            this.ResponseData = responseData;
            this.StatusCode = statusCode;
        }

        public byte[] ResponseData { get; private set; }
        
        public string ContentType { get; private set; }
        
        public int StatusCode { get; private set; }


        public static void InjectExceptionHandler(ImageResizer.Caching.ResponseArgs ra)
        {
            // Wrap the default ResizeImageToStream()
            var old = ra.ResizeImageToStream;
            ra.ResizeImageToStream = new ImageResizer.Caching.ResizeImageDelegate(delegate(Stream s)
            {
                // We *expect* an AlternateResponseException to be thrown,
                // that's why we injected this handler in the first place!
                try
                {
                    old(s);
                }
                catch (AlternateResponseException rce)
                {
                    ra.ResponseHeaders.ContentType = rce.ContentType;
                    s.Write(rce.ResponseData, 0, rce.ResponseData.Length);
                }
            });
        }
    }
}
