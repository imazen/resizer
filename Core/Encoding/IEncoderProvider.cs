using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Encoding {
    public interface IEncoderProvider {
        IEncoder GetEncoder(System.Drawing.Image originalImage, ResizeSettings settings);
    }
}
