//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Globalization;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.Encoder
{
    class BitmapEncoderRule : BitmapEncoderRuleBase
    {
        public BitmapEncoderRule()
            : base(Resources.BitmapEncoderRule_Text)
        {
        }

        void Check<T>(MainForm form, Action<T> action, T value)
        {
            try
            {
                action(value);
            }
            catch (Exception e)
            {
                form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, new DataEntry(Resources.Value, value));
            }
        }

        protected override bool ProcessEncoder(MainForm form, IWICBitmapEncoder encoder, object tag)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmap bitmap = factory.CreateBitmap(16, 16, Consts.GUID_WICPixelFormat32bppBGRA, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad);
            IWICPalette palette = factory.CreatePalette();
            palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false);
            IWICColorContext context = factory.CreateColorContext();
            context.InitializeFromExifColorSpace(ExifColorSpace.sRGB);

            ComponentInfoHelper.CheckEquals<IWICBitmapEncoderInfo>(form, encoder.GetEncoderInfo, this, Extensions.CompareInfos);
            Check(form, encoder.SetPalette, palette);
            Check(form, encoder.SetThumbnail, bitmap);
            Check(form, encoder.SetPreview, bitmap);
            try
            {
                encoder.SetColorContexts(1, new IWICColorContext[] { context });
            }
            catch (Exception e)
            {
                form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e);
            }

            try
            {
                encoder.Commit();
                form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_ShouldFail, "IWICBitmapEncoder::Commit(...)"), new DataEntry(Resources.FrameCount, 0));
            }
            catch (Exception e)
            {
                form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_FRAMEMISSING, e, new DataEntry(Resources.FrameCount, 0));
            }

            palette.ReleaseComObject();
            bitmap.ReleaseComObject();
            factory.ReleaseComObject();
            context.ReleaseComObject();

            return base.ProcessEncoder(form, encoder, tag);
        }
    }
}
