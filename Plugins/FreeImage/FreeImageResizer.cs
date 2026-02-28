// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration.Issues;
using FreeImageAPI;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.Drawing;
using ImageResizer.Plugins.FreeImageResizer;
using ImageResizer.Plugins.FreeImageScaling;

/// Adds support for FreeImage resizing algorithms, which include CatmullRom, Lanczos3, bspline, box, bicubic, and bilinear filters.
/// The FreeImage library was discontinued in 2015 and has known security vulnerabilities. Migrate to the built-in GDI/WIC pipeline or a maintained alternative.
namespace ImageResizer.Plugins.FreeImageResizer {
    [Obsolete("The FreeImage library is discontinued and has known security vulnerabilities. Migrate to the built-in GDI/WIC pipeline.")]
    public class FreeImageResizerPlugin : FreeImageScalingPlugin { public FreeImageResizerPlugin() { } }
}

namespace ImageResizer.Plugins.FreeImageScaling {
    /// <summary>
    /// Adds support for scaling.
    /// The FreeImage library was discontinued in 2015 and has known security vulnerabilities. Migrate to the built-in GDI/WIC pipeline or a maintained alternative.
    /// </summary>
    [Obsolete("The FreeImage library is discontinued and has known security vulnerabilities. Migrate to the built-in GDI/WIC pipeline.")]
    public class FreeImageScalingPlugin : BuilderExtension, IPlugin, IQuerystringPlugin, IIssueProvider {
        /// <summary>
        /// Creates a new instance of the FreeImageScaling plugin.
        /// </summary>
        public FreeImageScalingPlugin() {
        }
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "fi.scale" };
        }

        public static FREE_IMAGE_FILTER ParseResizeAlgorithm(string sf, FREE_IMAGE_FILTER defaultValue, out bool valid){
            FREE_IMAGE_FILTER filter = defaultValue;

             valid = true;
             if ("bicubic".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BICUBIC;
             else if ("bilinear".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BILINEAR;
             else if ("box".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BOX;
             else if ("bspline".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BSPLINE;
             else if ("catmullrom".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_CATMULLROM;
             else if ("lanczos".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_LANCZOS3;
             else valid = false;

            return filter;
        }

        protected override RequestedAction PreRenderImage(ImageState s) {
            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            string sf = s.settings["fi.scale"];
            if (string.IsNullOrEmpty(sf)) return RequestedAction.None;
            bool validAlg = false;
            FREE_IMAGE_FILTER filter = ParseResizeAlgorithm(sf, FREE_IMAGE_FILTER.FILTER_CATMULLROM, out validAlg);
            if (!validAlg) throw new ImageProcessingException("The specified resizing filter '" + sf + "' did not match bicubic, bilinear, box, bspline, catmullrom, or lanczos.");

            

            //The minimum dimensions of the temporary bitmap.
            SizeF targetSize = PolygonMath.getParallelogramSize(s.layout["image"]);
            targetSize = new SizeF((float)Math.Ceiling(targetSize.Width), (float)Math.Ceiling(targetSize.Height));

            s.ApplyCropping();
            s.EnsurePreRenderBitmap();

            //The size of the temporary bitmap. 
            //We want it larger than the size we'll use on the final copy, so we never upscale it
            //- but we also want it as small as possible so processing is fast.
            SizeF tempSize = PolygonMath.ScaleOutside(targetSize, s.copyRect.Size);
            int tempWidth = (int)Math.Ceiling(tempSize.Width);
            int tempHeight = (int)Math.Ceiling(tempSize.Height);
            FIBITMAP src = FIBITMAP.Zero;
            FIBITMAP midway = FIBITMAP.Zero;
            try {
                var oldbit = s.preRenderBitmap ?? s.sourceBitmap;
                //Crop if needed, Convert, scale, then convert back.
                src = FreeImage.CreateFromBitmap(oldbit);
                
                midway = FreeImage.Rescale(src, tempWidth, tempHeight, filter);
                FreeImage.UnloadEx(ref src);
                //Clear the old pre-rendered image if needed
                if (s.preRenderBitmap != null) s.preRenderBitmap.Dispose();
                //Reassign the pre-rendered image
                s.preRenderBitmap = FreeImage.GetBitmap(midway);
                s.copyRect = new RectangleF(0, 0, s.preRenderBitmap.Width, s.preRenderBitmap.Height);
                FreeImage.UnloadEx(ref midway);
                s.preRenderBitmap.MakeTransparent();

            } finally {
                if (!src.IsNull) FreeImage.UnloadEx(ref src);
                if (!midway.IsNull) FreeImage.UnloadEx(ref midway);
            }
            

            return RequestedAction.Cancel;
        }

        /// <summary>
        /// Returns deprecation and availability issues for the FreeImage plugin.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            issues.Add(new Issue("FreeImageScaling: the FreeImage library was discontinued in 2015 and has known security vulnerabilities (CVEs). " +
                "Remove all FreeImage plugins and migrate to the built-in GDI/WIC pipeline or a maintained alternative.",
                "FreeImage has not received security patches since 2015. Continued use exposes your application to known image parsing vulnerabilities. " +
                "Remove the FreeImageBuilder, FreeImageDecoder, FreeImageEncoder, and FreeImageScaling plugins from your configuration.",
                IssueSeverity.Critical));
            return issues;
        }
    }
}
