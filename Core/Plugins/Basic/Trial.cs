/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration;
using System.Threading;
using System.Drawing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Can be used by plugins to implement 'trial version' functionality. Not currently used.
    /// </summary>
    public class Trial:BuilderExtension, IPlugin {
        public Trial() {
        }
        public static void InstallPermanent(){
            //Re-install every request if it is removed
            Config.Current.Pipeline.PreHandleImage -= new PreHandleImageEventHandler(Pipeline_PreHandleImage);
            Config.Current.Pipeline.PreHandleImage += new PreHandleImageEventHandler(Pipeline_PreHandleImage);
            //Install it.
            if (!Config.Current.Plugins.Has<Trial>()) new Trial().Install(Config.Current);
        }

        static void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            if (!Config.Current.Plugins.Has<Trial>()) new Trial().Install(Config.Current);
        }

        Config c;
        public IPlugin Install(Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        /// <summary>
        /// The Trial plugin cannot be removed using this method.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c) {
            return false; 
        }

        public enum TrialWatermarkMode {
            After500,
            Always,
            Randomly

        }
        private static int requestCount = 0;

        protected override RequestedAction PreFlushChanges(ImageState s) {
            if (s.destGraphics == null) return RequestedAction.None;


            Interlocked.Increment(ref requestCount); //Track request count

            string mode = c.get("trial.watermarkMode","After500");
            TrialWatermarkMode m = TrialWatermarkMode.After500;
            if ("always".Equals(mode, StringComparison.OrdinalIgnoreCase)) m = TrialWatermarkMode.Always;
            if ("randomly".Equals(mode, StringComparison.OrdinalIgnoreCase)) m = TrialWatermarkMode.Randomly;

            bool applyWatermark = (m == TrialWatermarkMode.Always);
            if (m == TrialWatermarkMode.After500 && requestCount > 500) applyWatermark = true;
            if (m == TrialWatermarkMode.Randomly) applyWatermark = (new Random(requestCount).Next(0, 41) < 10); //25% chance

            if (!applyWatermark) return RequestedAction.None;

            DrawString(PolygonMath.GetBoundingBox(s.layout["image"]), s.destGraphics, "Unlicensed", FontFamily.GenericSansSerif, Color.FromArgb(70, Color.White));

      

            return RequestedAction.None;
        }

      public virtual void DrawString(RectangleF area, Graphics g, String text, FontFamily ff, Color c)
        {
            SizeF size = g.MeasureString(text, new Font(ff, 32));
            double difX = (size.Width - area.Width) / -size.Width;

            double difY = (size.Height - area.Height) / -size.Height;
            float finalFontSize = 32 + (float)(32 * Math.Min(difX, difY));
            SizeF finalSize = g.MeasureString(text, new Font(ff, finalFontSize));

            g.DrawString(text, new Font(ff, finalFontSize), new SolidBrush(c),
                new PointF((area.Width - finalSize.Width) / 2 + area.Left, (area.Height - finalSize.Height) / 2 + area.Height));
            g.Flush();
        }
    }
}
