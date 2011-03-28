using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Encoding {
    public interface IImageEncoder {

        /// <summary>
        /// If the encoder can handle the requirements specified by 'settings', it should return an encoder instance.
        /// If not, it should return null.
        /// </summary>
        /// <param name="originalImage">A source image used to provide hinting for palette generation and for determining the fallback image format. Leave it null if not available.</param>
        /// <param name="settings">Output format settings, among others. </param>
        /// <returns></returns>
        public IImageEncoder CreateIfSuitable(Image originalImage, ResizeSettingsCollection settings);

        /// <summary>
        /// Encodes the image to the specified stream, 
        /// </summary>
        /// <param name="s"></param>
        public void Write(Image i, Stream s);

        /// <summary>
        /// True if the output format will support transparency as it is currently configured.
        /// </summary>
        public bool SupportsTransparency { get; }

        /// <summary>
        /// Returns the appropriate mime-time for the output format as currently configured.
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Returns a file extension appropriate for the output format as currently configured.
        /// </summary>
        public string Extension { get; }

    }
}
