//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.Encoder
{
    class BitmapFrameEncode : BitmapEncoderRuleBase
    {
        public BitmapFrameEncode()
            : base(Resources.BitmapFrameEncode_Text)
        {
        }

        protected override bool ProcessEncoder(MainForm form, IWICBitmapEncoder encoder, object tag)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmap bitmap = factory.CreateBitmap(1, 1, Consts.GUID_WICPixelFormat128bpp7ChannelsAlpha, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad);
            IWICBitmapFrameEncode frame = null;
            try
            {
                try
                {
                    encoder.CreateNewFrame(out frame, null);
                }
                catch (Exception e)
                {
                    form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
                }

                if (frame != null)
                {
                    try
                    {
                        frame.Initialize(null);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed, "NULL"), new DataEntry(e));
                        frame.ReleaseComObject();
                        frame = null;
                    }
                }
                if (frame != null)
                {
                    try
                    {
                        frame.WriteSource(bitmap, null);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed, "..., NULL"), new DataEntry(e));
                        frame.ReleaseComObject();
                        frame = null;
                    }
                }

                if (frame != null)
                {
                    try
                    {
                        frame.Commit();
                        encoder.Commit();
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
                    }
                }
            }
            finally
            {
                frame.ReleaseComObject();
                bitmap.ReleaseComObject();
                factory.ReleaseComObject();
            }

            return base.ProcessEncoder(form, encoder, tag);
        }
    }
}
