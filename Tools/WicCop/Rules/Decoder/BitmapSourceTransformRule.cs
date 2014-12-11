//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Globalization;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    class BitmapSourceTransformRule : DecoderRuleBase
    {
        public BitmapSourceTransformRule()
            : base(Resources.BitmapSourceTransformRule_Text)
        {
        }

        protected override bool ProcessFrameDecode(MainForm form, IWICBitmapFrameDecode frame, DataEntry[] de, object tag)
        {
            IWICBitmapSourceTransform transform = null;
            try
            {
                transform = (IWICBitmapSourceTransform)frame;
            }
            catch (InvalidCastException)
            {
                return false;
            }

            if (transform == null)
            {
                form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_NULL, "IWICBitmapFrameDecode::QueryInterface(IID_IWICBitmapSourceTransform, ...)"), de);
            }
            else
            {
                try
                {
                    if (!transform.DoesSupportTransform(WICBitmapTransformOptions.WICBitmapTransformRotate0))
                    {
                        form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_NotExpectedValue, "IWICBitmapSourceTransform::DoesSupportTransform(WICBitmapTransformRotate0, ...)"), de, new DataEntry(Resources.Actual, "FALSE"), new DataEntry(Resources.Expected, "TRUE"));
                    }
                    uint width;
                    uint height;
                    frame.GetSize(out width, out height);

                    uint widthSaved = width;
                    uint heightSaved = height;
                    transform.GetClosestSize(ref width, ref height);

                    if (width != widthSaved || height != heightSaved)
                    {
                        form.Add(this, Resources.IWICBitmapSourceTransform_ChangedSize, de, new DataEntry(Resources.Expected, new Size((int)widthSaved, (int)heightSaved)), new DataEntry(Resources.Actual, new Size((int)width, (int)height)));
                    }

                    Guid pixelFormat;
                    frame.GetPixelFormat(out pixelFormat);

                    Guid pixelFormatSaved = pixelFormat;
                    transform.GetClosestPixelFormat(ref pixelFormat);

                    if (pixelFormat != pixelFormatSaved)
                    {
                        form.Add(this, Resources.IWICBitmapSourceTransform_ChangedPixelformat, de, new DataEntry(Resources.Expected, pixelFormatSaved), new DataEntry(Resources.Actual, pixelFormat));
                    }

                    uint stride = (PixelFormatInfoRule.GetBitPerPixel(pixelFormat) * width + 7) / 8;

                    byte[] buffer = new byte[stride * height];
                    try
                    {
                        transform.CopyPixels(null, width, height, null, WICBitmapTransformOptions.WICBitmapTransformRotate0, stride, (uint)buffer.LongLength, buffer);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed, "NULL, uiWidth, uiHeight, NULL, WICBitmapTransformRotate0, ..."), de, new DataEntry(e), new DataEntry("uiWidth", width), new DataEntry("uiHeight", height));
                    }
                }
                finally
                {
                    transform.ReleaseComObject();
                }
            }

            return true;
        }
    }
}
