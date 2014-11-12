using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ImageResizer.Resizing;
using ImageResizer.Util;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using ImageResizer.ExtensionMethods;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.SeamCarving {
    /// <summary>
    /// This plugin provides content-aware image resizing and 5 different algorithms.
    /// </summary>
    public class SeamCarvingPlugin : BuilderExtension, IQuerystringPlugin, IPlugin {

        CairManager cair = new CairManager();
        /// <summary>
        /// Creates a new instance of SeamCarvingPlugin
        /// </summary>
        public SeamCarvingPlugin() {
            Timeout = 5000;
        }

        public SeamCarvingPlugin(NameValueCollection args):this()
        {
            Timeout = args.Get<int>("timeout", Timeout);
        }

        public int Timeout { get; set; }

        public enum FilterType {
            None = 10,
            Prewitt = 0,
            V1 = 1,
            VSquare = 2,
            Sobel = 3,
            Laplacian = 4
        }

        public enum OutputType {
            CAIR=  0,
            Grayscale =  1,
            Edge=  2,
            VerticalEnergy =  3,
            HorizontalEnergy =  4,
            Removal= 5,
            CAIR_HD=  6

        }
        public enum EnergyType {
            Backward = 0,
            Forward = 1
        }

        private const string CarveData = "CarveData";

        protected override RequestedAction LayoutImage(ImageState s) {
            if (s.sourceBitmap == null) return RequestedAction.None;


            //Parse carve data bitmap
            if (!string.IsNullOrEmpty(s.settings["carve.data"])) {
                string[] parts = s.settings["carve.data"].Split('|');
                //Parse block count and string
                var block_count = int.Parse(parts[0]);
                var carveString = new LzwDecoder("012").Decode(PathUtils.FromBase64UToBytes(parts[1]));

                float block_size = (int)Math.Floor(Math.Sqrt(s.originalSize.Width * s.originalSize.Height / (double)block_count));

                var carveData = new CarveDataPlotter() {
                    BlockCount=block_count,
                    Stride = (int)Math.Ceiling((float)s.originalSize.Width / block_size),
                    Rows = (int)Math.Ceiling((float)s.originalSize.Height / block_size)
                };

                carveData.Init(carveString);

                Size remove = carveData.GetRemovalSpace(s.originalSize.Width,s.originalSize.Height,(int)block_size);

                if (remove.Width / s.originalSize.Width > remove.Height / s.originalSize.Height) {
                    s.originalSize = new Size(s.originalSize.Width - remove.Width, s.originalSize.Height);
                } else {
                    s.originalSize = new Size(s.originalSize.Width, s.originalSize.Height - remove.Height);
                }
                
                //Save later
                s.Data[CarveData] = carveData;
            }



            return RequestedAction.None;

        }


        protected override RequestedAction PostLayoutImage(ImageState s) {
            
            return RequestedAction.None;
        }



        protected override RequestedAction PreRenderImage(ImageState s) {
            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            s.ApplyCropping();
            s.EnsurePreRenderBitmap();

            //Parse carve algorithm kind
            FilterType ftype = s.settings.Get<FilterType>("carve", FilterType.None);
            if ("true".Equals(s.settings["carve"], StringComparison.OrdinalIgnoreCase)) ftype = FilterType.Prewitt;
            if (string.IsNullOrEmpty(s.settings["carve"]) && s.settings.Mode == FitMode.Carve) ftype = FilterType.Prewitt;

            //If we have carve data
            CarveDataPlotter carveData = s.Data.ContainsKey(CarveData) ? (s.Data[CarveData] as CarveDataPlotter) : null;
            if (carveData != null && ftype == FilterType.None) ftype = FilterType.Prewitt;

            RectangleF copyRect = s.copyRect;

            if (carveData != null) copyRect = new RectangleF(new PointF(0, 0), s.sourceBitmap.Size);

            if (ftype == FilterType.None) return RequestedAction.None; //Only override rendering when carving is requested.

            //The minimum dimensions of the temporary bitmap.
            SizeF targetSize = PolygonMath.getParallelogramSize(s.layout["image"]);
            targetSize = new SizeF((float)Math.Ceiling(targetSize.Width), (float)Math.Ceiling(targetSize.Height));

  
            //The size of the temporary bitmap. 
            //We want it larger than the size we'll use on the final copy, so we never upscale it
            //- but we also want it as small as possible so processing is fast.
            SizeF tempSize = PolygonMath.ScaleOutside(targetSize, copyRect.Size);
            int tempWidth = (int)Math.Ceiling(tempSize.Width);
            int tempHeight = (int)Math.Ceiling(tempSize.Height);

            //The intermediate and seam carved files
            string tempFile = Path.GetTempFileName();
            string outputTempFile = Path.GetTempFileName();

            try {
                try {

                    //Create a temporary bitmap that is 'halfway resized', so we can efficiently perfom seam carving.

                    //Unless it's already been done for us by FreeImageResize or something
                    if (s.preRenderBitmap != null && (tempWidth - s.preRenderBitmap.Width < 50 && tempHeight - s.preRenderBitmap.Height < 50)) {
                        s.preRenderBitmap.Save(tempFile, ImageFormat.Bmp);
                        tempWidth = s.preRenderBitmap.Width;
                        tempHeight = s.preRenderBitmap.Height;
                    } else {
                        //Create the temporary bitmap and graphics.
                        using (Bitmap temp = new Bitmap(tempWidth, tempHeight, PixelFormat.Format32bppArgb))
                        using (Graphics g = Graphics.FromImage(temp))
                        using (ImageAttributes ia = new ImageAttributes()) {
                            //High quality everthing
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.CompositingMode = CompositingMode.SourceOver;
                            ia.SetWrapMode(WrapMode.TileFlipXY);
                            if (s.preRenderBitmap != null) {
                                g.DrawImage(s.preRenderBitmap, new Rectangle(0, 0, tempWidth, tempHeight), 0, 0, s.preRenderBitmap.Width, s.preRenderBitmap.Height, GraphicsUnit.Pixel, ia);
                            } else {
                                g.DrawImage(s.sourceBitmap, new Rectangle(0, 0, tempWidth, tempHeight), copyRect.X, copyRect.Y, copyRect.Width, copyRect.Height, GraphicsUnit.Pixel, ia);
                            }
                            g.Flush(FlushIntention.Flush);
                            //Save
                            temp.Save(tempFile, ImageFormat.Bmp);
                        }
                    }

                    string maskFile = carveData != null ? Path.GetTempFileName() : null;
                    try {
                        if (carveData != null)
                            carveData.SaveBitmapAs(maskFile, tempWidth, tempHeight);
                        
                        Size intTargetSize = new Size((int)targetSize.Width, (int)targetSize.Height);
                        CairJob job = new CairJob();
                        if (maskFile != null) job.WeightPath = maskFile;
                        job.SourcePath = tempFile;
                        job.DestPath = outputTempFile;
                        job.Size = intTargetSize;
                        job.Filter = ftype;
                        job.Timeout = Timeout;
                        cair.CairyIt(job);
                    } finally {
                        if (maskFile != null) File.Delete(maskFile);
                    }

                } finally {
                    File.Delete(tempFile);
                }

                //Dispose old intermediate bitmap first
                if (s.preRenderBitmap != null) s.preRenderBitmap.Dispose();

                //Load the new intermediate file from disk
                s.preRenderBitmap = new Bitmap(outputTempFile);
                s.preRenderBitmap.MakeTransparent();
                
                //Reset the s.copyRect to match the new bitmap
                s.copyRect = new RectangleF(new PointF(0,0), new SizeF(targetSize.Width, targetSize.Height));

            } finally {
                File.Delete(outputTempFile);
            }

            return RequestedAction.Cancel;
        }


        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "carve" };
        }

        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }


        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
