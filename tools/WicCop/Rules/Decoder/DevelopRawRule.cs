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
using System.Runtime.InteropServices;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    class DevelopRawRule : DecoderRuleBase
    {
        delegate void GetTriplet<T>(out T t1, out T t2, out T t3);

        class Notification : IWICDevelopRawNotificationCallback
        {
            bool notified;
            WICRawChangeNotification expected;
            readonly List<WICRawChangeNotification> raised = new List<WICRawChangeNotification>();

            internal void Expect(WICRawChangeNotification expected)
            {
                notified = false;
                this.expected = expected;
                raised.Clear();
            }

            internal bool Notified
            {
                get { return notified; }
            }

            internal WICRawChangeNotification[] GetRaised()
            {
                return raised.ToArray();
            }

            public void Notify(WICRawChangeNotification NotificationMask)
            {
                if ((uint)(NotificationMask & expected) != 0 || (uint)NotificationMask == 0)
                {
                    notified = true;
                }
                raised.Add(NotificationMask);
            }
        }

        static readonly double[] rotation90 = new double[] { -180, -90, 0, 90, 180, 360 };
        static readonly double[] rotation = GetRotations(50);

        public DevelopRawRule()
            : base(Resources.DevelopRawRule_Text)
        {
        }

        static double[] GetRotations(int length)
        {
            Random r = new Random();
            double[] res = new double[rotation90.Length + length];
            rotation90.CopyTo(res, 0);
            for (int i = 0; i < length; ++i)
            {
                res[rotation90.Length + i] = r.NextDouble() * (360 + 270) - 270;
            }

            return res;
        }

        internal static WinCodecError? SetSupported(WICRawCapabilities capabilities)
        {
            if (capabilities == WICRawCapabilities.WICRawCapabilityFullySupported)
            {
                return null;
            }
            else
            {
                return WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION;
            }
        }

        void CheckSetDestinationColorContextSupported(MainForm form, WinCodecError? error, IWICDevelopRaw raw, DataEntry[] de, Guid pixelFormatOriginal)
        {
            Notification[] notifications = new Notification[] { new Notification(), new Notification(), null };
            IWICImagingFactory factory = new WICImagingFactory() as IWICImagingFactory;
            IWICColorContext[] contexts = { factory.CreateColorContext(), null };
            contexts[0].InitializeFromExifColorSpace(ExifColorSpace.sRGB);
            Action<IWICColorContext> setter = raw.SetDestinationColorContext;
            try
            {
                foreach (Notification n in notifications)
                {
                    try
                    {
                        raw.SetNotificationCallback(n);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, n == null ? e.TargetSite.ToString(Resources._0_Failed, "NULL") : e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                    }
                    foreach (IWICColorContext c in contexts)
                    {
                        try
                        {
                            setter(c);
                            if (error.HasValue)
                            {
                                form.Add(this, setter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, error.Value));
                            }
                            else
                            {
                                if (n != null && !n.Notified)
                                {
                                    form.Add(this, Resources.DevelopRawRule_NoNotification, de, new DataEntry(Resources.Actual, n.GetRaised()), new DataEntry(Resources.Expected, WICRawChangeNotification.WICRawChangeNotification_DestinationColorContext), new DataEntry(Resources.Method, setter.ToString("{0}")));
                                }
                                if (n != notifications[0] && notifications[0].Notified)
                                {
                                    form.Add(this, Resources.DevelopRawRule_NotificationOnWrong, de, new DataEntry(Resources.Method, setter.ToString("{0}")));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (error.HasValue)
                            {
                                form.CheckHRESULT(this, error.Value, e, c == null ? "NULL" : null, de);
                            }
                            else
                            {
                                form.Add(this, c == null ? e.TargetSite.ToString(Resources._0_Failed, "NULL") : e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                            }
                        }
                    }
                }
            }
            finally
            {
                contexts.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        void CheckSetToneCurveSupported(MainForm form, WinCodecError? error, IWICDevelopRaw raw, DataEntry[] de, Guid pixelFormatOriginal)
        {
            Notification[] notifications = new Notification[] { new Notification(), new Notification(), null };
            Func<uint, IntPtr, uint> getter = raw.GetToneCurve;
            Action<uint, IntPtr> setter = raw.SetToneCurve;

            IntPtr p = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf(typeof(WICRawToneCurvePoint)) * 2 + Marshal.SizeOf(typeof(uint));
                p = Marshal.AllocCoTaskMem(size);

                Marshal.WriteInt32(p, 2);
                Marshal.WriteInt64(new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(uint))), 0);
                Marshal.WriteInt64(new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(uint)) + Marshal.SizeOf(typeof(double))), 0);
                Marshal.WriteInt64(new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(uint)) + Marshal.SizeOf(typeof(WICRawToneCurvePoint))), BitConverter.DoubleToInt64Bits(1));
                Marshal.WriteInt64(new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(uint)) + Marshal.SizeOf(typeof(WICRawToneCurvePoint)) + Marshal.SizeOf(typeof(double))), BitConverter.DoubleToInt64Bits(1));
                foreach (Notification n in notifications)
                {
                    try
                    {
                        raw.SetNotificationCallback(n);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, n == null ? e.TargetSite.ToString(Resources._0_Failed, "NULL") : e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                    }

                    bool getSupported = false;
                    try
                    {
                        getter((uint)size, p);
                        getSupported = true;
                    }
                    catch
                    {
                        break;
                    }
                    if (getSupported)
                    {
                        try
                        {
                            notifications[0].Expect(WICRawChangeNotification.WICRawChangeNotification_ToneCurve);
                            notifications[1].Expect(WICRawChangeNotification.WICRawChangeNotification_ToneCurve);
                            setter((uint)size, p);
                            if (error.HasValue)
                            {
                                form.Add(this, setter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, error.Value));
                            }
                            else
                            {
                                if (n != null && !n.Notified)
                                {
                                    form.Add(this, Resources.DevelopRawRule_NoNotification, de, new DataEntry(Resources.Actual, n.GetRaised()), new DataEntry(Resources.Expected, WICRawChangeNotification.WICRawChangeNotification_ToneCurve), new DataEntry(Resources.Method, setter.ToString("{0}")));
                                }
                                if (n != notifications[0] && notifications[0].Notified)
                                {
                                    form.Add(this, Resources.DevelopRawRule_NotificationOnWrong, de, new DataEntry(Resources.Method, setter.ToString("{0}")));
                                }

                                CheckGetSupported(form, true, getter, de);
                            }
                        }
                        catch (Exception e)
                        {
                            CheckGetSupported(form, true, getter, de);
                            if (error.HasValue)
                            {
                                form.CheckHRESULT(this, error.Value, e, de);
                            }
                            else
                            {
                                form.Add(this, setter.ToString(Resources._0_Failed), de, new DataEntry(e));
                            }
                        }
                    }
                    CheckPixelFormat(form, raw, de, pixelFormatOriginal);
                }
            }
            finally
            {
                if (p != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(p);
                }
            }
        }

        void CheckSetRgbWhitePointSupported(MainForm form, WinCodecError? error, IWICDevelopRaw raw, DataEntry[] de, Guid pixelFormatOriginal)
        {
            Notification[] notifications = new Notification[] { new Notification(), new Notification(), null };
            GetTriplet<uint> getter = raw.GetWhitePointRGB;
            Action<uint, uint, uint> setter = raw.SetWhitePointRGB;

            foreach (Notification n in notifications)
            {
                try
                {
                    raw.SetNotificationCallback(n);
                }
                catch (Exception e)
                {
                    form.Add(this, n == null ? e.TargetSite.ToString(Resources._0_Failed, "NULL") : e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                }

                uint r, g, b;
                r = g = b = uint.MaxValue;
                bool getSupported = false;
                try
                {
                    getter(out r, out g, out b);
                    getSupported = true;
                }
                catch
                {
                    break;
                }
                if (getSupported)
                {
                    try
                    {
                        notifications[0].Expect(WICRawChangeNotification.WICRawChangeNotification_RGBWhitePoint);
                        notifications[1].Expect(WICRawChangeNotification.WICRawChangeNotification_RGBWhitePoint);
                        setter(r, g, b);
                        if (error.HasValue)
                        {
                            form.Add(this, setter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, error.Value));
                        }
                        else
                        {
                            if (n != null && !n.Notified)
                            {
                                form.Add(this, Resources.DevelopRawRule_NoNotification, de, new DataEntry(Resources.Actual, n.GetRaised()), new DataEntry(Resources.Expected, WICRawChangeNotification.WICRawChangeNotification_RGBWhitePoint), new DataEntry(Resources.Method, setter.ToString("{0}")), new DataEntry(Resources.Value, new uint[] { r, g, b }));
                            }
                            if (n != notifications[0] && notifications[0].Notified)
                            {
                                form.Add(this, Resources.DevelopRawRule_NotificationOnWrong, de, new DataEntry(Resources.Method, setter.ToString("{0}")), new DataEntry(Resources.Value, new uint[] { r, g, b }));
                            }

                            CheckGetSupported(form, true, getter, de, new uint[] { r, g, b });
                        }
                    }
                    catch (Exception e)
                    {
                        CheckGetSupported(form, true, getter, de, new uint[] { r, g, b });
                        if (error.HasValue)
                        {
                            form.CheckHRESULT(this, error.Value, e, de);
                        }
                        else
                        {
                            form.Add(this, setter.ToString(Resources._0_Failed), de, new DataEntry(e));
                        }
                    }
                }
                CheckPixelFormat(form, raw, de, pixelFormatOriginal);
            }
        }

        void CheckSetNamedWhitePointSupported(MainForm form, WinCodecError? error, WICNamedWhitePoint wp, IWICDevelopRaw raw, DataEntry[] de, Guid pixelFormatOriginal)
        {
            List<WICNamedWhitePoint> points = new List<WICNamedWhitePoint>();
            List<WICNamedWhitePoint> outOfRange = new List<WICNamedWhitePoint>();

            foreach (WICNamedWhitePoint p in Enum.GetValues(typeof(WICNamedWhitePoint)))
            {
                if ((uint)(p & wp) == 0 && !error.HasValue)
                {
                    outOfRange.Add(p);
                }
                else
                {
                    points.Add(p);
                }
            }
            CheckSetSupported(form, error, raw.GetNamedWhitePoint, raw.SetNamedWhitePoint, raw, WICRawChangeNotification.WICRawChangeNotification_NamedWhitePoint, de, pixelFormatOriginal, points.ToArray());
            CheckSetSupported(form, WinCodecError.WINCODEC_ERR_VALUEOUTOFRANGE, raw.GetNamedWhitePoint, raw.SetNamedWhitePoint, raw, WICRawChangeNotification.WICRawChangeNotification_NamedWhitePoint, de, pixelFormatOriginal, outOfRange.ToArray());
        }

        void CheckSetSupported<T>(MainForm form, WinCodecError? error, Func<T> getter, Action<T> setter, IWICDevelopRaw raw, WICRawChangeNotification mask, DataEntry[] de, Guid pixelFormatOriginal, params T[] values) where T : struct
        {
            Notification[] notifications = new Notification[] { new Notification(), new Notification(), null };

            foreach (Notification n in notifications)
            {
                try
                {
                    raw.SetNotificationCallback(n);
                }
                catch (Exception e)
                {
                    form.Add(this, n == null ? e.TargetSite.ToString(Resources._0_Failed, "NULL") : e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                }

                foreach (T value in values)
                {
                    T saved = default(T);
                    bool getSupported = false;
                    try
                    {
                        saved = getter();
                        getSupported = true;
                    }
                    catch
                    {
                        break;
                    }
                    if (getSupported)
                    {
                        try
                        {
                            notifications[0].Expect(mask);
                            notifications[1].Expect(mask);
                            setter(value);
                            if (error.HasValue)
                            {
                                form.Add(this, setter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, error.Value));
                            }
                            else
                            {
                                if (n != null && !n.Notified && saved.Equals(value))
                                {
                                    form.Add(this, Resources.DevelopRawRule_NoNotification, de, new DataEntry(Resources.Actual, n.GetRaised()), new DataEntry(Resources.Expected, mask), new DataEntry(Resources.Method, setter.ToString("{0}")), new DataEntry(Resources.Value, value));
                                }
                                if (n != notifications[0] && notifications[0].Notified)
                                {
                                    form.Add(this, Resources.DevelopRawRule_NotificationOnWrong, de, new DataEntry(Resources.Method, setter.ToString("{0}")), new DataEntry(Resources.Value, value));
                                }

                                CheckGetSupported<T>(form, true, getter, de, value);
                            }
                        }
                        catch (Exception e)
                        {
                            CheckGetSupported<T>(form, true, getter, de, saved);
                            if (error.HasValue)
                            {
                                DataEntry[] de2 = de.Clone() as DataEntry[];
                                Array.Resize(ref de2, de2.Length + 1);
                                de2[de2.Length - 1] = new DataEntry(Resources.Value, value);
                                form.CheckHRESULT(this, error.Value, e, de2);
                            }
                            else
                            {
                                form.Add(this, setter.ToString(Resources._0_Failed), de, new DataEntry(e), new DataEntry(Resources.Value, value));
                            }
                        }
                    }
                    CheckPixelFormat(form, raw, de, pixelFormatOriginal);
                }
            }
        }

        T? CheckGetSupported<T>(MainForm form, bool supported, Func<T> getter, DataEntry[] de, T? expected) where T : struct
        {
            T? value = null;
            try
            {
                value = getter();
                if (supported)
                {
                    if (expected.HasValue && !value.Equals(expected))
                    {
                        form.Add(this, getter.ToString(Resources._0_NotExpectedValue), de, new DataEntry(Resources.Expected, expected), new DataEntry(Resources.Actual, value));
                    }
                }
                else
                {
                    form.Add(this, getter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION));
                }
            }
            catch (Exception e)
            {
                if (supported)
                {
                    form.Add(this, getter.ToString(Resources._0_Failed), de, new DataEntry(e));
                }
                else
                {
                    form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, de);
                }
            }

            return value;
        }

        uint[] CheckGetSupported(MainForm form, bool supported, GetTriplet<uint> getter, DataEntry[] de, uint[] rgb)
        {
            uint[] value = null;
            try
            {
                uint r, g, b;
                getter(out r, out g, out b);
                if (supported)
                {
                    value = new uint[] { r, g, b };
                    if (rgb != null && (r != rgb[0] || g != rgb[1] || b != rgb[2]))
                    {
                        form.Add(this, getter.ToString(Resources._0_NotExpectedValue), de, new DataEntry(Resources.Expected, rgb), new DataEntry(Resources.Actual, value));
                    }
                }
                else
                {
                    form.Add(this, getter.ToString(Resources._0_ShouldFail), de, new DataEntry(Resources.Expected, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION));
                }
            }
            catch (Exception e)
            {
                if (supported)
                {
                    form.Add(this, getter.ToString(Resources._0_Failed), de, new DataEntry(e));
                }
                else
                {
                    form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, de);
                }
            }

            return value;
        }

        WICRawToneCurvePoint[] CheckGetSupported(MainForm form, bool supported, Func<uint, IntPtr, uint> getter, DataEntry[] de)
        {
            WICRawToneCurvePoint[] value = null;
            uint size = 0;
            IntPtr p = IntPtr.Zero;
            try
            {
                try
                {
                    size = getter(0, IntPtr.Zero);
                    if (supported)
                    {
                        if (size > 0)
                        {
                            p = Marshal.AllocCoTaskMem((int)size);
                        }
                    }
                    else
                    {
                        form.Add(this, getter.ToString(Resources._0_ShouldFail, "0, NULL, ..."), de, new DataEntry(Resources.Expected, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION));
                    }
                }
                catch (Exception e)
                {
                    if (supported)
                    {
                        form.Add(this, getter.ToString(Resources._0_Failed, "0, NULL, ..."), de, new DataEntry(e));
                    }
                    else
                    {
                        form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, "0, NULL, ...", de);
                    }
                }

                if (size > 0)
                {
                    try
                    {
                        uint newSize = getter(size, p);
                        if (newSize == size)
                        {
                            newSize = (uint)Marshal.ReadInt32(p);
                            value = PropVariantMarshaler.ToArrayOf<WICRawToneCurvePoint>(new IntPtr(p.ToInt64() + Marshal.SizeOf(newSize)), (int)newSize);
                        }
                        else
                        {
                            form.Add(this, getter.ToString(Resources._0_NotExpectedValue), de, new DataEntry(Resources.Expected, size), new DataEntry(Resources.Actual, newSize));
                        }
                    }
                    catch (Exception e)
                    {
                        form.Add(this, getter.ToString(Resources._0_Failed), de, new DataEntry(e));
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(p);
            }

            return value;
        }

        IWICColorContext[] CheckGetSupported(MainForm form, bool supported, Func<uint, IWICColorContext[], uint> getter, DataEntry[] de)
        {
            IWICColorContext[] value = null;
            uint size = 0;
            try
            {
                size = getter(0, null);
                if (supported)
                {
                    if (size > 0)
                    {
                        value = new IWICColorContext[size];

                        IWICImagingFactory factory = new WICImagingFactory() as IWICImagingFactory;
                        for (uint i = 0; i < size; i++)
                        {
                            value[i] = factory.CreateColorContext();
                        }
                        factory.ReleaseComObject();
                    }
                }
                else
                {
                    form.Add(this, getter.ToString(Resources._0_ShouldFail, "0, NULL, ..."), de, new DataEntry(Resources.Expected, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION));
                }
            }
            catch (Exception e)
            {
                if (supported)
                {
                    form.Add(this, getter.ToString(Resources._0_Failed, "0, NULL, ..."), de, new DataEntry(e));
                }
                else
                {
                    form.CheckHRESULT(this, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, e, "0, NULL, ...", de);
                }
            }

            if (size > 0)
            {
                try
                {
                    uint newSize = getter(size, value);
                    if (newSize == size)
                    {
                        int index = 0;
                        foreach (IWICColorContext c in value)
                        {
                            if (c == null)
                            {
                                form.Add(this, getter.ToString(Resources._0_NULLItem), de, new DataEntry(Resources.Index, index));
                            }
                            index++;
                        }
                    }
                    else
                    {
                        form.Add(this, getter.ToString(Resources._0_NotExpectedValue), de, new DataEntry(Resources.Expected, size), new DataEntry(Resources.Actual, newSize));
                    }
                }
                catch (Exception e)
                {
                    form.Add(this, getter.ToString(Resources._0_Failed), de, new DataEntry(e));
                }
            }

            return value;
        }

        internal static bool GetSupported(WICRawCapabilities capabilities)
        {
            return capabilities == WICRawCapabilities.WICRawCapabilityGetSupported 
                || capabilities == WICRawCapabilities.WICRawCapabilityFullySupported;
        }

        static bool GetSupported(WICRawRotationCapabilities capabilities)
        {
            return capabilities == WICRawRotationCapabilities.WICRawRotationCapabilityGetSupported
                || capabilities == WICRawRotationCapabilities.WICRawRotationCapabilityFullySupported
                || capabilities == WICRawRotationCapabilities.WICRawRotationCapabilityNinetyDegreesSupported;
        }

        void CheckPixelFormat(MainForm form, IWICDevelopRaw raw, DataEntry[] de, Guid pixelFormatOriginal)
        {
            Guid pixelFormat;
            raw.GetPixelFormat(out pixelFormat);

            if (pixelFormat != pixelFormatOriginal)
            {
                form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_NotExpectedValue, "IWICDevelopRaw::GetPixelFormat(...)"), de, new DataEntry(Resources.Expected, pixelFormatOriginal), new DataEntry(Resources.Actual, pixelFormat));
            }
        }

        protected override bool ProcessFrameDecode(MainForm form, IWICBitmapFrameDecode frame, DataEntry[] de, object tag)
        {
            Guid pixelFormatOriginal;
            frame.GetPixelFormat(out pixelFormatOriginal);

            IWICDevelopRaw raw = null;
            try
            {
                raw = (IWICDevelopRaw)frame;
                if (raw == null)
                {
                    form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_NULL, "IWICBitmapFrameDecode::QueryInterface(IID_IWICDevelopRaw, ...)"), de);
                }
                else
                {
                    CheckPixelFormat(form, raw, de, pixelFormatOriginal);

                    WICRawCapabilitiesInfo capabilities = new WICRawCapabilitiesInfo();
                    capabilities.cbSize = (uint)Marshal.SizeOf(capabilities);
                    try
                    {
                        raw.QueryRawCapabilitiesInfo(ref capabilities);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));

                        return true;
                    }
                    bool supports = false;
                    foreach (FieldInfo fi in typeof(WICRawCapabilitiesInfo).GetFields())
                    {
                        if (fi.FieldType == typeof(WICRawCapabilities) || fi.FieldType == typeof(WICRawRotationCapabilities))
                        {
                            if (!Enum.IsDefined(fi.FieldType, fi.GetValue(capabilities)))
                            {
                                form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources._0_UnexpectedFieldValue, fi.Name), de, new DataEntry(Resources.Expected, Enum.GetValues(fi.FieldType)), new DataEntry(Resources.Actual, fi.GetValue(capabilities)));
                            }
                            supports |= 0 != (uint)Convert.ChangeType(fi.GetValue(capabilities), typeof(uint));
                        }
                    }
                    if (!supports)
                    {
                        form.Add(this, Resources.DevelopRaw_NoSupportedFeatures, de);
                    }

                    CheckGetSupported<double>(form, GetSupported(capabilities.ContrastSupport), raw.GetContrast, de, 0);
                    CheckGetSupported(form, GetSupported(capabilities.DestinationColorProfileSupport), raw.GetColorContexts, de).ReleaseComObject();
                    CheckGetSupported<double>(form, GetSupported(capabilities.ExposureCompensationSupport), raw.GetExposureCompensation, de, 0);
                    CheckGetSupported<double>(form, GetSupported(capabilities.GammaSupport), raw.GetGamma, de, 1);
                    uint? kelvinWhitePoint = CheckGetSupported<uint>(form, GetSupported(capabilities.KelvinWhitePointSupport), raw.GetWhitePointKelvin, de, null);
                    WICNamedWhitePoint? namedWhitePoint = CheckGetSupported<WICNamedWhitePoint>(form, GetSupported(capabilities.NamedWhitePointSupport), raw.GetNamedWhitePoint, de, null);
                    CheckGetSupported<double>(form, GetSupported(capabilities.NoiseReductionSupport), raw.GetNoiseReduction, de, 0);
                    uint[] rgbWhitePoint = CheckGetSupported(form, GetSupported(capabilities.RGBWhitePointSupport), raw.GetWhitePointRGB, de, null);
                    WICRawRenderMode? renderingMode = CheckGetSupported<WICRawRenderMode>(form, GetSupported(capabilities.RenderModeSupport), raw.GetRenderMode, de, WICRawRenderMode.WICRawRenderModeNormal);
                    CheckGetSupported<double>(form, GetSupported(capabilities.RotationSupport), raw.GetRotation, de, 0);
                    CheckGetSupported<double>(form, GetSupported(capabilities.SaturationSupport), raw.GetSaturation, de, 0);
                    CheckGetSupported<double>(form, GetSupported(capabilities.SharpnessSupport), raw.GetSharpness, de, 0);
                    CheckGetSupported<double>(form, GetSupported(capabilities.TintSupport), raw.GetTint, de, 0);
                    WICRawToneCurvePoint[] toneCurve = CheckGetSupported(form, GetSupported(capabilities.ToneCurveSupport), raw.GetToneCurve, de);

                    CheckSetSupported<double>(form, SetSupported(capabilities.ContrastSupport), raw.GetContrast, raw.SetContrast, raw, WICRawChangeNotification.WICRawChangeNotification_Contrast, de, pixelFormatOriginal, -1, 0, 1);
                    CheckSetSupported<double>(form, SetSupported(capabilities.ExposureCompensationSupport), raw.GetExposureCompensation, raw.SetExposureCompensation, raw, WICRawChangeNotification.WICRawChangeNotification_ExposureCompensation, de, pixelFormatOriginal, 0, -5, 5);
                    CheckSetSupported<double>(form, SetSupported(capabilities.GammaSupport), raw.GetGamma, raw.SetGamma, raw, WICRawChangeNotification.WICRawChangeNotification_Gamma, de, pixelFormatOriginal, 1, 0.2, 5);
                    CheckSetSupported<double>(form, SetSupported(capabilities.NoiseReductionSupport), raw.GetNoiseReduction, raw.SetNoiseReduction, raw, WICRawChangeNotification.WICRawChangeNotification_NoiseReduction, de, pixelFormatOriginal, 0, 1);
                    CheckSetSupported<double>(form, SetSupported(capabilities.SaturationSupport), raw.GetSaturation, raw.SetSaturation, raw, WICRawChangeNotification.WICRawChangeNotification_Saturation, de, pixelFormatOriginal, 0, -1, 1);
                    CheckSetSupported<double>(form, SetSupported(capabilities.SharpnessSupport), raw.GetSharpness, raw.SetSharpness, raw, WICRawChangeNotification.WICRawChangeNotification_Sharpness, de, pixelFormatOriginal, 0, 1);
                    CheckSetSupported<double>(form, SetSupported(capabilities.TintSupport), raw.GetTint, raw.SetTint, raw, WICRawChangeNotification.WICRawChangeNotification_Tint, de, pixelFormatOriginal, 0, -1, 1);
                    CheckSetSupported<WICRawRenderMode>(form, SetSupported(capabilities.RenderModeSupport), raw.GetRenderMode, raw.SetRenderMode, raw, WICRawChangeNotification.WICRawChangeNotification_RenderMode, de, pixelFormatOriginal, WICRawRenderMode.WICRawRenderModeDraft, WICRawRenderMode.WICRawRenderModeNormal, WICRawRenderMode.WICRawRenderModeBestQuality);

                    try
                    {
                        uint max, min, step;
                        raw.GetKelvinRangeInfo(out min, out max, out step);
                        MethodInfo mi = typeof(IWICDevelopRaw).GetMethod("GetKelvinRangeInfo");
                        if ((max < min)
                            || (min == max && step != 0)
                            || (min != max && (step == 0 || ((max - min) % step) != 0)))
                        {
                            ParameterInfo[] pi = mi.GetParameters();
                            form.Add(this, mi.ToString(Resources._0_NotExpectedValue), de, new DataEntry(pi[0].Name, max), new DataEntry(pi[1].Name, min), new DataEntry(pi[2].Name, step));
                        }
                        else
                        {
                            uint[] goodTemps = min == max ? new uint[] { min } : new uint[] { min, min + step, max, max - step };
                            uint[] badTemps = step == 1 ? new uint[] { min - step, max + step } : new uint[] { min - step, max + step, min + step / 2, max - step / 2 };

                            CheckSetSupported<uint>(form, null, raw.GetWhitePointKelvin, raw.SetWhitePointKelvin, raw, WICRawChangeNotification.WICRawChangeNotification_KelvinWhitePoint, de, pixelFormatOriginal, goodTemps);
                            CheckSetSupported<uint>(form, WinCodecError.WINCODEC_ERR_VALUEOUTOFRANGE, raw.GetWhitePointKelvin, raw.SetWhitePointKelvin, raw, WICRawChangeNotification.WICRawChangeNotification_KelvinWhitePoint, de, pixelFormatOriginal, badTemps);
                        }
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                    }

                    CheckSetDestinationColorContextSupported(form, SetSupported(capabilities.DestinationColorProfileSupport), raw, de, pixelFormatOriginal);

                    switch(capabilities.RotationSupport)
                    {
                        case WICRawRotationCapabilities.WICRawRotationCapabilityFullySupported:
                            CheckSetSupported<double>(form, null, raw.GetRotation, raw.SetRotation, raw, WICRawChangeNotification.WICRawChangeNotification_Rotation, de, pixelFormatOriginal, rotation);
                            break;

                        case WICRawRotationCapabilities.WICRawRotationCapabilityNinetyDegreesSupported:
                            CheckSetSupported<double>(form, null, raw.GetRotation, raw.SetRotation, raw, WICRawChangeNotification.WICRawChangeNotification_Rotation, de, pixelFormatOriginal, rotation90);
                            CheckSetSupported<double>(form, WinCodecError.WINCODEC_ERR_VALUEOUTOFRANGE, raw.GetRotation, raw.SetRotation, raw, WICRawChangeNotification.WICRawChangeNotification_Rotation, de, pixelFormatOriginal, rotation90);
                            break;

                        default:
                            CheckSetSupported<double>(form, WinCodecError.WINCODEC_ERR_UNSUPPORTEDOPERATION, raw.GetRotation, raw.SetRotation, raw, WICRawChangeNotification.WICRawChangeNotification_Rotation, de, pixelFormatOriginal, rotation);
                            break;
                    }

                    CheckSetNamedWhitePointSupported(form, SetSupported(capabilities.NamedWhitePointSupport), capabilities.NamedWhitePointSupportMask, raw, de, pixelFormatOriginal);
                    CheckSetRgbWhitePointSupported(form, SetSupported(capabilities.RGBWhitePointSupport), raw, de, pixelFormatOriginal);
                    CheckSetToneCurveSupported(form, SetSupported(capabilities.ToneCurveSupport), raw, de, pixelFormatOriginal);

                    foreach (WICRawParameterSet ps in Enum.GetValues(typeof(WICRawParameterSet)))
                    {
                        try
                        {
                            raw.LoadParameterSet(ps);

                            IPropertyBag2 pb = null;
                            try
                            {
                                pb = raw.GetCurrentParameterSet();
                            }
                            catch (Exception e)
                            {
                                form.Add(this, e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e));
                            }
                            finally
                            {
                                pb.ReleaseComObject();
                            }
                        }
                        catch (Exception e)
                        {
                            form.Add(this, e.TargetSite.ToString(Resources._0_Failed), de, new DataEntry(e), new DataEntry(Resources.Value, ps));
                        }
                    }
                }
            }
            catch (InvalidCastException)
            {
                return false;
            }

            return true;
        }
    }
}
