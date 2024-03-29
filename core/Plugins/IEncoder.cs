// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Drawing;
using System.IO;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     An image encoder. Exposes methods for suitability checking, encoding, transparency compatibility checking, and
    ///     mime-type/extension calculation.
    /// </summary>
    [Obsolete("Cannot be used with Imageflow backend; relies on GDI Image object")]
    public interface IEncoder
    {
        /// <summary>
        ///     If the encoder can handle the requirements specified by 'settings' and 'source', it should return an encoder
        ///     instance.
        ///     If not, it should return null.
        /// </summary>
        /// <param name="settings">Request settings, like format, quality, colors, dither, etc.</param>
        /// <param name="original">
        ///     May be a Drawing.Image instance, a path, or null. To provide both, set Image.tag to the path. Helps the encoder
        ///     detect the original format if the format was not specified.
        ///     May also be used for palette generation hinting by some encoders.
        /// </param>
        /// <returns></returns>
        IEncoder CreateIfSuitable(ResizeSettings settings, object original);

        /// <summary>
        ///     Encodes the image to the specified stream
        /// </summary>
        /// <param name="i"></param>
        /// <param name="s"></param>
        void Write(Image i, Stream s);

        /// <summary>
        ///     True if the output format will support transparency as it is currently configured.
        /// </summary>
        bool SupportsTransparency { get; }

        /// <summary>
        ///     Returns the appropriate mime-time for the output format as currently configured.
        /// </summary>
        string MimeType { get; }

        /// <summary>
        ///     Returns a file extension appropriate for the output format as currently configured, without a leading dot.
        /// </summary>
        string Extension { get; }
    }
}