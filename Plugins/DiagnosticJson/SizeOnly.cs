﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ImageResizer.Util;

namespace ImageResizer.Plugins.DiagnosticJson
{
    /// <summary>
    /// A size-only variant for PolyRect, since we have some cases
    /// where we don't need all the values.
    /// </summary>
    public class SizeOnly
    {
        public SizeOnly(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public SizeOnly(RectangleF rect)
            : this(rect.Width, rect.Height)
        { }

        public SizeOnly(Rectangle rect)
            : this(rect.Width, rect.Height)
        { }

        public SizeOnly(SizeF size)
            : this(size.Width, size.Height)
        { }

        public SizeOnly(Size size)
            : this(size.Width, size.Height)
        { }

        public float width { get; private set; }
        public float height { get; private set; }
    }
}
