using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Provides a way to modify settings before they reach the managed API. Does not execute early enough to affect disk caching, although that may change in a later version.
    /// </summary>
    public interface ISettingsModifier {
        /// <summary>
        /// Implementations should support being called on their own result multiple times without behavioral differences. Currently only executed in the managed API, too late to affect the disk cache, but that will probably change (it's likely all ISettingsModifiers will get executed twice, once after PostRewrite and once before the managed API executes). 
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        ResizeSettings Modify(ResizeSettings settings);
    }
}
