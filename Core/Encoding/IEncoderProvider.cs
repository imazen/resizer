using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Encoding {
    public interface IEncoderProvider {
        public IImageEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettingsCollection settings);
    }
}
