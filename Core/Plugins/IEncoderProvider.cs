/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Encoding {
    /// <summary>
    /// A provider (or selector) of IEncoder instances.
    /// </summary>
    public interface IEncoderProvider {
        /// <summary>
        /// Returns an encoder based on the provided settings and the source object
        /// </summary>
        /// <param name="settings">Request settings, like format, quality, colors, dither, etc.</param>
        /// <param name="original">May be a Drawing.Image instance, a path, or null. To provide both, set Image.tag to the path. Helps the encoder detect the original format if the format was not specified.</param>
        /// <returns></returns>
        IEncoder GetEncoder(ResizeSettings settings, object original);
    }
}
