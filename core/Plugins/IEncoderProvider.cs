// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     A provider (or selector) of IEncoder instances.
    /// </summary>
    [Obsolete("Cannot be used with Imageflow backend; relies on GDI Image object")]
    public interface IEncoderProvider
    {
        /// <summary>
        ///     Returns an encoder based on the provided settings and the source object
        /// </summary>
        /// <param name="settings">Request settings, like format, quality, colors, dither, etc.</param>
        /// <param name="original">
        ///     May be a Drawing.Image instance, a path, or null. To provide both, set Image.tag to the path.
        ///     Helps the encoder detect the original format if the format was not specified.
        /// </param>
        /// <returns></returns>
        IEncoder GetEncoder(ResizeSettings settings, object original);
    }
}