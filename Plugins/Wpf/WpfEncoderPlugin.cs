using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Plugins.Basic;
using ImageResizer.Encoding;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageResizer.Plugins.WpfBuilder
{
    public class WpfEncoderPlugin : DefaultEncoder, IPlugin, IEncoder
    {
        private ResizeSettings localSettings { get; set; }

        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public WpfEncoderPlugin(ResizeSettings settings, object original)
            : base(settings, original) 
        {
            localSettings = settings;
        }

        public override IEncoder CreateIfSuitable(ResizeSettings settings, object original)
        {
            /* TODO */
            return new WpfEncoderPlugin(settings, original);
        }

        public void Write(BitmapSource i, Stream s) 
        {
            BitmapEncoder encoder = null;

            if (MimeType.Equals("image/jpeg"))
            {
                encoder = new JpegBitmapEncoder();
                ((JpegBitmapEncoder)encoder).QualityLevel = localSettings.Quality;
            }
            else if (MimeType.Equals("image/png"))
            {
                encoder = new PngBitmapEncoder();
            }
            else if (MimeType.Equals("image/gif"))
            {
                encoder = new GifBitmapEncoder();
                encoder.Palette = new BitmapPalette(i, 256);
            }

            encoder.Frames.Add(BitmapFrame.Create(i));

            using (MemoryStream outputStream = new MemoryStream())
            {
                encoder.Save(outputStream);
                outputStream.WriteTo(s);
            }
        }
    }
}
