//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.Test.Tools.WicCop.InteropServices;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop
{
    public partial class PerformanceForm : Form
    {
        private IWICBitmapDecoder decoder = null;
        private IWICBitmapFrameDecode frame = null;
        private bool redraw = false;
        private bool setupMode = false;

        public PerformanceForm()
        {
            InitializeComponent();

            filesListView.SuspendLayout();

            if (Settings.Default.Files != null)
            {
                foreach (string file in Settings.Default.Files)
                {
                    filesListView.Items.Add(file);
                }
            }

            filesListView.ResumeLayout();
            filesListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            filesListView.Columns[0].Width = Math.Max(filesListView.Width, filesListView.Columns[0].Width);
        }

        private void filesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count > 0)
            {

                string file = filesListView.SelectedItems[0].Text;

                if (decoder != null)
                {
                    decoder.ReleaseComObject();
                    decoder = null;
                }

                if (frame != null)
                {
                    frame.ReleaseComObject();
                    frame = null;
                }

                IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();

                decoder = factory.CreateDecoderFromFilename(file, null, NativeMethods.GenericAccessRights.GENERIC_READ, WICDecodeOptions.WICDecodeMetadataCacheOnLoad);

                if (decoder != null)
                {
                    uint frameCount = decoder.GetFrameCount();

                    if (frameCount > 0)
                    {
                        frame = decoder.GetFrame(0);
                    }

                    if (frame != null)
                    {
                        setupMode = true;
                        SetupDevelopRaw();
                        setupMode = false;

                        DisplayImage();
                    }
                }

                factory.ReleaseComObject();
            }
        }

        private void DisplayImage()
        {
            if (!setupMode)
            {
                lock (this)
                {
                    redraw = true;
                }
                if (!backgroundWorker.IsBusy)
                {
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        private void DisplayImageInternal()
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactory();

            IWICBitmapScaler scaler = factory.CreateBitmapScaler();
            IWICFormatConverter formatConvert = factory.CreateFormatConverter();
            GCHandle h = new GCHandle();
            Image image = rawPictureBox.Image;

            try
            {
                uint pixelColorWidth = 3; // 3 bytes/channel for Consts.GUID_WICPixelFormat24bppRGB, or more generally (((bits / pixel) + 7) / 8)

                uint width = (uint)rawPictureBox.Width;
                uint height = (uint)rawPictureBox.Height;

                scaler.Initialize(frame, width, height, WICBitmapInterpolationMode.WICBitmapInterpolationModeFant);

                formatConvert.Initialize(scaler, Consts.GUID_WICPixelFormat24bppBGR, WICBitmapDitherType.WICBitmapDitherTypeNone, null, 0.0, WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut);

                uint stride = width * pixelColorWidth;
                uint size = stride * height;

                byte[] pixels = new byte[size];

                formatConvert.CopyPixels(null, stride, size, pixels);

                h = GCHandle.Alloc(pixels, GCHandleType.Pinned);

                Bitmap bitmap = new Bitmap((int)width, (int)height, (int)stride, PixelFormat.Format24bppRgb, h.AddrOfPinnedObject());

                rawPictureBox.Image = bitmap;
            }
            catch
            {
            }
            finally
            {
                if (image != null)
                {
                    image.Dispose();
                }
                if (h.IsAllocated)
                {
                    h.Free();
                }
                scaler.ReleaseComObject();
                formatConvert.ReleaseComObject();
                factory.ReleaseComObject();
            }
        }

        private T? GetValue<T>(bool supported, Func<T> getter) where T : struct
        {
            T? value = null;

            try
            {
                if (supported)
                {
                    value = getter();
                }
            }
            catch 
            {
            }

            return value;
        }

        private void SetupTrackBar<T>(TrackBar trackBar, T? value) where T : struct
        {
            trackBar.Enabled = false;

            if (value.HasValue)
            {
                int multiple = (trackBar.Maximum == 10) ? 10 : 1;
                if (value.HasValue)
                {
                    int intValue = Convert.ToInt32(value.Value) * multiple;

                    if (intValue < trackBar.Minimum)
                    {
                        trackBar.Minimum = intValue;
                    }

                    if (intValue > trackBar.Maximum)
                    {
                        trackBar.Maximum = intValue;
                    }

                    trackBar.Enabled = true;
                    trackBar.Value = intValue;
                }
            }
        }

        private void SetupDevelopRaw()
        {
            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                WICRawCapabilitiesInfo capabilities = new WICRawCapabilitiesInfo();
                capabilities.cbSize = (uint)Marshal.SizeOf(capabilities); 
                raw.QueryRawCapabilitiesInfo(ref capabilities);

                SetupTrackBar<double>(exposureTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.ExposureCompensationSupport), raw.GetExposureCompensation));
                SetupTrackBar<double>(contrastTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.ContrastSupport), raw.GetContrast));
                SetupTrackBar<double>(sharpnessTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.SharpnessSupport), raw.GetSharpness));
                SetupTrackBar<double>(tintTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.TintSupport), raw.GetTint));
                SetupTrackBar<double>(gammaTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.GammaSupport), raw.GetGamma));
                SetupTrackBar<double>(saturationTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.SaturationSupport), raw.GetSaturation));
                SetupTrackBar<double>(noiseReductionTrackBar, GetValue<double>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.NoiseReductionSupport), raw.GetNoiseReduction));

                if (Rules.Decoder.DevelopRawRule.SetSupported(capabilities.RenderModeSupport) == null)
                {
                    renderModeComboBox.Enabled = true;
                    renderModeComboBox.SelectedIndex = 1;
                    ChangeValue<WICRawRenderMode>(raw.SetRenderMode, WICRawRenderMode.WICRawRenderModeNormal);
                }

                if (Rules.Decoder.DevelopRawRule.GetSupported(capabilities.KelvinWhitePointSupport))
                {
                    uint minTemp = 1500;
                    uint maxTemp = 30000;
                    uint stepTemp = 1;
                    try
                    {
                        raw.GetKelvinRangeInfo(out minTemp, out maxTemp, out stepTemp);
                    }
                    catch
                    {
                    }

                    temperatureTrackBar.Minimum = Convert.ToInt32(minTemp);
                    temperatureTrackBar.Maximum = Convert.ToInt32(maxTemp);
                    temperatureTrackBar.TickFrequency = Convert.ToInt32(stepTemp);

                    SetupTrackBar<uint>(temperatureTrackBar, GetValue<uint>(Rules.Decoder.DevelopRawRule.GetSupported(capabilities.KelvinWhitePointSupport), raw.GetWhitePointKelvin));
                }
            }
        }

        private void ChangeValue<T>(Action<T> setter, T value) where T : struct
        {
            try
            {
                setter(value);
            }
            catch
            {
            }

            DisplayImage();
        }

        private void temperatureTrackBar_ValueChanged(object sender, EventArgs e)
        {
            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<uint>(raw.SetWhitePointKelvin, (uint)temperatureTrackBar.Value);
            }
        }

        private void exposureTrackBar_ValueChanged(object sender, EventArgs e)
        {
            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetExposureCompensation, (double)exposureTrackBar.Value);
            }
        }

        private void gammaTrackBar_ValueChanged(object sender, EventArgs e)
        {
            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetGamma, (double)gammaTrackBar.Value);
            }
        }

        private void contrastTrackBar_ValueChanged(object sender, EventArgs e)
        {
            double value = contrastTrackBar.Value / 10.0;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetContrast, value);
            }
        }

        private void sharpnessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            double value = sharpnessTrackBar.Value / 10.0;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetSharpness, value);
            }
        }

        private void noiseReductionTrackBar_ValueChanged(object sender, EventArgs e)
        {
            double value = noiseReductionTrackBar.Value / 10.0;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetNoiseReduction, value);
            }
        }

        private void saturationTrackBar_ValueChanged(object sender, EventArgs e)
        {
            double value = saturationTrackBar.Value / 10.0;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetSaturation, value);
            }
        }

        private void tintTrackBar_ValueChanged(object sender, EventArgs e)
        {
            double value = tintTrackBar.Value / 10.0;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;

            if (raw != null)
            {
                ChangeValue<double>(raw.SetTint, value);
            }
        }

        private void renderModeComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            WICRawRenderMode renderMode = (WICRawRenderMode)renderModeComboBox.SelectedIndex;

            IWICDevelopRaw raw = frame as IWICDevelopRaw;
            
            if (raw != null)
            {
                ChangeValue<WICRawRenderMode>(raw.SetRenderMode, renderMode);
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                lock (this)
                {
                    redraw = false;
                }
                DisplayImageInternal();
            }
            while (redraw);
        }
    }
}
