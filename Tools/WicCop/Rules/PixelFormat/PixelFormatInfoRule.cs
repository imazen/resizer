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

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules.PixelFormat
{
    class PixelFormatInfoRule : RuleBase<ComponentRuleGroup>, IWowRegistryChecked
    {
        public delegate uint GetPixelFormats(uint count, Guid[] array);

        public static readonly Guid[] AllPixelFormats = GetAllPixelFormats();

        public PixelFormatInfoRule()
            : base(Resources.PixelFormatInfoRule_Text)
        {
        }


        public static uint GetBitPerPixel(Guid pixelFormat)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICPixelFormatInfo info = null;
            try
            {
                info = (IWICPixelFormatInfo)factory.CreateComponentInfo(pixelFormat);

                return info.GetBitsPerPixel();
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
            }
        }

        static Guid[] GetAllPixelFormats()
        {
            List<Guid> res = new List<Guid>();

            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IEnumUnknown eu = factory.CreateComponentEnumerator(WICComponentType.WICPixelFormat, WICComponentEnumerateOptions.WICComponentEnumerateRefresh);
            int hr = 0;
            object[] o = new object[1];
            while (hr == 0)
            {
                uint fetched = 0;

                hr = eu.Next(1, o, ref fetched);
                if (fetched == 1)
                {
                    IWICPixelFormatInfo info = (IWICPixelFormatInfo)o[0];
                    Guid guid;
                    info.GetFormatGUID(out guid);
                    res.Add(guid);

                    info.ReleaseComObject();
                }
                o.ReleaseComObject();
            }

            return res.ToArray();
        }

        public static Guid[] CheckPixelFormats(MainForm form, RuleBase rule, GetPixelFormats method)
        {
            uint count = method(0, null);
            if (count == 0)
            {
                form.Add(rule, Resources.NoPixelFormats);

                return new Guid[0];
            }
            else
            {
                Guid[] pixelFormats = new Guid[count];
                method(count, pixelFormats);

                HashSet<Guid> dups = new HashSet<Guid>();
                HashSet<Guid> unknown = new HashSet<Guid>();
                for (int i = 0; i < pixelFormats.Length; i++)
                {
                    Guid g = pixelFormats[i];
                    if (Array.IndexOf(pixelFormats, g, i + 1) >= 0)
                    {
                        dups.Add(g);
                    }
                    if (Array.IndexOf(PixelFormatInfoRule.AllPixelFormats, g) < 0)
                    {
                        unknown.Add(g);
                    }
                }
                if (unknown.Count > 0)
                {
                    form.Add(rule, Resources.NonRegisteredPixelFormat, new DataEntry(Resources.PixelFormat, unknown.ToArray()));
                }
                if (dups.Count > 0)
                {
                    form.Add(rule, Resources.DuplicatedPixelFormat, new DataEntry(Resources.PixelFormat, dups.ToArray()));
                }

                return pixelFormats;
            }
        }

        void Check(MainForm form, IWICPixelFormatInfo2 info, object tag)
        {
            if (info.GetNumericRepresentation() == WICPixelFormatNumericRepresentation.WICPixelFormatNumericRepresentationUnspecified)
            {
                form.Add(this, Resources.NoNumericRepresention);
            }
            Check(form, info as IWICPixelFormatInfo, tag);
        }

        void Check(MainForm form, IWICPixelFormatInfo info, object tag)
        {
            uint bpp = info.GetBitsPerPixel();
            if (bpp == 0)
            {
                form.Add(this, Resources.ZeroBPP);
            }

            uint count = info.GetChannelCount();
            if (count == 0)
            {
                form.Add(this, Resources.ZeroChannelCount);
            }

            byte[][] channelMasks = new byte[count][];
            Dictionary<int, List<uint>> dups = new Dictionary<int, List<uint>>();
            byte[] fullMask = new byte[(bpp + 7) / 8];
            for (uint i = 0; i < count; i++)
            {
                DataEntry[] de = new DataEntry[] { new DataEntry(Resources.Channel, i) };
                byte[] mask = new byte[info.GetChannelMask(i, 0, null)];
                if (mask.Length > 0)
                {
                    info.GetChannelMask(i, (uint)mask.Length, mask);
                }

                if (mask.Length != (bpp + 7) / 8)
                {
                    form.Add(this, Resources.IncorrectMaskLegth, de, new DataEntry(Resources.Expected, (bpp + 7) / 8), new DataEntry(Resources.Actual, mask.Length));
                }
                byte[] fullMaskSaved = fullMask.Clone() as byte[];
                for (int k = 0; k < fullMask.Length && k < mask.Length; k++)
                {
                    fullMask[k] |= mask[k];
                }
                if (fullMaskSaved.ItemsEqual(fullMask))
                {
                    form.Add(this, Resources.IncorrectChannelMask, de, new DataEntry(Resources.Mask, mask));
                }

                int idx  = Array.FindIndex(channelMasks, mask.ItemsEqual);
                if (idx >= 0)
                {
                    List<uint> r;
                    if (!dups.TryGetValue(idx, out r))
                    {
                        r = new List<uint>();
                        dups[idx] = r;
                        r.Add((uint)idx);
                    }
                    r.Add(i);
                }
                channelMasks[(int)i] = mask;
            }

            foreach (List<uint> l in dups.Values)
            {
                form.Add(this, Resources.DuplicatedChannelMask, new DataEntry(Resources.Channel, l.ToArray()));
            }
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            bool info2Supported = false;

            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICPixelFormatInfo2 info = null;
            try
            {
                info = factory.CreateComponentInfo(Parent.Clsid) as IWICPixelFormatInfo2;
                info2Supported = info != null;
            }
            finally
            {
                info.ReleaseComObject();
                factory.ReleaseComObject();
            }

            if (info2Supported)
            {
                ComponentInfoHelper.Check<IWICPixelFormatInfo2, object>(form, Parent.Clsid, Check, tag, this, false);
            }
            else
            {
                ComponentInfoHelper.Check<IWICPixelFormatInfo, object>(form, Parent.Clsid, Check, tag, this, false);
            }
        }

        #region IWowRegistryChecked Members
        IEnumerable<string> IWowRegistryChecked.GetKeys()
        {
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}", Parent.Clsid);
            yield return string.Format(CultureInfo.InvariantCulture, "CLSID\\{{{0}}}\\Instance\\{{{1}}}", Consts.CATID_WICPixelFormats, Parent.Clsid);
        }
        #endregion
    }
}
