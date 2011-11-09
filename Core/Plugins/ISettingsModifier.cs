using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    public interface ISettingsModifier {
        ResizeSettings Modify(ResizeSettings settings);
    }
}
