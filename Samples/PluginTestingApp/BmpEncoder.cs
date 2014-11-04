/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Globalization;

namespace ImageResizerSamples
{
    /// <summary>
    /// Provides basic encoding functionality for Jpeg, png, and gif output. Allows adjustable Jpeg compression, but doesn't implement indexed PNG files or quantized GIF files.
    /// </summary>
    public class BmpEncoder : ImageResizer.Encoding.IEncoder, ImageResizer.Plugins.IPlugin
    {
        public ImageResizer.Encoding.IEncoder CreateIfSuitable(ImageResizer.ResizeSettings settings, object original)
        {
            if ("bmp".Equals(settings.Format, StringComparison.OrdinalIgnoreCase)) return new BmpEncoder();
            return null;
        }

        public void Write(Image i, Stream s)
        {
            i.Save(s, ImageFormat.Bmp);
        }

        public bool SupportsTransparency
        {
            get { return true; }
        }

        public string MimeType
        {
            get { return "image/bmp"; }
        }

        public string Extension
        {
            get { return "bmp"; }
        }

        public ImageResizer.Plugins.IPlugin Install(ImageResizer.Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(ImageResizer.Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}