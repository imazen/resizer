// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins
{
    public interface IAsyncTyrantCache
    {
        bool CanProcess(HttpContext current, IAsyncResponsePlan e);
        Task ProcessAsync(HttpContext current, IAsyncResponsePlan e);
    }
}