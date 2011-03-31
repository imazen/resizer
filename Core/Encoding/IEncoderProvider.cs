using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Encoding {
    public interface IEncoderProvider {
        IEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettings settings);
    }
}
