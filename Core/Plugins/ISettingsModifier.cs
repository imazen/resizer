using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    public interface ISettingsModifier {
        /// <summary>
        /// Implementations should support being called on their own result multiple result without behavioral differences.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        ResizeSettings Modify(ResizeSettings settings);
    }
}
