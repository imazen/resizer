// Copyright (c) 2012 Jason Morse and Nathanael Jones
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Resizing;
using ImageMagick;

namespace ImageResizer.Plugins.VectorRenderer
{
    /// <summary>
    ///   Ghostscript image resizer decoder capable of rendering postscript-based files to bitmaps.
    /// </summary>
    public class VectorRendererPlugin : BuilderExtension, IPlugin, IFileExtensionPlugin, IIssueProvider, IQuerystringPlugin
    {

        public VectorRendererPlugin()
        {
        }

        #region Fields

        private static readonly string[] _queryStringKeys = new[]
                                                            {
                                                                "width",
                                                                "height",
                                                            };
        private static readonly string[] _supportedExtensions = new[]
                                                                {
                                                                    ".eps",
                                                                    ".svg"
                                                                };
        #endregion

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Need to retain stream for bitmap backing store.")]
        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath)
        {
            try
            {
                var readSettings = new MagickReadSettings
                {
                    Density = new Density(300)
                };

                using (MagickImage image = new MagickImage(s, readSettings))
                {
                    image.Resize(new MagickGeometry(settings.Width, settings.Height));
                    return image.ToBitmap();
                }
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public IPlugin Install(Config c)
        {
            if (c != null)
            {
                c.Plugins.add_plugin(this);
            }
            return this;
        }

        public bool Uninstall(Config c)
        {
            if (c != null)
            {
                c.Plugins.remove_plugin(this);
            }
            return true;
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return Enumerable.Empty<IIssue>();
        }

        public IEnumerable<string> GetSupportedFileExtensions()
        {
            return _supportedExtensions;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return _queryStringKeys;
        }

  
    }
}