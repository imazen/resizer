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
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    class BitmapDecoderInfoRule : DecoderRuleBase, IWowRegistryChecked
    {
        new class Tag
        {
            public Guid[] PixelFormats;
            public string[] Extensions;
            public bool SupportsMultiframe;
        }

        public BitmapDecoderInfoRule()
            : base(Resources.BitmapDecoderInfoRule_Text)
        {
        }

        void Check(MainForm form, IWICBitmapDecoderInfo info, Tag tag)
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

            uint count;
            uint size = info.GetPatterns(0, IntPtr.Zero, out count);
            if (size != 0)
            {
                IntPtr p = Marshal.AllocCoTaskMem((int)size);
                try
                {
                    info.GetPatterns(size, p, out count);

                    WICBitmapPattern[] patterns = PropVariantMarshaler.ToArrayOf<WICBitmapPattern>(p, (int)count);
                    int index = 0;
                    HashSet<int> dups = new HashSet<int>();
                    foreach (WICBitmapPattern pattern in patterns)
                    {
                        index++;
                        if (pattern.Length == 0)
                        {
                            form.Add(this, Resources.PatternZeroLength, new DataEntry(Resources.PatternIndex, index));
                        }
                        else if (index < patterns.Length)
                        {
                            if (Array.FindIndex(patterns, index, delegate(WICBitmapPattern obj) { return obj.EndOfStream == pattern.EndOfStream && obj.Position == pattern.Position && GetNormalizedMask(obj).ItemsEqual(GetNormalizedMask(pattern)); }) >= 0)
                            {
                                dups.Add(index);
                            }
                        }
                    }
                    if (dups.Count > 0)
                    {
                        form.Add(this, Resources.PatternDuplicated, new DataEntry(Resources.PatternIndices, dups.ToArray()));
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(p);
                }
            }

            if (count == 0 || size == 0)
            {
                form.Add(this, Resources.PatternNo);
            }
        }

        static byte[] GetNormalizedMask(WICBitmapPattern pattern)
        {
            byte[] t = PropVariantMarshaler.ToArrayOf<byte>(pattern.Pattern, (int)pattern.Length);
            byte[] mask = PropVariantMarshaler.ToArrayOf<byte>(pattern.Mask, (int)pattern.Length);

            for (int i = 0; i < t.Length; i++)
            {
                t[i] &= mask[i];
            }

            return t;
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            Tag t = new Tag();
            ComponentInfoHelper.Check<IWICBitmapDecoderInfo, Tag>(form, Parent.Clsid, Check, t, this, true);

            base.RunOverride(form, t);
        }

        protected override bool ProcessDecoder(MainForm form, IWICBitmapDecoder decoder, DataEntry[] de, object tag)
        {
            IWICBitmapDecoderInfo info = decoder.GetDecoderInfo();
            try
            {
                Guid clsid;
                info.GetCLSID(out clsid);
                if (clsid != Parent.Clsid)
                {
                    form.Add(this, Resources.IncorrectDecoderPickedUp, de, new DataEntry(Resources.Expected, Parent.Clsid), new DataEntry(Resources.Actual, clsid));
                }
                else
                {
                    Tag t = (Tag)tag;
                    if (!t.SupportsMultiframe && decoder.GetFrameCount() > 1)
                    {
                        form.Add(this, Resources.DecoderDoesNotMultiframe, de, new DataEntry(Resources.FrameCount, decoder.GetFrameCount()));
                    }
                }
            }
            finally
            {
                info.ReleaseComObject();
            }

            return true;
        }

        protected override bool ProcessFrameDecode(MainForm form, IWICBitmapFrameDecode frame, DataEntry[] de, object tag)
        {
            Guid pixelFormat;
            frame.GetPixelFormat(out pixelFormat);

            Tag t = (Tag)tag;
            if (Array.IndexOf(t.PixelFormats, pixelFormat) < 0)
            {
                form.Add(this, Resources.DecoderUnsuportedPixelFormat, de, new DataEntry(Resources.PixelFormat, pixelFormat), new DataEntry(Resources.SupportedPixelFormats, t.PixelFormats));
            }

            return true;
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}", Parent.Clsid);
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}\\Instance\\{{{1}}}", Consts.CATID_WICBitmapDecoders, Parent.Clsid);
        }
        #endregion
    }
}
