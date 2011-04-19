/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Util;
using System.Drawing;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Adds drop shadow capabilities
    /// </summary>
    public class DropShadow : BuilderExtension, IPlugin, IQuerystringPlugin {

        protected override RequestedAction LayoutEffects(ImageState s) {
            if (base.LayoutEffects(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Clone last ring, then offset it.
            if (s.settings["shadowWidth"] != null) {
                float shadowWidth = Utils.getFloat(s.settings, "shadowWidth", 0);


                PointF shadowOffset = Utils.parsePointF(s.settings["shadowOffset"], new PointF(0, 0));

                //For drawing purposes later
                s.layout.AddInvisiblePolygon("shadowInner", PolygonMath.MovePoly(s.layout.LastRing.points, shadowOffset));

                //For layout purposes
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


            //parse shadow
            Color shadowColor = Utils.parseColor(s.settings["shadowColor"], Color.Transparent);
            int shadowWidth = Utils.getInt(s.settings, "shadowWidth", -1);

            //Skip on transparent or 0-width shadow
            if (shadowColor == Color.Transparent || shadowWidth <= 0) return RequestedAction.None;

            //Offsets may show inside the shadow - so we have to fix that
            s.destGraphics.FillPolygon(new SolidBrush(shadowColor),
                PolygonMath.InflatePoly(s.layout["shadowInner"], 1)); //Inflate 1 for FillPolgyon rounding errors.

            //Then we can draw the outer gradient
            Utils.DrawOuterGradient(s.destGraphics, s.layout["shadowInner"],
                             shadowColor, Color.Transparent, shadowWidth);

            return RequestedAction.None;
        }


        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "shadowColor", "shadowOffset", "shadowWidth" };
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
