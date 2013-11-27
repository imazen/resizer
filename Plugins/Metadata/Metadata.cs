/* Copyright (c) 2013 Imazen LLC. See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using ImageResizer;
using ImageResizer.ExtensionMethods;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Metadata
{
    /// <summary>
    /// A plugin for ImageResizer that copies image metadata from the source
    /// image to the result image.
    /// </summary>
    [Obsolete("Quick opt-in metadata copying for V3. Will be redesigned in V4.")]
    public class Metadata : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new string[] { "metadata" };
        }

        protected override RequestedAction ProcessFinalBitmap(ImageState s)
        {
            string metadata = s.settings["metadata"];
            Bitmap dest = s.destBitmap;

            if (dest == null ||
                !"true".Equals(metadata, StringComparison.OrdinalIgnoreCase))
            {
                return RequestedAction.None;
            }

            // We've been asked to copy the image metadata.  If the destination
            // image doesn't support property items at all, attempting to set
            // one will throw an ArgumentException with the message "Property not
            // supported.".  If an individual specific property is not supported,
            // SetPropertyItem() will ignore the attempt silently and *not* throw
            // an exception.
            try
            {
                // TODO: Do we need to filter any of the items out?  In particular,
                // 0x0112 (PropertyTagOrientation) may not be valid in the new image
                // if it's been used to auto-rotate the output.  Similarly, values
                // for 0x0100 (PropertyTagImageWidth) and 0x0101 (PropertyTagImageWidth)
                // likely shouldn't be copied as well.
                foreach (PropertyItem prop in s.sourceBitmap.PropertyItems)
                {
                    dest.SetPropertyItem(prop);
                }
            }
            catch (ArgumentException /*ex*/)
            {
                // TODO: Should we be logging a message/issue that the
                // output image doesn't support property items (metadata),
                // but it's been asked for?

                // Right now, we silently ignore the exception and allow the
                // destination image to continue even without the property
                // items.
            }

            return RequestedAction.None;
        }
    }
}
