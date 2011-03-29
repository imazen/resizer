using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Resizing;
using System.Drawing;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Plugins {
    public class SizeLimitingModule:AbstractImageProcessor, IPlugin {
        public SizeLimitingModule(SizeLimits l) {
            this.l = l;
        }
        protected SizeLimits l;

        protected override void PostLayoutImage(ImageState s) {
            base.PostLayoutImage(s);

            SizeF box = s.layout.GetBoundingBox().Size;

            double wFactor = box.Width / l.ImageSize.Width;
            double hFactor = box.Height / l.ImageSize.Height;

            double scaleFactor = wFactor > hFactor ? wFactor : hFactor;
            if (scaleFactor > 1) {
                //The bounding box exceeds the ImageSize. 
                s.layout.Scale(1 / scaleFactor, new PointF(0, 0));
            }
        }

        public IPlugin Install(Config c) {
            throw new NotImplementedException();
        }

        public bool Uninstall(Config c) {
            throw new NotImplementedException();
        }

        public string ShortName {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> GetSupportedFileExtensions() {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            throw new NotImplementedException();
        }
    }
}
