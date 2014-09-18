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

namespace ImageResizer.Plugins.CopyMetadata
{
    /// <summary>
    /// A plugin for ImageResizer that copies image metadata from the source
    /// image to the result image.
    /// </summary>
    [Obsolete("Quick opt-in metadata copying for V3. Will be redesigned in V4.")]
    public class CopyMetadataPlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        private const string SettingsKey = "copymetadata";

        private List<int> excludedProperties = null;

        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new string[] { SettingsKey };
        }

        protected override RequestedAction ProcessFinalBitmap(ImageState s)
        {
            string metadata = s.settings[SettingsKey];
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
                // There are a handful of properties we *won't* copy, because they
                // may conflict with the resulting image.
                if (this.excludedProperties == null)
                {
                    List<int> list = new List<int>();

                    list.Add(0x0100);   // PropertyTagImageWidth
                    list.Add(0x0101);   // PropertyTagImageHeight
                    list.Add(0x0102);   // PropertyTagBitsPerSample
                    list.Add(0x0103);   // PropertyTagCompression
                    list.Add(0x0106);   // pixel composition (EXIF 2.2, not documented on MSDN)
                    list.Add(0x0108);   // PropertyTagCellWidth
                    list.Add(0x0109);   // PropertyTagCellHeight
                    list.Add(0x010A);   // PropertyTagFillOrder

                    list.Add(0x0111);   // PropertyTagStripOffsets
                    list.Add(0x0112);   // PropertyTagOrientation

                    list.Add(0x0115);   // PropertyTagSamplesPerPixel
                    list.Add(0x0116);   // PropertyTagRowsPerStrip
                    list.Add(0x0117);   // PropertyTagStripBytesCount
                    list.Add(0x0118);   // PropertyTagMinSampleValue
                    list.Add(0x0119);   // PropertyTagMaxSampleValue
                    list.Add(0x011A);   // PropertyTagXResolution
                    list.Add(0x011B);   // PropertyTagYResolution
                    list.Add(0x011C);   // PropertyTagPlanarConfig
                    list.Add(0x011D);   // PropertyTagPageName
                    list.Add(0x011E);   // PropertyTagXPosition
                    list.Add(0x011F);   // PropertyTagYPosition

                    list.Add(0x0120);   // PropertyTagFreeOffset
                    list.Add(0x0121);   // PropertyTagFreeByteCounts
                    list.Add(0x0122);   // PropertyTagGrayResponseUnit
                    list.Add(0x0123);   // PropertyTagGrayResponseCurve
                    list.Add(0x0124);   // PropertyTagT4Option
                    list.Add(0x0125);   // PropertyTagT6Option
                    list.Add(0x0128);   // PropertyTagResolutionUnit
                    list.Add(0x0129);   // PropertyTagPageNumber
             
                    list.Add(0x013D);   // PropertyTagPredictor
                    list.Add(0x013E);   // PropertyTagWhitePoint
                    list.Add(0x013F);   // PropertyTagPrimaryChromaticities
                    list.Add(0x0140);   // PropertyTagColorMap
                    list.Add(0x0141);   // PropertyTagHalftoneHints
                    list.Add(0x0142);   // PropertyTagTileWidth
                    list.Add(0x0143);   // PropertyTagTileLength
                    list.Add(0x0144);   // PropertyTagTileOffset
                    list.Add(0x0145);   // PropertyTagTileByteCounts

                    list.Add(0x015A);   // Indexed image (not on MSDN)
                    list.Add(0x015B);   // JPEG quantization andHuffman tables (not on MSDN)

                    list.Add(0x0200);   // PropertyTagJPEGProc
                    list.Add(0x0201);   // PropertyTagJPEGInterFormat
                    list.Add(0x0202);   // PropertyTagJPEGInterLength
                    list.Add(0x0203);   // PropertyTagJPEGRestartInterval
                    list.Add(0x0205);   // PropertyTagJPEGLosslessPredictors
                    list.Add(0x0206);   // PropertyTagJPEGPointTransforms
                    list.Add(0x0207);   // PropertyTagJPEGQTables
                    list.Add(0x0208);   // PropertyTagJPEGDCTables
                    list.Add(0x0209);   // PropertyTagJPEGACTables
 
                    list.Add(0x0211);   // PropertyTagYCbCrCoefficients
                    list.Add(0x0212);   // PropertyTagYCbCrSubsampling
                    list.Add(0x0213);   // PropertyTagYCbCrPositioning
                    list.Add(0x0214);   // PropertyTagREFBlackWhite

                    list.Add(0x5001);   // PropertyTagResolutionXUnit
                    list.Add(0x5002);   // PropertyTagResolutionYUnit
                    list.Add(0x5003);   // PropertyTagResolutionXLengthUnit
                    list.Add(0x5004);   // PropertyTagResolutionYLengthUnit

                    list.Add(0x5010);   // PropertyTagJPEGQuality
                    list.Add(0x5011);   // PropertyTagGridSize

                    this.excludedProperties = list;
                }

                // Copy all properties that aren't in the exclusion list.
                foreach (PropertyItem prop in s.sourceBitmap.PropertyItems)
                {
                    if (!this.excludedProperties.Contains(prop.Id))
                    {
                        dest.SetPropertyItem(prop);
                    }
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
