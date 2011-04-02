using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer {

    public enum ScaleMode {
        /// <summary>
        /// The default. Only downsamples images - never enlarges. If an image is smaller than 'width' and 'height', the image coordinates are used instead.
        /// </summary>
        DownscaleOnly,
        /// <summary>
        /// Only upscales (zooms) images - never downsamples except to meet web.config restrictions. If an image is larger than 'width' and 'height', the image coordinates are used instead.
        /// </summary>
        UpscaleOnly,
        /// <summary>
        /// Upscales and downscales images according to 'width' and 'height', within web.config restrictions.
        /// </summary>
        Both,
        /// <summary>
        /// When the image is smaller than the requested size, padding is added instead of stretching the image
        /// </summary>
        UpscaleCanvas
    }

    public enum StretchMode {
        /// <summary>
        /// Maintains aspect ratio. Default.
        /// </summary>
        Proportionally,
        /// <summary>
        /// Skews image to fit the new aspect ratio defined by 'width' and 'height'
        /// </summary>
        Fill
    }
    public enum CropMode {
        /// <summary>
        /// Default. No cropping - uses letterboxing if strecth=proportionally and both width and height are specified.
        /// </summary>
        None,
        /// <summary>
        /// Minimally crops to preserve aspect ratio if stretch=proportionally.
        /// </summary>
        Auto,
        /// <summary>
        /// Crops using the custom crop rectangle. Letterboxes if stretch=proportionally and both widht and height are specified.
        /// </summary>
        Custom
    }

}
namespace fbs.ImageResizer.Resizing {
    public enum RequestedAction {
        /// <summary>
        /// Does nothing
        /// </summary>
        None = 0,
        /// <summary>
        /// Requests that ImageBuilder cancels the default logic of the method, and stop executing plugin calls for the method immediately.
        /// </summary>
        Cancel,




    }
}