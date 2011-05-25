/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer {

    public enum ServerCacheMode {
        /// <summary>
        /// Request no caching of the resulting image.
        /// </summary>
        No,
        /// <summary>
        /// Request that the resulting image always be cached on the server, even if no modifications are made. 
        /// </summary>
        Always,
        /// <summary>
        /// Default caching behavior. Modified images are cached, unmodified images are not cached.
        /// </summary>
        Default

    }



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

    public enum CropUnits {
        /// <summary>
        /// Indicates the crop units are pixels on the original image.
        /// </summary>
        Pixels,
        /// <summary>
        /// Indicates that the crop values are between 0 and 1 and indicate the percent of the original image surface to start and stop at.
        /// </summary>
        Percentages
    }

}
