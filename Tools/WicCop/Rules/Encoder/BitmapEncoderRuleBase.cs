//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Globalization;

using Microsoft.Test.Tools.WicCop.InteropServices;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop.Rules.Encoder
{
    abstract class BitmapEncoderRuleBase : RuleBase<ComponentRuleGroup>
    {
        protected BitmapEncoderRuleBase(string text)
            : base(text)
        {
        }

        protected virtual bool ProcessEncoder(MainForm form, IWICBitmapEncoder encoder, object tag)
        {
            return false;
        }

        protected override void RunOverride(MainForm form, object tag)
        {
            TempFileCollection files = null;
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();
            IWICBitmapEncoderInfo info = null;
            IWICBitmapEncoder encoder = null;
            IWICStream stream = null;
            int i = 0;
            try
            {
                files = new TempFileCollection();
                info = (IWICBitmapEncoderInfo)factory.CreateComponentInfo(Parent.Clsid);

                do 
                {
                    stream.ReleaseComObject();
                    encoder.ReleaseComObject();
                    stream = factory.CreateStream();
                    stream.InitializeFromFilename(files.AddExtension(i.ToString(CultureInfo.InvariantCulture)), NativeMethods.GenericAccessRights.GENERIC_WRITE);
                    i++;
                    try
                    {
                        encoder = info.CreateInstance();
                        encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
                    }
                    catch (Exception e)
                    {
                        form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
                        break;
                    }
                }
                while(ProcessEncoder(form, encoder, tag));
            }
            finally
            {
                factory.ReleaseComObject();
                info.ReleaseComObject();
                encoder.ReleaseComObject();
                stream.ReleaseComObject();
                if (files != null)
                {
                    files.Delete();
                }
            }
        }
    }
}
