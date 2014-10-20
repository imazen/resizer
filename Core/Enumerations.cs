/* Copyright (c) 2014 Imazen See license.txt */
using System;
using ImageResizer.ExtensionMethods;

namespace ImageResizer {

    /// <summary>
    /// Output image formats
    /// </summary>
    public enum OutputFormat {
        /// <summary>
        /// Jpeg - the best format for photographs and thumbnails
        /// </summary>
        [EnumString("jpg",true)]
        [EnumString("jpe")]
        [EnumString("jif")]
        [EnumString("jfif")]
        [EnumString("jfi")]
        [EnumString("exif")]
        Jpeg = 8,
        /// <summary>
        /// The best format for transparent images and vector graphics
        /// </summary>
        Png = 16,
        /// <summary>
        /// A really poor choice for pretty much everything except animation
        /// </summary>
        Gif = 32
    }
           


    /// <summary>
    /// When to disk cache the image
    /// </summary>
    public enum ServerCacheMode {
        /// <summary>
        /// Request no disk caching of the resulting image.
        /// </summary>
        No,
        /// <summary>
        /// Request that the resulting image always be disk cached on the server, even if no modifications are made. 
        /// </summary>
        Always,
        /// <summary>
        /// Default caching behavior. Modified images are disk cached, unmodified images are not.
        /// </summary>
        Default

    }
    /// <summary>
    /// When to process and re-encode the file. 
    /// </summary>
    public enum ProcessWhen {
        /// <summary>
        /// Request no processing of the image or file (generally used with cache=always).
        /// The file contents will be used as-is.
        /// </summary>
        No,
        /// <summary>
        /// Require the file or image to be processed. Will cause non-image files to fail with an ImageCorruptedException.
        /// </summary>
        Always,
        /// <summary>
        /// Default. Only files with both a supported image extension and resizing settings specified in the querystring will be processed.
        /// </summary>
        Default
    }



    /// <summary>
    /// Controls whether the image is allowed to upscale, downscale, both, or if only the canvas gets to be upscaled.
    /// </summary>
    public enum ScaleMode {
        /// <summary>
        /// The default. Only downsamples images - never enlarges. If an image is smaller than 'width' and 'height', the image coordinates are used instead.
        /// </summary>
        [EnumString("down")]
        DownscaleOnly,
        /// <summary>
        /// Only upscales (zooms) images - never downsamples except to meet web.config restrictions. If an image is larger than 'width' and 'height', the image coordinates are used instead.
        /// </summary>
        [EnumString("up")]
        UpscaleOnly,
        /// <summary>
        /// Upscales and downscales images according to 'width' and 'height', within web.config restrictions.
        /// </summary>
        Both,
        /// <summary>
        /// When the image is smaller than the requested size, padding is added instead of stretching the image
        /// </summary>
        [EnumString("canvas")]
        UpscaleCanvas
    }

    /// <summary>
    /// [Deprecated (Use FitMode.Stretch)] Previously used to force an image to be 'stretched' to match a different aspect ratio.
    /// </summary>
    [Obsolete("Use Mode=FitMode.Stretch instead. Will be removed in V3.5 or V4.")]
    public enum StretchMode {
        /// <summary>
        /// [Deprecated (Use FitMode)] Maintains aspect ratio. Default.
        /// </summary>
        Proportionally,
        /// <summary>
        /// [Deprecated (Use FitMode)] Skews image to fit the new aspect ratio defined by 'width' and 'height'
        /// </summary>
        Fill
    }
    /// <summary>
    /// How to resolve aspect ratio differences between the requested size and the original image's size.
    /// </summary>
    public enum FitMode {
        /// <summary>
        /// Fit mode will be determined by other settings, such as &amp;carve=true, &amp;stretch=fill, and &amp;crop=auto. If none are specified and width/height are specified , &amp;mode=pad will be used. If maxwidth/maxheight are used, &amp;mode=max will be used.
        /// </summary>
        None,

        /// <summary>
        /// Width and height are considered maximum values. The resulting image may be smaller to maintain its aspect ratio. The image may also be smaller if the source image is smaller
        /// </summary>
        Max,
        /// <summary>
        /// Width and height are considered exact values - padding is used if there is an aspect ratio difference. Use &amp;anchor to override the MiddleCenter default.
        /// </summary>
        Pad,
        /// <summary>
        /// Width and height are considered exact values - cropping is used if there is an aspect ratio difference. Use &amp;anchor to override the MiddleCenter default.
        /// </summary>
        Crop,
        /// <summary>
        /// Width and height are considered exact values - seam carving is used if there is an aspect ratio difference. Requires the SeamCarving plugin to be installed, otherwise behaves like 'pad'.
        /// </summary>
        Carve,
        /// <summary>
        /// Width and height are considered exact values - if there is an aspect ratio difference, the image is stretched.
        /// </summary>
        Stretch,
       

    }

    [Obsolete("Obsolete. Use Mode=Crop to specify automatic cropping. Set CropTopLeft and CropTopRight to specify custom coordinates. Will be removed in V3.5 or V4.")]
    public enum CropMode {
        /// <summary>
        /// Default. No cropping - uses letterboxing if strecth=proportionally and both width and height are specified.
        /// </summary>
        None,
        /// <summary>
        /// [Deprecated] Use Mode=Crop. Minimally crops to preserve aspect ratio if stretch=proportionally.
        /// </summary>
        [Obsolete("Use Mode=Crop instead.")]
        Auto,
        /// <summary>
        /// Crops using the custom crop rectangle. Letterboxes if stretch=proportionally and both widht and height are specified.
        /// </summary>
        Custom
    }

    [Obsolete("Obsolete. Specify 0 for a crop unit to indicate source pixel coordinates.  Will be removed in V3.5 or V4.")]
    public enum CropUnits {
        /// <summary>
        /// Indicates the crop units are pixels on the original image.
        /// </summary>
        SourcePixels,
        /// <summary>
        /// Indicates a custom range is being specified for the values. Base 0.
        /// </summary>
        Custom


    }
    /// <summary>
    /// Horizontal and vertical flipping. Convertible to System.Drawing.RotateFlipType by casting.
    /// </summary>
    public enum FlipMode{
        /// <summary>
        /// No flipping
        /// </summary>
        None = 0,
        /// <summary>
        /// Flip horizontally
        /// </summary>
        [EnumString("h")]
        X = 4,
        /// <summary>
        /// Flip vertically (identical to 180 degree rotation)
        /// </summary>
        [EnumString("v")]
        Y = 6,
        /// <summary>
        /// Flip horizontally and vertically
        /// </summary>
        [EnumString("both")]
        XY = 2
    }


    /// <summary>
    /// Anchor location. Convertible to System.Drawing.ContentAlignment by casting.
    /// </summary>
    [Flags]
    public enum AnchorLocation {
        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned on the left.
        /// </summary>
        TopLeft = 1,

        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned at the center.
        /// </summary>
        TopCenter = 2,

        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned on the right.
        /// </summary>
        TopRight = 4,
        
        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned onthe left.
        /// </summary>
        MiddleLeft = 16,
        
        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned at the center.
        /// </summary>
        MiddleCenter = 32,

        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned on  the right.
        /// </summary>
        MiddleRight = 64,

        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned on the left.
        /// </summary>
        BottomLeft = 256,
        
        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned at  the center.
        /// </summary>
        BottomCenter = 512,

        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned on the right.
        /// </summary>
        BottomRight = 1024,
    }

    /// <summary>
    /// Modes of converting the image to Grayscale. GrayscaleMode.Y usually produces the best resuts
    /// </summary>
    public enum GrayscaleMode {
        [EnumString("false")]
        None = 0,
        /// <summary>
        /// The reccomended value. Y and NTSC are identical.
        /// </summary>
        [EnumString("true")]
        Y = 1,
        
        NTSC = 1,
        RY = 2,
        BT709= 3,

        /// <summary>
        /// Red, green, and blue are averaged to get the grayscale image. Usually produces poor results compared to other algorithms.
        /// </summary>
        Flat = 4
    }
    /// <summary>
    /// The Jpeg subsampling mode to use. Requires FreeImageEncoder, FreeImageBuilder, WicEncoder, or WicBuilder.
    /// </summary>
    public enum JpegSubsamplingMode {
        /// <summary>
        /// The encoder's default subsampling method will be used.
        /// </summary>
        Default = 0,
        /// <summary>
        /// 411 Subsampling - Only supported by FreeImageBuilder and FreeImageEncoder. Poor quality.
        /// </summary>
        [EnumString("411",true)]
        Y4Cb1Cr1 = 4,
        /// <summary>
        /// 420 Subsampling - Commonly used in H262 and H264. Low quality compared to 422 and 444. 
        /// </summary>
        [EnumString("420",true)]
        Y4Cb2Cr0 = 8,
        /// <summary>
        /// 422 Subsampling - Great balance of quality and file size, commonly used in high-end video formats.
        /// </summary>
        [EnumString("422",true)]
        Y4Cb2Cr2 = 16,
        /// <summary>
        /// 444 subsampling - Highest quality, largest file size.
        /// </summary>
        [EnumString("444",true)]
        HighestQuality =32,
        /// <summary>
        /// 444 subsampling - Highest quality, largest file size.
        /// </summary>
        [EnumString("444",true)]
        Y4Cb4Cr4 = 32
    }
    
}
