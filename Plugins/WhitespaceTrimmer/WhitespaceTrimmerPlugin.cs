using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using ImageResizer.Util;
using AForge.Imaging;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.WhitespaceTrimmer {
    public class WhitespaceTrimmerPlugin:BuilderExtension, IPlugin, IQuerystringPlugin {

        public WhitespaceTrimmerPlugin() {
        }

        public readonly string RectDataKey = "whitespacetrimbox";

        protected override RequestedAction LayoutImage(ImageState s) {
            if (s.sourceBitmap == null) return RequestedAction.None;

            //percentpadding. Percentage is 0-100, multiplied by the average of the width and height.
            double percentpadding = s.settings.Get<double>("trim.percentpadding", 0) / 100;
            
            int? threshold = s.settings.Get<int>("trim.threshold");
            if (threshold != null) {
                if (threshold < 0) threshold = 0; if (threshold > 255) threshold = 255;
                
                Rectangle box = new BoundingBoxFinder().FindBoxSobel(s.sourceBitmap, new Rectangle(0, 0, s.sourceBitmap.Width, s.sourceBitmap.Height), (byte)threshold);
                //Add padding
                int paddingPixels = (int)Math.Ceiling(percentpadding * (box.Width + box.Height) / 2);
                box.X = Math.Max(0, box.X - paddingPixels);
                box.Y= Math.Max(0, box.Y - paddingPixels);
                box.Width = Math.Min(s.sourceBitmap.Width, box.Width + paddingPixels * 2);
                box.Height = Math.Min(s.sourceBitmap.Height, box.Height + paddingPixels * 2);

                //Adjust s.originalSize so the layout occurs properly.
                s.originalSize = box.Size;
                s.Data[RectDataKey] = box;
            }
            return RequestedAction.None;

        }

        protected override RequestedAction PostLayoutImage(ImageState s) {
            //Now we offset copyRect so it works properly.
            if (s.Data.ContainsKey(RectDataKey)){
                Rectangle box = (Rectangle)s.Data[RectDataKey];
                s.copyRect = new RectangleF(s.copyRect.X + box.X, s.copyRect.Y + box.Y,s.copyRect.Width,s.copyRect.Height);
            }
            return RequestedAction.None;
        }



        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "trim.percentpadding", "trim.threshold" };
        }
    }
}
