using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration.Issues;
using ImageResizer.Util;
using ImageResizer.Plugins.Basic;
using System.IO;
using ImageResizer.Configuration;
using System.Web.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Windows;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.WpfBuilder
{
    public class WpfImageSettings 
    {
        /// <summary>
        /// The x offset (positive or negative) used to draw the image rect inside the DrawingContext
        /// </summary>
        public float OffsetX { get; set; }

        /// <summary>
        /// The y offset (positive or negative) used to draw the image rect inside the DrawingContext
        /// </summary>
        public float OffsetY { get; set; }

        /// <summary>
        /// The final visible image width
        /// </summary>
        public float DestinationImageCanvasWidth { get; set; }

        /// <summary>
        /// The final visible image height
        /// </summary>
        public float DestinationImageCanvasHeight { get; set; }

        /// <summary>
        /// The width of the image inside the rect drawned into the DrawingContext
        /// </summary>
        public float DestinationImageWidth { get; set; }

        /// <summary>
        /// The height of the image inside the rect drawned into the DrawingContext
        /// </summary>
        public float DestinationImageHeight { get; set; }

        public WpfImageSettings() { }
    }

    public class WpfBuilderPlugin : BuilderExtension, IPlugin, IIssueProvider, IFileExtensionPlugin
    {
        Config c;
        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        private void calculateImageSize(ImageState imageState, out int destWidth, out int destHeight) 
        {
            destWidth = 0;
            destHeight = 0;

            destHeight = Convert.ToInt32(imageState.layout["image"][3].Y - imageState.layout["image"][0].Y);
            destWidth = Convert.ToInt32(imageState.layout["image"][1].X - imageState.layout["image"][3].X);
        }

        protected override RequestedAction BuildJob(ImageJob job)
        {
            if (!"wpf".Equals(job.Settings["builder"])) return RequestedAction.None;

            // Estrazione delle ResizeSettings
            ResizeSettings settings = job.Settings;


            Stream s = null;
            bool disposeStream = !(job.Source is Stream);
            long originalPosition = 0;
            bool restoreStreamPosition = false;

            string path;
            s = c.CurrentImageBuilder.GetStreamFromSource(job.Source, job.Settings, ref disposeStream, out path, out restoreStreamPosition);
            if (s == null) return RequestedAction.None; //We don't support the source object!
            if (job.ResetSourceStream) restoreStreamPosition = true;
            job.SourcePathData = path;

            // Instanzio uno stream locale per le operazioni WPF
            using (MemoryStream localStream = (s is MemoryStream) ? (MemoryStream)s : StreamUtils.CopyStream(s)) 
            {
                if (s != null && restoreStreamPosition && s.CanSeek) 
                    s.Seek(originalPosition, SeekOrigin.Begin);

                if (disposeStream) 
                    s.Dispose();

                /* ? ? ? */
                IEncoder managedEncoder = c.Plugins.GetEncoder(job.Settings, job.SourcePathData);
                bool supportsTransparency = managedEncoder.SupportsTransparency;

                // Recupero le dimensioni originali
                var frame = BitmapFrame.Create(StreamUtils.CopyStream(localStream));
                System.Windows.Size originalSize = new System.Windows.Size(frame.PixelWidth, frame.PixelHeight);

                // Resetto lo stream locale alla posizione iniziale, dopo aver letto i metadata
                localStream.Position = 0;



                // Uhm... sono costretto a referenziare le System.Drawing (GDI) per questo,
                // TODO: chiedere al tipo se si può prevedere un costruttore di ImageState che non preveda un System.Drawing.Size come parametro
                System.Drawing.Size orig = new System.Drawing.Size((int)originalSize.Width, (int)originalSize.Height);

                using (ImageState imageState = new ImageState(settings, orig, true))
                {
                    c.CurrentImageBuilder.Process(imageState);

                    Rectangle imageDest = PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(imageState.layout["image"]));

                    BitmapSource finalImage;

                    BitmapImage bi = new BitmapImage();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.BeginInit();
                    bi.StreamSource = localStream;


                    WpfImageSettings wpfImageSettings = imageState.WpfDestinationImageSettings(settings);

                    bi.DecodePixelWidth = Convert.ToInt32(wpfImageSettings.DestinationImageWidth);
                    bi.DecodePixelHeight = Convert.ToInt32(wpfImageSettings.DestinationImageHeight);
                    bi.EndInit();

                    // Creation of the encoder
                    WpfEncoderPlugin wpfEncoder = new WpfEncoderPlugin(settings, job.SourcePathData);


                    RenderTargetBitmap final = new RenderTargetBitmap(imageState.finalSize.Width, imageState.finalSize.Height, settings.Get<int>("dpi", 96), settings.Get<int>("dpi", 96), PixelFormats.Default);
                    DrawingVisual dv = new DrawingVisual();

                    using (DrawingContext dc = dv.RenderOpen())
                    {
                        string ARGBBackgroundColor = String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", wpfEncoder.MimeType.Equals("image/jpeg") ? 255 : settings.BackgroundColor.A, 
                                                                                                settings.BackgroundColor.R, 
                                                                                                settings.BackgroundColor.G, 
                                                                                                settings.BackgroundColor.B);

                        System.Windows.Media.Brush BrushBackgroundColor = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ARGBBackgroundColor));

                        /* todo: verificare */
                        dc.DrawRectangle(BrushBackgroundColor, null, new Rect(0, 0, wpfImageSettings.DestinationImageWidth, wpfImageSettings.DestinationImageHeight));

                        Rect rect = new Rect(wpfImageSettings.OffsetX, wpfImageSettings.OffsetY, wpfImageSettings.DestinationImageWidth, wpfImageSettings.DestinationImageHeight);

                        //dc.PushTransform(new RotateTransform(settings.Rotate, (double)imageState.finalSize.Width / 2, (double)imageState.finalSize.Height / 2));
                        
                        dc.DrawImage(bi, rect);
                    }
                    
                    final.Render(dv);
                    finalImage = final;

                    // Write the image to the output stream
                    wpfEncoder.Write(finalImage, (Stream)job.Dest);
                }
            }
            
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutImage(ImageState s) 
        {
            base.LayoutImage(s);

            return RequestedAction.None;
        }


        public WpfBuilderPlugin() { }


        #region IIssueProvider Members

        public IEnumerable<IIssue> GetIssues()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IFileExtensionPlugin Members

        public IEnumerable<string> GetSupportedFileExtensions()
        {
            return new string[] { };
        }

        #endregion
    }
}

