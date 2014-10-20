/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Util;
using System.Drawing;
using ImageResizer.ExtensionMethods;
using System.Drawing.Drawing2D;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Adds drop shadow capabilities (shadowColor, shadowOffset, and shadowWidth commands)
    /// </summary>
    public class DropShadow : BuilderExtension, IPlugin, IQuerystringPlugin {

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "shadowColor", "shadowOffset", "shadowWidth" };
        }


        protected override RequestedAction LayoutEffects(ImageState s) {
            float shadowWidth = s.settings.Get<float>("shadowWidth", 0);
            if (shadowWidth != 0) {

                var offset = s.settings.GetList<float>("shadowOffset", 0, 2);
                PointF shadowOffset =  offset == null ? new PointF(0,0) : new PointF(offset[0], offset[1]);

                //Clone last ring, then offset it - provides the inner bounds of the shadow later
                s.layout.AddInvisiblePolygon("shadowInner", PolygonMath.MovePoly(s.layout.LastRing.points, shadowOffset));

                //Determine the outer bound of the shadow
                s.layout.AddRing("shadow", PolygonMath.InflatePoly(s.layout.LastRing.points, new float[]{
                    Math.Max(0, shadowWidth - shadowOffset.Y),
                    Math.Max(0, shadowWidth + shadowOffset.X),
                    Math.Max(0, shadowWidth + shadowOffset.Y),
                    Math.Max(0, shadowWidth - shadowOffset.X)
                }));
            }
            return RequestedAction.None;
        }

        protected override RequestedAction RenderEffects(ImageState s) {
            if (base.RenderEffects(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions


            if (s.destGraphics == null) return RequestedAction.None;

            //parse shadow
            Color shadowColor = ParseUtils.ParseColor(s.settings["shadowColor"], Color.Transparent);
            int shadowWidth = s.settings.Get<int>( "shadowWidth", -1);

            //Skip on transparent or 0-width shadow
            if (shadowColor == Color.Transparent || shadowWidth <= 0) return RequestedAction.None;

            using (Brush b = new SolidBrush(shadowColor)) {
                //Offsets may show inside the shadow - so we have to fix that
                s.destGraphics.FillPolygon(b,
                    PolygonMath.InflatePoly(s.layout["shadowInner"], 1)); //Inflate 1 for FillPolgyon rounding errors.
            }
            //Then we can draw the outer gradient
            DrawOuterGradient(s.destGraphics, s.layout["shadowInner"],
                             shadowColor, Color.Transparent, shadowWidth);

            return RequestedAction.None;
        }



        /// <summary>
        /// Draws a gradient around the specified polygon. Fades from 'inner' to 'outer' over a distance of 'width' pixels. 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="poly"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="width"></param>
        public static void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width) {

            PointF[,] corners = PolygonMath.GetCorners(poly, width);
            PointF[,] sides = PolygonMath.GetSides(poly, width);
            //Overlapping these causes darker areas... Dont use InflatePoly

            //Paint corners
            for (int i = 0; i <= corners.GetUpperBound(0); i++) {
                PointF[] pts = PolygonMath.GetSubArray(corners, i);
                using (Brush b = PolygonMath.GenerateRadialBrush(inner, outer, pts[0], width + 1)) {
                    g.FillPolygon(b, pts);
                }
            }
            //Paint sides
            for (int i = 0; i <= sides.GetUpperBound(0); i++) {
                PointF[] pts = PolygonMath.GetSubArray(sides, i);
                using (LinearGradientBrush b = new LinearGradientBrush(pts[3], pts[0], inner, outer)) {
                    b.SetSigmaBellShape(1);
                    b.WrapMode = WrapMode.TileFlipXY;
                    g.FillPolygon(b, pts);
                }
            }
        }
    }
}
