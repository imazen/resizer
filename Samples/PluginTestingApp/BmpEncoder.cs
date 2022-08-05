/* Copyright (c) 2014 Imazen See license.txt */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Encoding;
using ImageResizer.Plugins;

namespace ImageResizerSamples
{
    /// <summary>
    ///     Provides basic encoding functionality for Jpeg, png, and gif output. Allows adjustable Jpeg compression, but
    ///     doesn't implement indexed PNG files or quantized GIF files.
    /// </summary>
    public class BmpEncoder : IEncoder, IPlugin
    {
        public IEncoder CreateIfSuitable(ResizeSettings settings, object original)
        {
            if ("bmp".Equals(settings.Format, StringComparison.OrdinalIgnoreCase)) return new BmpEncoder();
            return null;
        }

        public void Write(Image i, Stream s)
        {
            i.Save(s, ImageFormat.Bmp);
        }

        public bool SupportsTransparency => true;

        public string MimeType => "image/bmp";

        public string Extension => "bmp";

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}