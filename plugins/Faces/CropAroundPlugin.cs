// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.ExtensionMethods;
using ImageResizer.Util;

namespace ImageResizer.Plugins.CropAround {

    static class Extensions
    {
        public static IEnumerable<List<T>> SplitList<T>(this List<T> locations, int nSize = 30)
        {
            for (var i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }
    }

    struct SalientArea
    {
        public RectangleF Area { get; set; }
        public float Weight { get; set; }



        public static SalientArea[] FromQuery(ResizeSettings s)
        {
            var salientareas = s.GetList<float>("c.salientareas", null);
            if (salientareas != null) {
                if (salientareas.Length % 5 != 0) {
                    throw new ImageProcessingException(400,
                        "Incorrect number of values provided in c.salientareas (must be multiple of 5). Usage: &c.salientareas=x1,y1,x2,y2,weight,x1,y1,x2,y2,weight");
                }

                return salientareas.ToList()
                                   .SplitList(5)
                                   .Select(v => new SalientArea {
                                       Area = new RectangleF(v[0], v[1], v[2] - v[0], v[3] - v[1]),
                                       Weight = v[4]
                                   })
                                   .ToArray();
            }

            var focus = s.GetList<float>("c.focus", null);
            if (focus != null && focus.Length == 2) {
                return new[] {new SalientArea {Area = new RectangleF(focus[0], focus[1], 0, 0), Weight = 1}};
            }

            if (focus != null && focus.Length > 2 && focus.Length % 4 == 0) {
                return focus.ToList()
                            .SplitList(4)
                            .Select(v => new SalientArea {
                                Area = new RectangleF(v[0], v[1], v[2] - v[0], v[3] - v[1]),
                                Weight = 1
                            })
                            .ToArray();
            }
            return new SalientArea[0];
        }

        public static double IntersectVolume(RectangleF window, SalientArea[] regions)
        {
            double volume = 0;
            foreach (var r in regions) {
                var size = RectangleF.Intersect(r.Area, window).Size;
                volume += size.Width * size.Height * r.Weight;
            }
            return volume;
        }
    }

    /// <summary>
    /// Enables cropping based on a set of rectangles to preserve
    /// </summary>
    public class CropAroundPlugin:BuilderExtension, IPlugin,IQuerystringPlugin  {
     
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
            return new string[] { "c.focus", "c.salientareas", "c.zoom", "c.finalmode"};
        }

        protected override RequestedAction LayoutImage(ImageState s) {
            //Only activated if both width and height are specified, and mode=crop.
            if (s.settings.Mode != FitMode.Crop || s.settings.Width < 0 || s.settings.Height < 0) return RequestedAction.None;

            var finalMode = s.settings.Get("c.finalmode", FitMode.Pad);

            var regions = SalientArea.FromQuery(s.settings);
            if (regions.Length == 0) return RequestedAction.None;


            var a = new Aligner {
                Regions = regions,
                ImageSize = s.originalSize,
                TargetSize = new Size(s.settings.Width, s.settings.Height),
                NeverCropSalientArea = finalMode != FitMode.Crop,
                Zoom = regions[0].Area.Width > 0 && regions[0].Area.Height > 0 && s.settings.Get("c.zoom", false)
            };

            s.copyRect = a.GetCrop();

            s.ValidateCropping();

            //So, if we haven't met the aspect ratio yet, what mode will we pass on?
            s.settings.Mode = finalMode;

            return RequestedAction.None;
        }
        
    }


    class Aligner
    {
        /// Zoom cannot be true unless 1 or more focus regions have positive area
        internal bool Zoom { get; set; }

        /// Prevent cropping of any salient region, even if it prevents us from meeting the TargetSize aspect ratio.
        internal bool NeverCropSalientArea { get; set; }

        // Salient regions ordered by priority
        internal SalientArea[] Regions { get; set; }

        internal Size TargetSize { get; set; }

        internal Size ImageSize { get; set; }

        RectangleF SalientBoundingBox()
        {
            return PolygonMath.GetBoundingBox(Regions
                .SelectMany(r => new[] { r.Area.Location, new PointF(r.Area.Right, r.Area.Bottom) })
                .ToArray());
        }

        RectangleF GetZoomCrop()
        {
            RectangleF box = SalientBoundingBox();

            var bounds = new RectangleF(new PointF(0, 0), ImageSize);
            //Clip box to original image bounds
            box = PolygonMath.ClipRectangle(box, bounds);
            
            //Crop close
            var copySize = PolygonMath.ScaleOutside(box.Size, TargetSize);
            //Clip to bounds.
            return PolygonMath.ClipRectangle(PolygonMath.ExpandTo(box, copySize), bounds);
        }

        internal RectangleF GetCrop()
        {
            if (Zoom) return GetZoomCrop();


            var salientBounds = SalientBoundingBox();

            var idealCropSize = PolygonMath.ScaleInside(TargetSize, ImageSize);
            
            var fits = PolygonMath.FitsInside(salientBounds.Size, idealCropSize);

            // If there's no need to crop out a salient region, then center on the salient bounding box and align within the image bounds.
            if (fits) {
                return PolygonMath.AlignWithin(new RectangleF(PointF.Empty, idealCropSize),
                    new RectangleF(PointF.Empty, ImageSize), PolygonMath.Midpoint(salientBounds));
            }

            if (NeverCropSalientArea) {
                var compromiseCrop = new SizeF(Math.Max(salientBounds.Width, idealCropSize.Width),
                    Math.Max(salientBounds.Height, idealCropSize.Height));

                // Don't worry about 3 pixels or less, that's just annoying.
                if (compromiseCrop.Width - idealCropSize.Width <= 3 &&
                    compromiseCrop.Height - idealCropSize.Height <= 3) {
                    compromiseCrop = idealCropSize;
                }

                return PolygonMath.AlignWithin(new RectangleF(PointF.Empty, compromiseCrop),
                    new RectangleF(PointF.Empty, ImageSize), PolygonMath.Midpoint(salientBounds));

            } 

            // Find the least bad crop (brute force)
            var vertical = salientBounds.Height > idealCropSize.Height;

            var choiceCount = vertical
                ? salientBounds.Height - idealCropSize.Height
                : salientBounds.Width - idealCropSize.Width;

            var searchArea = new RectangleF(vertical ? 0 : salientBounds.X, vertical ? salientBounds.Y : 0, vertical ? idealCropSize.Width : salientBounds.Width, vertical ? salientBounds.Height : idealCropSize.Height);

            var initialWindow = PolygonMath.AlignWith(new RectangleF(PointF.Empty, idealCropSize), searchArea,
                ContentAlignment.TopLeft);

            double bestVolume = 0;
            var bestCrop = initialWindow;
           
            for (var i = 0; i < choiceCount; i++) {
                var window = initialWindow;
                window.Offset(vertical ? 0 : i, vertical ? i : 0);

                var volume = SalientArea.IntersectVolume(window, Regions);
                if (volume > bestVolume) {
                    bestVolume = volume;
                    bestCrop = window;
                }
            }
            return bestCrop;

        }

    }
}
