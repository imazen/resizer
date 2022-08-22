// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.DiagnosticJson
{
    public class LayoutInformation
    {
        public LayoutInformation(ImageState state)
        {
            this.instructions = new NameValueCollection(state.settings);

            this.sourceRect = new SizeOnly(state.originalSize);
            this.finalRect = new SizeOnly(state.destSize);

            this.imageSourcePoly = new PolyRect(state.copyRect);

            if (state.layout.ContainsRing("image"))
            {
                this.imageDestPoly = new PolyRect(state.layout["image"]);
            }

            if (state.layout.ContainsRing("imageArea"))
            {
                this.imageDestAreaPoly = new PolyRect(state.layout["imageArea"]);
            }

            // Check to see if sFlip/sRotate has altered the original raw image
            // rectangle.  The check must be the same as in
            // ImageBuilder.PrepareSourceBitmap().  Note that the adjustment
            // happens only when there's an actual bitmap, regardless of the
            // sFlip/sRotate settings.
            if (state.sourceBitmap != null &&
                (state.settings.SourceFlip != RotateFlipType.RotateNoneFlipNone ||
                !string.IsNullOrEmpty(state.settings["sRotate"])))
            {
                // We need to calculate the original rect/poly by *reversing* the
                // requested sFlip/sRotate.  We determine what the requested change
                // was, then calculate the reverse.
                var angle = state.settings.Get<double>("sRotate", 0);
                var flipRotate = (int)PolygonMath.CombineFlipAndRotate(state.settings.SourceFlip, angle);
                var copyPoly = PolygonMath.ToPoly(state.copyRect);
                var trueOriginalSize = state.originalSize;

                // The RotateFlipType values are ordered such that odd values
                // transpose the size of the rectangle, %4 gives the rotation
                // and /4 (=> 0 or 1) whether there's been an x-flip.  We can
                // use this to streamline our calculations.
                if (flipRotate % 2 == 1)
                {
                    trueOriginalSize = new Size(state.originalSize.Height, state.originalSize.Width);
                }

                this.preAdjustedSourceRect = new SizeOnly(trueOriginalSize);

                // Remember that the sFlip/sRotate change performed the rotation
                // first and then the flip, so we have to do the opposite to go
                // backwards.
                if (flipRotate / 4 == 1)
                {
                    copyPoly = PolygonMath.ScalePoints(copyPoly, -1, 1, PointF.Empty);
                    copyPoly = PolygonMath.MovePoly(copyPoly, new PointF(trueOriginalSize.Width, 0));
                }

                // It's possible to calculate a rotation-origin that will place
                // the original pre-sRotate (0,0) point back at (0,0) again...
                // but since it involves sqrt(), there would be rounding errors
                // that we should be able to avoid.  (We might, in fact, want to
                // avoid using PolygonMath entirely, and hand-map the points
                // backwards for accuracy.)
                switch (flipRotate % 4)
                {
                    case 0: // no rotation
                        // no-op!
                        break;

                    case 1: // 90 degrees, clockwise
                        copyPoly = PolygonMath.RotatePoly(copyPoly, -90);
                        copyPoly = PolygonMath.MovePoly(copyPoly, new PointF(0, trueOriginalSize.Height));
                        break;

                    case 2: // 180 degrees, clockwise
                        copyPoly = PolygonMath.RotatePoly(copyPoly, -180);
                        copyPoly = PolygonMath.MovePoly(copyPoly, new PointF(trueOriginalSize.Width, trueOriginalSize.Height));
                        break;

                    case 3: // 270 degrees, clockwise
                        copyPoly = PolygonMath.RotatePoly(copyPoly, -270);
                        copyPoly = PolygonMath.MovePoly(copyPoly, new PointF(trueOriginalSize.Width, 0));
                        break;
                }

                this.preAdjustedImageSourcePoly = new PolyRect(copyPoly);
            }
        }

        public NameValueCollection instructions { get; private set; }

        public SizeOnly sourceRect { get; private set; }
        public SizeOnly finalRect { get; private set; }

        public PolyRect imageSourcePoly { get; private set; }
        public PolyRect imageDestPoly { get; private set; }
        public PolyRect imageDestAreaPoly { get; private set; }

        public SizeOnly preAdjustedSourceRect { get; private set; }
        public PolyRect preAdjustedImageSourcePoly { get; private set; }
    }
}
