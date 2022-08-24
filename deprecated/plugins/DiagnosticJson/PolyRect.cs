// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ImageResizer.Util;

namespace ImageResizer.Plugins.DiagnosticJson
{
    /// <summary>
    /// A JSON serializer-helper class to represent rectangles and polygons
    /// </summary>
    public class PolyRect
    {
        public PolyRect(float x, float y, float width, float height)
        {
            this.SetBounds(x, y, width, height);
            this.rect = true;
            var right = x + width;
            var bottom = y + height;
            this.points = new float[][] {
                new float[] { x, y },
                new float[] { right, y },
                new float[] { right, bottom },
                new float[] { x, bottom }
            };
        }

        public PolyRect(RectangleF rect)
            : this(rect.X, rect.Y, rect.Width, rect.Height)
        { }

        public PolyRect(Rectangle rect)
            : this(rect.X, rect.Y, rect.Width, rect.Height)
        { }

        public PolyRect(SizeF size)
            : this(0, 0, size.Width, size.Height)
        { }

        public PolyRect(Size size)
            : this(0, 0, size.Width, size.Height)
        { }

        public PolyRect(PointF[] points)
        {
            var rect = PolygonMath.GetBoundingBox(points);
            this.SetBounds(rect.X, rect.Y, rect.Width, rect.Height);

            // PolygonMath.IsUnrotated can tell us that the points follow a
            // particular pattern, but in order to represent 90-degree rotations
            // and flips, we want to consider them to be *non-rectangle* points.
            // Therefore, we use a much more strict definition: there can only
            // be 4 points, and they must be in the canonical order.
            var right = this.x + this.width;
            var bottom = this.y + this.height;

            this.rect = points.Length == 4 &&
                        points[0].X == this.x && points[0].Y == this.y &&
                        points[1].X == right && points[1].Y == this.y &&
                        points[2].X == right && points[2].Y == bottom &&
                        points[3].X == this.x && points[3].Y == bottom;

            this.points = points.Select(p => new float[] { p.X, p.Y }).ToArray();
        }

        public float x { get; private set; }
        public float y { get; private set; }
        public float width { get; private set; }
        public float height { get; private set; }
        public bool rect { get; private set; }
        public float[][] points { get; private set; }

        private void SetBounds(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
}
