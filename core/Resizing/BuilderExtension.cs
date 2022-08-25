// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;

namespace ImageResizer.Resizing
{
    /// <summary>
    ///     Provides a usable base class that can be used to modify the behavior of ImageBuilder.
    ///     When registered with an ImageBuilder instance, the ImageBuilder will call the corresponding methods on the
    ///     extension prior to executing its own methods.
    /// </summary>
    [Obsolete("ImageResizer is migrating away from GDI. Suggest new features be added to Imageflow instead.")]
    public class BuilderExtension : AbstractImageProcessor
    {
    }
}