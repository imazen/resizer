// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using Imazen.Common.Storage;

namespace ImageResizer.Plugins.Basic
{
    public class ImageInfoAPI : IPlugin, IQuerystringPlugin
    {
        private Config c;

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            this.c = c;
            return this;
        }

        private void Pipeline_PreHandleImage(IHttpModule sender, HttpContext context, IResponseArgs e)
        {
            var info = e.RewrittenQuerystring["getinfo"];
            if (string.IsNullOrEmpty(info)) return;

            var ra = e as ResponseArgs;
            e.ResponseHeaders.ContentType = "application/json; charset=utf-8";

            var d = new NameValueCollection();
            ra.ResizeImageToStream = new ResizeImageDelegate(delegate(Stream s)
            {
                try
                {
                    using (var src = ra.GetSourceImage())
                    {
                        var attemptFastMode = src.CanSeek;
                        var orig = attemptFastMode ? src.Position : 0;
                        var trySlowMode = !attemptFastMode;
                        if (attemptFastMode)
                            try
                            {
                                using (var i = Image.FromStream(src, false, false))
                                {
                                    d["width"] = i.Width.ToString();
                                    d["height"] = i.Height.ToString();
                                }
                            }
                            catch
                            {
                                trySlowMode = true;
                            }

                        if (trySlowMode)
                        {
                            if (attemptFastMode) src.Seek(orig, SeekOrigin.Begin);
                            using (var b = c.CurrentImageBuilder.LoadImage(src,
                                       new ResizeSettings(ra.RewrittenQuerystring)))
                            {
                                d["width"] = b.Width.ToString();
                                d["height"] = b.Height.ToString();
                            }
                        }

                        SimpleJson(s, d, e.RewrittenQuerystring["jsonp"]);
                    }
                }
                catch (FileNotFoundException)
                {
                    d["result"] = "404";
                    SimpleJson(s, d, e.RewrittenQuerystring["jsonp"]);
                }
                catch (BlobMissingException)
                {
                    d["result"] = "404";
                    SimpleJson(s, d, e.RewrittenQuerystring["jsonp"]);
                }
            });
        }

        private void SimpleJson(Stream target, NameValueCollection data, string callbackName)
        {
            var sw = new StreamWriter(target, System.Text.Encoding.UTF8);

            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(callbackName))
            {
                sb.Append(callbackName);
                sb.Append('(');
            }

            ;

            sb.Append('{');

            foreach (string key in data)
            {
                if (data[key] == null) continue;
                sb.Append('\'');
                sb.Append(key.Replace('\'', '_'));
                sb.Append("':'");
                sb.Append(data[key]);
                sb.AppendLine("',");
            }

            sb.Append('}');

            if (!string.IsNullOrEmpty(callbackName)) sb.Append(");");
            ;

            sw.Write(sb.ToString());
            sw.Flush();
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[] { "getinfo" };
        }
    }
}