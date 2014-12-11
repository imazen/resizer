//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    class BitmapDecoderRule : DecoderRuleBase
    {
        public BitmapDecoderRule()
            : base(Resources.BitmapDecoderRule_Text)
        {
        }

        protected override void QueryCapabilitiesError(MainForm form, string text, params DataEntry[] de)
        {
            form.Add(this, text, de);
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmapDecoderInfo info = (IWICBitmapDecoderInfo)factory.CreateComponentInfo(Parent.Clsid);
            Guid cf;
            info.GetContainerFormat(out cf);
            IWICBitmapDecoder decoder = null;
            try
            {
                decoder = info.CreateInstance();
                Guid containerFormat;
                try
                {
                    decoder.GetContainerFormat(out containerFormat);
                    if (containerFormat != cf)
                    {
                        form.Add(this, Resources.ContainerFormatsDoNotMatch, new DataEntry(Resources.Actual, cf), new DataEntry(Resources.Expected, containerFormat));
                    }
                }
                catch (Exception e)
                {
                    form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
                }

                ComponentInfoHelper.CheckEquals<IWICBitmapDecoderInfo>(form, decoder.GetDecoderInfo, this, Extensions.CompareInfos);
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
                decoder.ReleaseComObject();
            }

            base.RunOverride(form, tag);
        }

        void CheckGetBitmapSource(MainForm form, DataEntry[] de, Func<IWICBitmapSource> method, WinCodecError error)
        {
            IWICBitmapSource bs = null;
            try
            {
                bs = method();
            }
            catch (Exception e)
            {
                form.CheckHRESULT(this, error, e, de);
            }
            finally
            {
                bs.ReleaseComObject();
            }
        }

        void CheckGetColorContexts(MainForm form, DataEntry[] de, Func<uint, IWICColorContext[], uint> method)
        {
            IWICColorContext[] contexts = null;
            IWICImagingFactory factory = new WICImagingFactory() as IWICImagingFactory;
            try
            {
                try
                {
                    contexts = new IWICColorContext[method(0, null)];
                }
                catch (Exception e)
                {
                    form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, "0, NULL", de);

                    return;
                }
                if (contexts.Length > 0)
                {
                    for (int i = 0; i < contexts.Length; i++)
                    {
                        contexts[i] = factory.CreateColorContext();
                    }
                    try
                    {
                        method((uint)contexts.Length, contexts);

                        int index = 0;
                        foreach (IWICColorContext c in contexts)
                        {
                            if (c == null)
                            {
                                form.Add(this, method.ToString(Resources._0_NULLItem), de, new DataEntry(Resources.Index, index));
                            }
                            index++;
                        }
                    }
                    catch (Exception e)
                    {
                        form.Add(this, method.ToString(Resources._0_Failed), de, new DataEntry(e));
                    }
                }
            }
            finally
            {
                contexts.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        void CheckCopyPalette(MainForm form, DataEntry[] de, Action<IWICPalette> method)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICPalette palette = factory.CreatePalette();

            try
            {
                method(palette);
                try
                {
                    if (palette.GetColorCount() == 0)
                    {
                        form.Add(this, method.ToString(Resources._0_ZeroColorPalette), de);
                    }
                }
                catch (Exception e)
                {
                    form.Add(this, method.ToString(Resources._0_IncorrectStatePalette), de, new DataEntry(e));
                }
            }
            catch (Exception e)
            {
                form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_PALETTEUNAVAILABLE, e, de);
            }
            finally
            {
                palette.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        protected override bool ProcessDecoder(MainForm form, IWICBitmapDecoder decoder, DataEntry[] de, object tag)
        {
            CheckCopyPalette(form, de, decoder.CopyPalette);

            CheckGetColorContexts(form, de, decoder.GetColorContexts);

            if (decoder.GetFrameCount() == 0)
            {
                form.Add(this, Resources.FileNoFrames, de);
            }

            CheckGetBitmapSource(form, de, decoder.GetPreview, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION);
            CheckGetBitmapSource(form, de, decoder.GetThumbnail, WinCodecError.WINCODEC_ERR_CODECNOTHUMBNAIL);

            return true;
        }

        protected override bool ProcessFrameDecode(MainForm form, IWICBitmapFrameDecode frame, DataEntry[] de, object tag)
        {
            CheckGetColorContexts(form, de, frame.GetColorContexts);

            CheckGetBitmapSource(form, de, frame.GetThumbnail, WinCodecError.WINCODEC_ERR_CODECNOTHUMBNAIL);

            uint width;
            uint height;
            frame.GetSize(out width, out height);

            Guid pixelFormat;
            frame.GetPixelFormat(out pixelFormat);


            uint stride = (PixelFormatInfoRule.GetBitPerPixel(pixelFormat) * width + 7) / 8;
            byte[] buffer = new byte[stride * height];
            WICRect rect = new WICRect();
            rect.Height = (int)Math.Min(height, int.MaxValue);
            rect.Width = (int)Math.Min(width, int.MaxValue);
            try
            {
                frame.CopyPixels(rect, stride, stride * height, buffer);
                try
                {
                    frame.CopyPixels(null, stride, stride * height, buffer);
                }
                catch (Exception e)
                {
                    form.Add(this, e.TargetSite.ToString(Resources._0_Failed, "NULL"), de, new DataEntry(e));
                }
            }
            catch
            {
            }

            return true;
        }
    }
}
