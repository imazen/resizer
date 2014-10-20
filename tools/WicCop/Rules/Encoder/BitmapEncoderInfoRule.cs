//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.Encoder
{
    class BitmapEncoderInfoRule : BitmapEncoderRuleBase, IWowRegistryChecked
    {
        new class Tag
        {
            public Guid[] PixelFormats;
            public string[] Extensions;
            public bool SupportsMultiframe;
        }

        public BitmapEncoderInfoRule()
            : base(Resources.BitmapEncoderInfoRule_Text)
        {
        }

        void Check(MainForm form, IWICBitmapEncoderInfo info, Tag tag)
        {
            ComponentInfoHelper.CheckNotReserverdGuid(form, info.GetContainerFormat, this);

            tag.PixelFormats = PixelFormatInfoRule.CheckPixelFormats(form, this, info.GetPixelFormats);

            ComponentInfoHelper.CheckVersion(form, info.GetColorManagementVersion, this);
            ComponentInfoHelper.CheckCommaSeparatedString(form, info.GetMimeTypes, ComponentInfoHelper.MimeMask, this);

            tag.Extensions = ComponentInfoHelper.CheckCommaSeparatedString(form, info.GetFileExtensions, ComponentInfoHelper.ExtensionMask, this);

            if (!info.DoesSupportMultiframe() && info.DoesSupportAnimation())
            {
                form.Add(this, Resources.DecoderAnimationIfMultiframe);
            }
            tag.SupportsMultiframe = info.DoesSupportMultiframe();

        }

        protected override void RunOverride(MainForm form, object tag)
        {
            Tag t = new Tag();
            ComponentInfoHelper.Check<IWICBitmapEncoderInfo, Tag>(form, Parent.Clsid, Check, t, this, true);

            base.RunOverride(form, t);
        }

        protected override bool ProcessEncoder(MainForm form, IWICBitmapEncoder encoder, object tag)
        {
            Tag t = (Tag)tag;
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICPalette palette = factory.CreatePalette();
            palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false);
            IWICBitmapFrameEncode frame = null;
            IWICBitmapFrameEncode frame2 = null;
            IPropertyBag2[] bag = new IPropertyBag2[1];
            try
            {
                MethodInfo mi = typeof(IWICBitmapEncoder).GetMethod("CreateNewFrame");
                try
                {
                    encoder.CreateNewFrame(out frame, bag);
                }
                catch (Exception e)
                {
                    form.Add(this, mi.ToString(Resources._0_Failed), new DataEntry(Resources.FrameIndex, 0), new DataEntry(e));
                }

                if (frame == null)
                {
                    form.Add(this, mi.ToString(Resources._0_NULL), new DataEntry(Resources.Parameter, mi.GetParameters()[0].Name));
                }
                else
                {
                    if (bag[0] == null)
                    {
                        form.Add(this, mi.ToString(Resources._0_NULL), new DataEntry(Resources.Parameter, mi.GetParameters()[1].Name));
                    }
                    try
                    {
                        frame.Initialize(bag[0]);
                        frame.SetSize(1, 1);
                        frame.SetPalette(palette);
                        Guid pixelFormat = Guid.Empty;

                        List<Guid> allPixelFormats = new List<Guid>(PixelFormatInfoRule.AllPixelFormats);
                        allPixelFormats.Add(Consts.GUID_WICPixelFormatDontCare);
                        
                        foreach (Guid g in allPixelFormats)
                        {
                            pixelFormat = g;
                            frame.SetPixelFormat(ref pixelFormat);
                            if (g == pixelFormat)
                            {
                                if (Array.IndexOf(t.PixelFormats, g) < 0)
                                {
                                    form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources.DidNotChangeUnsupportedPixelFormat, "IWICBitmapFrameEncode::SetPixelFormat(...)"), new DataEntry(Resources.PixelFormat, g), new DataEntry(Resources.SupportedPixelFormats, t.PixelFormats));
                                }
                            }
                            else
                            {
                                if (Array.IndexOf(t.PixelFormats, g) >= 0)
                                {
                                    form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources.ChangedSupportedPixelFormat, "IWICBitmapFrameEncode::SetPixelFormat(...)"), new DataEntry(Resources.Expected, g), new DataEntry(Resources.Actual, pixelFormat));
                                }
                            }
                        }
                        pixelFormat = Consts.GUID_WICPixelFormat32bppBGRA;
                        frame.SetPixelFormat(ref pixelFormat);
                        byte[] buffer = new byte[(PixelFormatInfoRule.GetBitPerPixel(pixelFormat) + 7) /8];
                        frame.WritePixels(1, (uint)buffer.Length, (uint)buffer.Length, buffer);
                        frame.Commit();

                        try
                        {
                            encoder.CreateNewFrame(out frame2, null);
                            if (!t.SupportsMultiframe)
                            {

                                form.Add(this, mi.ToString(Resources._0_ShouldFail), new DataEntry(WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION));
                            }
                        }
                        catch (Exception e)
                        {
                            if (t.SupportsMultiframe)
                            {
                                form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(Resources.FrameIndex, 1), new DataEntry(e));
                            }
                            else 
                            {
                                form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, new DataEntry(Resources.FrameIndex, 1));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
                    }
                }
            }
            finally
            {
                frame2.ReleaseComObject();
                encoder.ReleaseComObject();
                bag.ReleaseComObject();
                factory.ReleaseComObject();
                palette.ReleaseComObject();
            }

            return base.ProcessEncoder(form, encoder, tag);
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}", Parent.Clsid);
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}\\Instance\\{{{1}}}", Consts.CATID_WICBitmapEncoders, Parent.Clsid);
        }
        #endregion
    }
}
