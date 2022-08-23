// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using ImageResizer.ExtensionMethods;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Allows gradients to be dynamically generated like so:
    ///     /gradient.png?color1=white&amp;color2=black&amp;angle=40&amp;width=20&amp;height=100
    /// </summary>
    public class Gradient : IPlugin, IQuerystringPlugin, IVirtualImageProvider, IVirtualImageProviderAsync
    {
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            return virtualPath.EndsWith("/gradient.png", StringComparison.OrdinalIgnoreCase);
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            return new GradientVirtualFile(queryString);
        }

        public Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            return Task.FromResult(FileExists(virtualPath, queryString));
        }

        public Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString)
        {
            return Task.FromResult<IVirtualFileAsync>(new GradientVirtualFile(queryString));
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[] { "color1", "color2", "angle", "width", "height" };
        }

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


        public class GradientVirtualFile : IVirtualFile, IVirtualBitmapFile, IVirtualFileSourceCacheKey,
            IVirtualFileAsync
        {
            public GradientVirtualFile(NameValueCollection query)
            {
                this.query = new ResizeSettings(query);
            }

            public string VirtualPath => "gradient.png";

            protected ResizeSettings query;

            public Stream Open()
            {
                var ms = new MemoryStream();
                using (var b = GetBitmap())
                {
                    b.Save(ms, ImageFormat.Png);
                }

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }

            public Task<Stream> OpenAsync()
            {
                return Task.FromResult<Stream>(Open());
            }


            public Bitmap GetBitmap()
            {
                Bitmap b = null;
                try
                {
                    var w = query.Width > 0 ? query.Width : query.MaxWidth > 0 ? query.MaxWidth : 8;
                    var h = query.Height > 0 ? query.Height : query.MaxHeight > 0 ? query.MaxHeight : 8;
                    var angle = query.Get<float>("angle", 0);
                    var c1 = ParseUtils.ParseColor(query["color1"], Color.White);
                    var c2 = ParseUtils.ParseColor(query["color2"], Color.Black);
                    b = new Bitmap(w, h);

                    using (var g = Graphics.FromImage(b))
                    using (Brush brush = new LinearGradientBrush(new Rectangle(0, 0, w, h), c1, c2, angle))
                    {
                        g.FillRectangle(brush, 0, 0, w, h);
                    }
                }
                catch
                {
                    if (b != null) b.Dispose();
                    throw;
                }

                return b;
            }

            public string GetCacheKey(bool includeModifiedDate)
            {
                return VirtualPath + PathUtils.BuildQueryString(query.Keep("width", "height", "w", "h", "maxwidth",
                    "maxheight", "angle", "color1", "color2"));
            }
        }
    }
}