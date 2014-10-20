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
using System.Runtime.InteropServices;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.FormatConverter
{
    class FormatConverterInfoRule : RuleBase<ComponentRuleGroup>, IWowRegistryChecked
    {
        static readonly Guid[] canonical = new Guid[] 
        {
            Consts.GUID_WICPixelFormat32bppBGRA,
            Consts.GUID_WICPixelFormat64bppRGBA,
            Consts.GUID_WICPixelFormat64bppRGBAFixedPoint,
            Consts.GUID_WICPixelFormat128bppRGBAFloat,
            Consts.GUID_WICPixelFormat128bppRGBAFixedPoint
        };

        public FormatConverterInfoRule()
            : base(Resources.FormatConverterInfoRule_Text)
        {
        }

        bool CheckConvertion(MainForm form, IWICFormatConverterInfo info, Guid from, Guid to)
        {
            if (from == to)
            {
                return true;
            }

            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICPalette palette = factory.CreatePalette();
            palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false);
            IWICBitmap bitmap = null;
            IWICFormatConverter converter = null;
            try
            {
                try
                {
                    converter = info.CreateInstance();
                }
                catch (Exception e)
                {
                    form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));

                    return false;
                }

                try
                {
                    bitmap = factory.CreateBitmap(1, 1, from, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad);
                    bitmap.SetPalette(palette);
                }
                catch (Exception e)
                {
                    form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e), new DataEntry(Resources.PixelFormat, from));

                    return false;
                }

                try
                {
                    converter.Initialize(bitmap, to, WICBitmapDitherType.WICBitmapDitherTypeNone, palette, 0, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
                }
                catch (Exception e)
                {
                    form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT, e, new DataEntry(Resources.Source, from), new DataEntry(Resources.Destination, to));

                    return false;
                }

                return true;
            }
            finally
            {
                palette.ReleaseComObject();
                converter.ReleaseComObject();
                bitmap.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        void Check(MainForm form, IWICFormatConverterInfo info, object tag)
        {
            Guid[] pixelFormats = PixelFormatInfoRule.CheckPixelFormats(form, this, info.GetPixelFormats);
            Type type = Type.GetTypeFromCLSID(Parent.Clsid);

            if (type == null)
            {
                form.Add(this, Resources.PixelFormatConverterNotCreatable);
            }
            else
            {
                IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
                foreach (Guid g in pixelFormats)
                {
                    bool supportsTo = false;
                    bool supportsFrom = false;
                    foreach (Guid c in canonical)
                    {
                        supportsTo |= CheckConvertion(form, info, g, c);
                        supportsFrom |= CheckConvertion(form, info, c, g);
                    }

                    if (!supportsTo)
                    {
                        form.Add(this, Resources.ToCannonicalNotSupported, new DataEntry(Resources.PixelFormat, g));
                    }
                    if (!supportsFrom)
                    {
                        form.Add(this, Resources.FromCannonicalNotSupported, new DataEntry(Resources.PixelFormat, g));
                    }
                }
            }
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            ComponentInfoHelper.Check<IWICFormatConverterInfo, object>(form, Parent.Clsid, Check, tag, this, true);
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}", Parent.Clsid);
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}\\Instance\\{{{1}}}", Consts.CATID_WICFormatConverters, Parent.Clsid);
        }
        #endregion
    }
}
