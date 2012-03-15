using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.Util;

namespace ImageResizer {
    /// <summary>
    /// A name/value collection of image processsing instructions. Replaces ResizeSettings.
    /// </summary>
    public class Instructions: NameValueCollection {
        /// <summary>
        /// Creates an empty settings collection. 
        /// </summary>
        public Instructions() : base() { }
        /// <summary>
        /// Copies the specified collection into a new Instructions instance.
        /// </summary>
        /// <param name="col"></param>
        public Instructions(NameValueCollection col) : base(col) { }
        /// <summary>
        /// Parses the specified querystring into name/value pairs. leading ? not required.
        /// </summary>
        /// <param name="queryString"></param>
        public Instructions(string queryString) : base(PathUtils.ParseQueryStringFriendlyAllowSemicolons(queryString)) { }



        /*
        ///Width
        ///Height
        ///Mode
        ///JpegQuality
        ///AutoRotate
        ///SourceRotate
        ///SourceFlip
        ///FinalRotate
        ///FinalFlip
        ///CropRectangle
        ///CropXUnits
        ///CropYUnits
        ///Format
        ///Zoom
        ///BackgroundColor
        ///Frame
        ///Page
        ///FallbackUrl 
        ///
         */
    }
}
