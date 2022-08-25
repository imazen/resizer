// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Specialized;
using ImageResizer.Collections;
using ImageResizer.ExtensionMethods;
using ImageResizer.Util;

namespace ImageResizer
{
    /// <summary>
    ///     A name/value collection of image processing instructions. The successor to ResizeSettings.
    ///     Just because a key doesn't have a property wrapper doesn't mean you can't use it. i["key"] = value; isn't that
    ///     scary.
    /// </summary>
    public class Instructions : QuerystringBase<Instructions>
    {
        /*
         * A list of commands not implemented by this class, along with justifications
         * 
         * maxwidth/maxheight - These will eventually be deprecated in favor of width=x;height=y;mode=max. 
         * Offering multiple variables for width and height leads to unexpected results, confusion, and consistency issues.
         * 
         * color1,color2, angle - Justification - Commands are specific to one infrequently used plugin: Gradient
         * speed - The SpeedOrQuality plugin doesn't have a great reason for existence, and is still alpha
         * a.oilpainting, a.sobel,  a.threshold, a.canny, a.equalize, a.posterize - Commands have neither been finalized nor confirmed.
         * fi.scale - Specific to two very infrequently used plugins: FreeImageBuilder and FreeImageResizer
         * preservePalette - Specific to the PrettyGifs plugin, and not well tested. *TODO*
         * jpeg.progressive - Supported by Imageflow, but not GDI, WIC, or WPF
         * memcache - This feature is pre-alpha
         * dpi - This feature is only useful if the user downloads the image before printing it. Lots of confusion around DPI, need to find a way to make it obvious. Perhaps naming it PrintDPI?
         * 
         * 
         */

        /// <summary>
        ///     Returns a human-friendly representation of the instruction set. Not suitable for URL usage; use ToQueryString() for
        ///     that.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return PathUtils.BuildSemicolonQueryString(this, false);
        }

        /// <summary>
        ///     Returns a URL-safe querystring containing the instruction set
        /// </summary>
        /// <returns></returns>
        public string ToQueryString()
        {
            return PathUtils.BuildQueryString(this, true);
        }

        /// <summary>
        ///     Creates an empty instructions collection.
        /// </summary>
        public Instructions() : base()
        {
        }

        /// <summary>
        ///     Copies the specified collection into a new Instructions instance.
        /// </summary>
        /// <param name="col"></param>
        public Instructions(NameValueCollection col) : base(col)
        {
        }

        /// <summary>
        ///     Parses the specified querystring into name/value pairs. Supports standard and semicolon syntaxes. The most readable
        ///     format is 'key=value;key2=value2' Discards everything after the first '#' character as a URL fragment.
        /// </summary>
        /// <param name="queryString"></param>
        public Instructions(string queryString) : base(PathUtils.ParseQueryStringFriendlyAllowSemicolons(queryString))
        {
        }


        /// <summary>
        ///     The width in pixels to constrain the image to. See 'Mode' and 'Scale' for constraint logic.
        /// </summary>
        public int? Width
        {
            get => Get<int>("width", Get<int>("w"));
            set
            {
                Set<int>("width", value);
                Remove("w");
            }
        }

        /// <summary>
        ///     The height in pixels to constrain the image to. See 'Mode' and 'Scale' for constraint logic.
        /// </summary>
        public int? Height
        {
            get => Get<int>("height", Get<int>("h"));
            set
            {
                Set<int>("height", value);
                Remove("h");
            }
        }

        /// <summary>
        ///     The fit mode to use when both Width and Height are specified. Defaults to Pad.
        /// </summary>
        public FitMode? Mode
        {
            get => Get<FitMode>("mode");
            set => Set<FitMode>("mode", value);
        }

        /// <summary>
        ///     The alignment to use when cropping or padding the image automatically. Defaults to MiddleCenter.
        /// </summary>
        public AnchorLocation? Anchor
        {
            get => Get<AnchorLocation>("anchor");
            set => Set<AnchorLocation>("anchor", value);
        }

        /// <summary>
        ///     Flip instruction to perform immediately after loading source image. Maps to 'sflip' and 'sourceFlip'.
        /// </summary>
        public FlipMode? SourceFlip
        {
            get => Get<FlipMode>("sflip", Get<FlipMode>("sourceFlip"));
            set
            {
                Set<FlipMode>("sflip", value);
                Remove("sourceFlip");
            }
        }

        /// <summary>
        ///     Flip instruction to perform after rendering is complete
        /// </summary>
        public FlipMode? FinalFlip
        {
            get => Get<FlipMode>("flip");
            set => Set<FlipMode>("flip", value);
        }

        /// <summary>
        ///     Control how upscaling is performed. Defaults to DownscaleOnly.
        /// </summary>
        public ScaleMode? Scale
        {
            get => Get<ScaleMode>("scale");
            set => Set<ScaleMode>("scale", value);
        }

        /// <summary>
        ///     Allows disk caching to be forced or prevented.
        /// </summary>
        public ServerCacheMode? Cache
        {
            get => Get<ServerCacheMode>("cache");
            set => Set<ServerCacheMode>("cache", value);
        }

        /// <summary>
        ///     Allows processing to be forced or prevented.
        /// </summary>
        public ProcessWhen? Process
        {
            get => Get<ProcessWhen>("process",
                this["useresizingpipeline"] != null ? new Nullable<ProcessWhen>(ProcessWhen.Always) : null);
            set
            {
                Set<ProcessWhen>("process", value);
                Remove("useresizingpipeline");
            }
        }

        /// <summary>
        /// Obsolete - not supported with Imageflow backend
        ///     The frame of the animated GIF to display. 1-based
        /// </summary>
        [Obsolete("This command is not supported with Imageflow - animated gifs are supported ")]
        public int? Frame
        {
            get => Get<int>("frame");
            set => Set<int>("frame", value);
        }

        /// <summary>
        /// Obsolete
        ///
        /// The page of the TIFF file to display. 1-based
        /// </summary>
        [Obsolete("TIFF support is not provided by most backends")]
        public int? Page
        {
            get => Get<int>("page");
            set => Set<int>("page", value);
        }

        /// <summary>
        ///     Determines JPEG encoding quality. Maps to 'quality' setting.
        /// </summary>
        public int? JpegQuality
        {
            get => Get<int>("quality");
            set => Set<int>("quality", value);
        }

        /// <summary>
        ///     Maps to 'subsampling'. Requires encoder=wic|freeimage or builder=wic|freeimage to take effect. Not supported by the
        ///     GDI pipeline.
        /// </summary>
        public JpegSubsamplingMode? JpegSubsampling
        {
            get => Get<JpegSubsamplingMode>("subsampling");
            set => Set<JpegSubsamplingMode>("subsampling", value);
        }


        /// <summary>
        /// Ignored; handled automatically by modern backends.
        /// </summary>
        [Obsolete("Ignored by all modern backends as they manage this automatically")]
        public byte? PaletteSize
        {
            get => Get<byte>("colors");
            set => Set<byte>("colors", value);
        }

        /// <summary>
        ///     A multiplier to apply to all sizing settings (still obeys Scale=down, though). Useful when you need to apply a
        ///     page-wide scaling factor, such as for mobile devices.
        /// </summary>
        public double? Zoom
        {
            get => Get<double>("zoom");
            set => Set<double>("zoom", value);
        }

        /// <summary>
        ///     Defines the horizontal width of the crop rectangle's coordinate space. For example, setting this to 100 makes the
        ///     crop X1 and X2 values percentages of the image width.
        /// </summary>
        public double? CropXUnits
        {
            get => Get<double>("cropxunits");
            set => Set<double>("cropxunits", value);
        }

        /// <summary>
        ///     Defines the vertical height of the crop rectangle's coordinate space. For example, setting this to 100 makes the
        ///     crop Y1 and Y1 values percentages of the image height.
        /// </summary>
        public double? CropYUnits
        {
            get => Get<double>("cropyunits");
            set => Set<double>("cropyunits", value);
        }

        /// <summary>
        ///     An X1,Y1,X2,Y2 array of coordinates. Unless CropXUnits and CropYUnits are specified, these are in the coordinate
        ///     space of the original image.
        /// </summary>
        public double[] CropRectangle
        {
            get => this.GetList<double>("crop", 0, 4);
            set => this.SetList("crop", value, true, 4);
        }


        /// <summary>
        /// Automatically rotates images based on gravity sensor data embedded in Exif.
        /// Defaults to true in V5 but not V4 (Also, ignored by Imageflow backend as images are always autortated there)
        /// 
        /// </summary>
        public bool? AutoRotate
        {
            get => Get<bool>("autorotate");
            set => Set<bool>("autorotate", value);
        }

        /// <summary>
        ///     Maps to 'srotate'. Rotates the source image prior to processing. Only 90 degree angles are currently supported.
        /// </summary>
        public double? SourceRotate
        {
            get => Get<double>("srotate");
            set => Set<double>("srotate", value);
        }

        /// <summary>
        /// Must be multiple of 90 degrees.  Rotates the image during rendering.
        /// </summary>
        public double? Rotate
        {
            get => Get<double>("rotate");
            set => Set<double>("rotate", value);
        }

        /// <summary>
        ///     Use 'OutputFormat' unless you need a custom value. Determines the format and encoding of the output image.
        /// </summary>
        public string Format
        {
            get => string.IsNullOrEmpty(this["format"]) ? this["thumbnail"] : this["format"];
            set
            {
                this["format"] = value;
                Remove("thumbnail");
            }
        }

        /// <summary>
        ///     Selects the image encoding format. Maps to 'format'. Returns null if the format is unspecified or if it isn't
        ///     defined in the enumeration.
        /// </summary>
        public OutputFormat? OutputFormat
        {
            get => Get<OutputFormat>("format", Get<OutputFormat>("thumbnail"));
            set
            {
                Set<OutputFormat>("format", value);
                Remove("thumbnail");
            }
        }

        /// <summary>
        /// If true, the image's ICC profile will be discarded instead of being evaluated. 
        /// </summary>
        public bool? IgnoreICC
        {
            get => Get<bool>("ignoreicc");
            set => Set<bool>("ignoreicc", value);
        }

        /// <summary>
        ///     The fallback image to redirect to if the original image doesn't exist. Must be the name of a pre-defined 404 image
        ///     or a filename in the default 404 images directory.
        ///     Requires the Image404 plugin to be installed.
        /// </summary>
        public string FallbackImage
        {
            get => this["404"];
            set => this["404"] = value;
        }

        /// <summary>
        ///     The color of margin and padding regions. Defaults to Transparent, or White (when JPEG is the selected output
        ///     format).
        /// </summary>
        public string BackgroundColor
        {
            get => this["bgcolor"];
            set => this["bgcolor"] = value;
        }

        /// <summary>
        ///     Defaults to 'bgcolor'. Allows a separate color to be used for padding areas vs. margins.
        /// </summary>
        [Obsolete("Use CSS to add padding, margins, borders to image; these are no longer supported due to little usage")]
        public string PaddingColor
        {
            get => this["paddingcolor"];
            set => this["paddingcolor"] = value;
        }

        /// <summary>
        ///     The color to draw the border with, if a border width is specified.
        /// </summary>
        [Obsolete("Use CSS to add margins and borders to image; these are no longer supported due to little usage")]
        public string BorderColor
        {
            get => this["bordercolor"];
            set => this["bordercolor"] = value;
        }

        /// <summary>
        ///     The name of a pre-defined preset, or a comma-delimited list of presets to apply. These may overwrite other
        ///     settings. Requires the Presets plugin.
        /// </summary>
        public string Preset
        {
            get => this["preset"];
            set => this["preset"] = value;
        }

        /// <summary>
        ///     The name of a pre-defined watermark layer or group from Web.config, or a comma-delimited list of names. Requires
        ///     the Watermark plugin.
        /// </summary>
        public string Watermark
        {
            get => this["watermark"];
            set => this["watermark"] = value;
        }


        /// <summary>
        ///     Applies a Negative filter to the image. Requires the SimpleFilters plugin
        /// </summary>
        public bool? Invert
        {
            get => Get<bool>("s.invert");
            set => Set<bool>("s.invert", value);
        }


        /// <summary>
        ///     Applies a Sepia filter to the image. Requires the SimpleFilters plugin
        /// </summary>
        public bool? Sepia
        {
            get => Get<bool>("s.sepia");
            set => Set<bool>("s.sepia", value);
        }

        /// <summary>
        ///     Applies the specified kind of grayscale filter to the image. Requires the SimpleFilters plugin
        /// </summary>
        public GrayscaleMode? Grayscale
        {
            get => Get<GrayscaleMode>("s.grayscale");
            set => Set<GrayscaleMode>("s.grayscale", value);
        }

        /// <summary>
        ///     Value between 0 and 1. Makes the rendered image transparent. Does not affect borders or background colors - those
        ///     accept 4-byte colors with alpha channels, however.
        ///     Requires the SimpleFilters plugin. Unless the output format is PNG, the image will be blended against white or the
        ///     background color.
        /// </summary>
        public double? Alpha
        {
            get => Get<double>("s.alpha");
            set => Set<double>("s.alpha", value);
        }

        /// <summary>
        ///     -1..1 Adjust the brightness of the image. Requires the SimpleFilters plugin
        /// </summary>
        public double? Brightness
        {
            get => Get<double>("s.brightness");
            set => Set<double>("s.brightness", value);
        }

        /// <summary>
        ///     -1..1 Adjust the contrast of the image. Requires the SimpleFilters plugin
        /// </summary>
        public double? Contrast
        {
            get => Get<double>("s.contrast");
            set => Set<double>("s.contrast", value);
        }

        /// <summary>
        ///     -1..1 Adjust the saturation of the image. Requires the SimpleFilters plugin
        /// </summary>
        public double? Saturation
        {
            get => Get<double>("s.saturation");
            set => Set<double>("s.saturation", value);
        }

        /// <summary>
        ///     Setting this enables automatic whitespace trimming using an energy function. 50 is safe, even 255 rarely cuts
        ///     anything off except a shadow. Set TrimPadding to pad the result slightly and improve appearance.
        ///     Requires the WhitespaceTrimmer plugin.
        /// </summary>
        public byte? TrimThreshold
        {
            get => Get<byte>("trim.threshold");
            set => Set<byte>("trim.threshold", value);
        }

        /// <summary>
        ///     Set TrimThreshold first. This specifies a percentage of the image size to 'add' to the crop rectangle. Setting to
        ///     0.5 or 1 usually produces good results.
        ///     Requires the WhitespaceTrimmer plugin.
        /// </summary>
        public double? TrimPadding
        {
            get => Get<double>("trim.percentpadding");
            set => Set<double>("trim.percentpadding", value);
        }

        /// <summary>
        ///     Gaussian Blur. Requires the AdvancedFilters plugin.
        /// </summary>
        [Obsolete("AdvancedFilters is no longer supported")]
        public double? Blur
        {
            get => Get<double>("a.blur");
            set => Set<double>("a.blur", value);
        }

        /// <summary>
        ///     Unsharp Mask. Requires the AdvancedFilters plugin.
        /// </summary>
        [Obsolete("AdvancedFilters is no longer supported; use f.sharpen instead of a.sharpen for a performant alternative")]
        public double? Sharpen
        {
            get => Get<double>("a.sharpen");
            set => Set<double>("a.sharpen", value);
        }

        /// <summary>
        ///     Safe noise removal. Requires the AdvancedFilters plugin.
        /// </summary>
        [Obsolete("AdvancedFilters is no longer supported")]
        public double? RemoveNoise
        {
            get => Get<double>("a.removenoise");
            set => Set<double>("a.removenoise", value);
        }

        /// <summary>
        /// Ignored; all modern backends handle this automatically
        /// </summary>
        [Obsolete("Ignored by all backends; dithering is always enabled with the Imageflow backend and not implemented in GDI")]
        public string Dither
        {
            get => this["dither"];
            set => this["dither"] = value;
        }


        /// <summary>
        ///     Specify a preferred encoder for compressing the output image file. Defaults to 'gdi'. Other valid values are
        ///     'freeimage' and 'wic', which require the FreeImageEncoder and WicEncoder plugins respectively.
        ///     FreeImage offers faster jpeg encoding, while WIC offers faster PNG and GIF encoding. Both, however, require full
        ///     trust.
        /// </summary>
        [Obsolete("Ignored")]
        public string Encoder
        {
            get => this["encoder"];
            set => this["encoder"] = value;
        }

        /// <summary>
        ///     Specify a preferred decoder for parsing the original image file. Defaults to 'gdi'. Other values include
        ///     'freeimage', 'wic', and 'psdreader'. The preferred decoder gets the first chance at reading the files. If that
        ///     fails, all other decoders try, in order of declaration in Web.config.
        ///     Requires the matching FreeImageDecoder, WicDecoder, or PsdReader plugin to be installed.
        /// </summary>
        [Obsolete("Ignored")]
        public string Decoder
        {
            get => this["decoder"];
            set => this["decoder"] = value;
        }

        /// <summary>
        ///     Specify the image processing pipeline to use. Defaults to 'imageflow' if that backend is installed, otherwise uses 'gdi'.
        /// </summary>
        public string Builder
        {
            get => this["builder"];
            set => this["builder"] = value;
        }

        /// <summary>
        /// Requires the Imageflow backend.
        /// 
        ///     Gets or sets a 1 or 4-element array defining corner radii. If the array is 1 element, it applies to all corners. If
        ///     it is 4 elements, each corner gets an individual radius. Values are percentages of the image width or height,
        ///     whichever is smaller.
        ///     Requires the SimpleFilters plugin.
        /// </summary>
        public double[] RoundCorners
        {
            get => this.GetList<double>("s.roundcorners", 0, 4, 1);
            set => this.SetList("s.roundcorners", value, true, 4, 1);
        }


        /// <summary>
        ///     ["paddingWidth"]: Gets/sets the width(s) of padding inside the image border.
        /// </summary>
        [Obsolete("Use CSS to add margins and borders to image; these are no longer supported due to little usage")]
        public BoxEdges Padding
        {
            get => BoxEdges.Parse(this["paddingWidth"], null);
            set => SetAsString<BoxEdges>("paddingWidth", value);
        }

        /// <summary>
        ///     ["margin"]: Gets/sets the width(s) of the margin outside the image border and effects.
        /// </summary>
        [Obsolete("Use CSS to add margins and borders to image; these are no longer supported due to little usage")]
        public BoxEdges Margin
        {
            get => BoxEdges.Parse(this["margin"], null);
            set => SetAsString<BoxEdges>("margin", value);
        }

        /// <summary>
        ///     Friendly get/set accessor for the ["borderWidth"] value. Returns null when unspecified.
        /// </summary>
        [Obsolete("Use CSS to add margins and borders to image; these are no longer supported due to little usage")]
        public BoxEdges Border
        {
            get => BoxEdges.Parse(this["borderWidth"], null);
            set => SetAsString<BoxEdges>("borderWidth", value);
        }
    }
}