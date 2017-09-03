// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    public delegate Task<Stream> ReadStreamAsyncDelegate();
    public delegate Task WriteResultAsyncDelegate(Stream outputStream, IAsyncResponsePlan plan);

    /// <summary>
    /// Encapsulates a response plan for responding to a request. 
    /// </summary>
    public interface IAsyncResponsePlan
    {
        string EstimatedContentType { get; set; }
        string EstimatedFileExtension { get; set; }

        string RequestCachingKey { get; }

        NameValueCollection RewrittenQuerystring { get; }

  
        ReadStreamAsyncDelegate OpenSourceStreamAsync{get;set;}

        WriteResultAsyncDelegate CreateAndWriteResultAsync { get; set; }


    }
}
