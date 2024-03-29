// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Drawing;
using ImageResizer.Configuration;
using Imazen.Common.Issues;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Implements app-wide size Limits on image size
    /// </summary>
    public class SizeLimiting : BuilderExtension, IPlugin, IIssueProvider
    {
        public SizeLimiting()
        {
        }

        private SizeLimits limits = null;

        /// <summary>
        ///     The image and total size limits
        /// </summary>
        public SizeLimits Limits
        {
            get => limits;
            set => limits = value;
        }

        public IPlugin Install(Config c)
        {
            //Load SizeLimits
            limits = new SizeLimits(c);
            c.Plugins.AllPlugins.Add(this);
            c.Plugins.ImageBuilderExtensions.Add(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }


        protected override RequestedAction PostLayoutImage(ImageState s)
        {
            base.PostLayoutImage(s);

            if (!limits.HasImageSize) return RequestedAction.None; //Skip this unless we have image size limits

            var box = s.layout.GetBoundingBox().Size;

            double wFactor = box.Width / limits.ImageSize.Width;
            double hFactor = box.Height / limits.ImageSize.Height;

            var scaleFactor = wFactor > hFactor ? wFactor : hFactor;
            if (scaleFactor > 1)
                //The bounding box exceeds the ImageSize. Scale down until it fits.
                s.layout.Scale(1 / scaleFactor, new PointF(0, 0));

            return RequestedAction.None;
        }

        protected override RequestedAction PrepareDestinationBitmap(ImageState s)
        {
            //Insure the total size is acceptable
            limits.ValidateTotalSize(s.destSize);
            return RequestedAction.None;
        }


        public IEnumerable<IIssue> GetIssues()
        {
            if (limits != null) return limits.GetIssues();
            else return null;
        }
    }
}