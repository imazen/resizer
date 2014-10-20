using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.Basic {
    public class AutoRotate:BuilderExtension, IPlugin,IQuerystringPlugin {
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "autorotate" };
        }

        protected override RequestedAction PostDecodeStream(ref Bitmap b, ResizeSettings settings) {

            if (!"true".Equals(settings["autorotate"], StringComparison.OrdinalIgnoreCase)) return RequestedAction.None;

            int propertyId = 0x0112;
            PropertyItem pi;
            try {
                pi = b.GetPropertyItem(propertyId);
            } catch (ArgumentException) {
                return RequestedAction.None;
            }
            if (pi == null) return RequestedAction.None;

            int total = 0;

            foreach (byte by in pi.Value) total += by; //Does not handle values larger than 255, but it doesn't need to, and is endian-agnostic.

            if (total == 8) b.RotateFlip(RotateFlipType.Rotate270FlipNone);
            if (total == 3) b.RotateFlip(RotateFlipType.Rotate180FlipNone);
            if (total == 6) b.RotateFlip(RotateFlipType.Rotate90FlipNone);

            if (total == 2) b.RotateFlip(RotateFlipType.RotateNoneFlipX);
            if (total == 4) b.RotateFlip(RotateFlipType.Rotate180FlipX);
            if (total == 5) b.RotateFlip(RotateFlipType.Rotate270FlipY);
            if (total == 7) b.RotateFlip(RotateFlipType.Rotate90FlipY);

            b.RemovePropertyItem(propertyId);

            return RequestedAction.None;
        }

    }
}
