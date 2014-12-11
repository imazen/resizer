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

using Microsoft.Test.Tools.WicCop.InteropServices;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.Decoder
{
    abstract class DecoderRuleBase : RuleBase<ComponentRuleGroup>
    {
        readonly static WinCodecError[] queryCapabilitiesErrors = new WinCodecError[] 
        {
            WinCodecError.WINCODEC_ERR_COMPONENTNOTFOUND,
            WinCodecError.WINCODEC_ERR_BADIMAGE,
            WinCodecError.WINCODEC_ERR_BADHEADER,
            WinCodecError.WINCODEC_ERR_UNKNOWNIMAGEFORMAT,
            WinCodecError.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT
        };

        protected DecoderRuleBase(string text)
            : base(text)
        {
        }

        protected virtual void QueryCapabilitiesError(MainForm form, string text, params DataEntry[] de)
        {
        }

        IEnumerable<string> GetDecodableFiles(MainForm form, Guid decoderClsid)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmapDecoderInfo info = null;
            IWICBitmapDecoder decoder = null;
            try
            {
                info = (IWICBitmapDecoderInfo)factory.CreateComponentInfo(decoderClsid);

                if (Settings.Default.Files != null)
                {
                    foreach (string s in Settings.Default.Files)
                    {
                        IWICStream stream = factory.CreateStream();
                        try
                        {
                            stream.InitializeFromFilename(s, NativeMethods.GenericAccessRights.GENERIC_READ);

                            bool matches = info.MatchesPattern(stream);
                            stream.Seek(0, 0, IntPtr.Zero);
                            decoder = info.CreateInstance();

                            if (decoder != null)
                            {
                                WICBitmapDecoderCapabilities? capabilities = null;

                                try
                                {
                                    capabilities = decoder.QueryCapability(stream);
                                    if (!matches)
                                    {
                                        QueryCapabilitiesError(form, string.Format(CultureInfo.CurrentUICulture, Resources.PatternDoesnotMatchButPassed, "IWICBitmapDecoder::QueryCapability(...)"), new DataEntry(s));
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (Array.IndexOf(queryCapabilitiesErrors,  (WinCodecError)Marshal.GetHRForException(e)) < 0)
                                    {
                                        QueryCapabilitiesError(form, e.TargetSite.ToString(Resources._0_FailedWithIncorrectHRESULT), new DataEntry(s), new DataEntry(Resources.Actual, e), new DataEntry(Resources.Expected, queryCapabilitiesErrors));
                                    }
                                }

                                if (capabilities.HasValue && 0 != (uint)(capabilities.Value & (WICBitmapDecoderCapabilities.WICBitmapDecoderCapabilityCanDecodeSomeImages | WICBitmapDecoderCapabilities.WICBitmapDecoderCapabilityCanDecodeAllImages)))
                                {
                                    yield return s;
                                }
                            }
                        }
                        finally
                        {
                            stream.ReleaseComObject();
                            decoder.ReleaseComObject();
                            decoder = null;

                        }
                    }
                }
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
                decoder.ReleaseComObject();
            }
        }

        protected virtual bool ProcessDecoder(MainForm form, IWICBitmapDecoder decoder, DataEntry[] de, object tag)
        {
            return true;
        }

        protected virtual bool ProcessFrameDecode(MainForm form, IWICBitmapFrameDecode frame, DataEntry[] de, object tag)
        {

            return true;
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            bool hasFile = false;
            bool processed = false;

            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmapDecoderInfo info = null;
            IWICBitmapDecoder decoder = null;
            try
            {
                info = (IWICBitmapDecoderInfo)factory.CreateComponentInfo(Parent.Clsid);
                foreach (string file in GetDecodableFiles(form, Parent.Clsid))
                {
                    decoder.ReleaseComObject();
                    decoder = info.CreateInstance();
                    hasFile = true;

                    IWICStream stream = factory.CreateStream();
                    stream.InitializeFromFilename(file, NativeMethods.GenericAccessRights.GENERIC_READ);
                    try
                    {
                        decoder.Initialize(stream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

                        if (ProcessDecoder(form, decoder, new DataEntry[] { new DataEntry(file) }, tag))
                        {
                            for (uint index = 0; index < decoder.GetFrameCount(); index++)
                            {
                                IWICBitmapFrameDecode frame = decoder.GetFrame(index);
                                try
                                {
                                    if (ProcessFrameDecode(form, frame, new DataEntry[] { new DataEntry(file), new DataEntry(index) }, tag))
                                    {
                                        processed = true;
                                    }
                                }
                                finally
                                {
                                    frame.ReleaseComObject();
                                }
                            }
                        }
                        else
                        {
                            processed = true;
                        }
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(file), new DataEntry(e));
                    }
                    finally
                    {
                        stream.ReleaseComObject();
                    }
                }
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
                decoder.ReleaseComObject();
            }

            if (!hasFile)
            {
                form.Add(Parent, string.Format(CultureInfo.CurrentUICulture, Resources.NoFilesFor_0, Parent.Text));
            }
            else if (!processed)
            {
                form.Add(this, string.Format(CultureInfo.CurrentUICulture, Resources.NoFilesFor_0, Text));
            }
        }
    }
}
