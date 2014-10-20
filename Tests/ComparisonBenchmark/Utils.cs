using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using WicResize.InteropServices;
using System.Runtime.InteropServices;

namespace Benchmark {
    public class Utils {


        public static byte[] WicResize(IWICComponentFactory factory, byte[] photoBytes, int width, int height, int quality) {
            IWICBitmapDecoder decoder = null ;
            var inputStream = factory.CreateStream();
            try {
                
                inputStream.InitializeFromMemory(photoBytes, (uint)photoBytes.Length);
                decoder = factory.CreateDecoderFromStream(inputStream, null,
                                                              WICDecodeOptions.WICDecodeMetadataCacheOnLoad);
                return WicResize(factory, decoder.GetFrame(0), width, height, quality);
            } finally {
                Marshal.ReleaseComObject(decoder);
                Marshal.ReleaseComObject(inputStream);
            }
        }
        

        public static byte[] WicResize(IWICComponentFactory factory, IWICBitmapFrameDecode frame, int width,
                                       int height, int quality) {
            // Prepare output stream to cache file
            var outputStream = new MemoryIStream();
            // Prepare PNG encoder
            var encoder = factory.CreateEncoder(Consts.GUID_ContainerFormatJpeg, null);
            encoder.Initialize(outputStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
            // Prepare output frame
            IWICBitmapFrameEncode outputFrame;
            var arg = new IPropertyBag2[1];
            encoder.CreateNewFrame(out outputFrame, arg);
            var propBag = arg[0];
            var propertyBagOption = new PROPBAG2[1];
            propertyBagOption[0].pstrName = "ImageQuality";
            propBag.Write(1, propertyBagOption, new object[] {((float) quality)/100});
            outputFrame.Initialize(propBag);
            double dpiX, dpiY;
            frame.GetResolution(out dpiX, out dpiY);
            outputFrame.SetResolution(dpiX, dpiY);

            uint ow, oh, w, h;
            frame.GetSize(out ow, out oh);
            if (ow > oh ) {
                w = (uint)width;
                h = (uint)((double)height * (double)oh / (double)ow);
            } else {
                w = (uint)((double)height * (double)ow / (double)oh);
                h = (uint)height;
            }


            outputFrame.SetSize(w, h);
            // Prepare scaler
            var scaler = factory.CreateBitmapScaler();
            scaler.Initialize(frame, w, h, WICBitmapInterpolationMode.WICBitmapInterpolationModeFant);
            // Write the scaled source to the output frame
            outputFrame.WriteSource(scaler, new WICRect {X = 0, Y = 0, Width = (int) width, Height = (int) height});
            outputFrame.Commit();
            encoder.Commit();
            var outputArray = outputStream.ToArray();
            outputStream.Close();
            Marshal.ReleaseComObject(outputFrame);
            Marshal.ReleaseComObject(scaler);
            Marshal.ReleaseComObject(propBag);
            Marshal.ReleaseComObject(encoder);
            return outputArray;
        }

        public static BitmapFrame ReadWpfBitmapFrame(MemoryStream photoStream) {
            var photoDecoder = BitmapDecoder.Create(
                photoStream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.None);
            return photoDecoder.Frames[0];
        }

        public static BitmapFrame WpfResize(BitmapFrame photo, int width, int height) {
            var target = new TransformedBitmap(
                photo,
                new ScaleTransform(
                    width/photo.Width*96/photo.DpiX,
                    height/photo.Height*96/photo.DpiY,
                    0, 0));
            return BitmapFrame.Create(target);
        }

        public static byte[] ToByteArrayWpf(BitmapFrame targetFrame, int quality) {
            byte[] targetBytes;
            using (var memoryStream = new MemoryStream()) {
                var targetEncoder = new JpegBitmapEncoder {
                                                              QualityLevel = quality
                                                          };
                targetEncoder.Frames.Add(targetFrame);
                targetEncoder.Save(memoryStream);
                targetBytes = memoryStream.ToArray();
            }
            return targetBytes;
        }

        public static Bitmap GdiResize(Image photo, int width, int height,
                                        InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic,
                                        SmoothingMode smoothingMode = SmoothingMode.HighQuality,
                                        PixelOffsetMode pixelMode = PixelOffsetMode.HighQuality,
            CompositingQuality compositingQuality = CompositingQuality.HighQuality,
            CompositingMode compositingMode = CompositingMode.SourceOver
            ) {
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized)) {
                graphics.CompositingQuality = compositingQuality;
                graphics.InterpolationMode = interpolationMode;
                graphics.CompositingMode = compositingMode;
                graphics.SmoothingMode = smoothingMode;
                graphics.PixelOffsetMode = pixelMode;

                graphics.DrawImage(photo, 0, 0, width, height);
            }
            return resized;
        }

        public static byte[] ToByteArrayGdi(Bitmap resized, int quality) {
            using (var memoryStream = new MemoryStream()) {
                var codec = ImageCodecInfo.GetImageDecoders()
                    .Where(c => c.FormatID == ImageFormat.Jpeg.Guid)
                    .FirstOrDefault();
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                if (codec != null) {
                    resized.Save(memoryStream, codec, encoderParams);
                }
                return memoryStream.ToArray();
            }
        }
    }
}