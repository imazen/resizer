using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageStudio.Library
{
    public class ImageManipulation
    {
        #region - Private Variables -
        private const string gif = "gif";
        private const string jpg = "jpg";
        private const string jpeg = "jpeg";
        private const string png = "png";
        private const string bmp = "bmp";
        private const string emf = "emf";
        private const string exif = "exif";
        private const string icon = "icon;ico";
        private const string tiff = "tiff";
        private const string wmf = "wmf";
        #endregion

        #region - Public Varibles -
        public const char DimensionDelimiter = 'x';
        public const string FileExtensionDelimiter = ".";
        #endregion

        /// <summary>
        /// Allows the aspect ratio to be maintained, this parameter is only used
        /// if either height or width are zero
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="smallSizeImagePath"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="maintainAspectRatio"></param>
        /// <returns></returns>
        public static bool ResizeWithRatio(string imagePath, int height, int width)
        {
            // if we want to maintain the aspect ratio, then we only use one of the height/width parameters
            // e.g. if height is 300, then we set the height to be 300, and calculate the value of width
            // in order to maintain the aspect ratio. And v.v.
            // So, either height or width must be zero
            using (Image fullSizeImage = Image.FromFile(imagePath))
            {
                double resizePercent = 1.0;
                if (width != 0)
                {
                    // we have been passed the width parameter.
                    resizePercent = Convert.ToDouble(width) / Convert.ToDouble(fullSizeImage.Width);
                    // now work out what the final height should be
                    height = Convert.ToInt32(fullSizeImage.Height * resizePercent);
                }
                else
                {
                    // we have been passed the height parameter.
                    resizePercent = Convert.ToDouble(height) / Convert.ToDouble(fullSizeImage.Height);
                    // now work out what the final width should be
                    width = Convert.ToInt32(fullSizeImage.Width * resizePercent);
                }
            }

            return Resize(imagePath, height, width);
        }

        /// <summary>
        /// Gets the resolution.
        /// </summary>
        /// <param name="imagePath">The image path.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public static void GetResolution(string imagePath, out int width, out int height)
        {
            using (Image fullSizeImage = Image.FromFile(imagePath))
            {
                width = fullSizeImage.Width;
                height = fullSizeImage.Height;
            }
        }

        /// <summary>
        /// Use this method when need to re-size high quality image to a smaller high quality image, it overwrites existing image if it exists if dosent it creates
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static bool Resize(string imagePath, int height, int width)
        {

            Bitmap nImage = null;

            using (Image fullSizeImage = Image.FromFile(imagePath))
            {
                using (Bitmap originalImage = new Bitmap(fullSizeImage))
                {
                    nImage = new Bitmap(width, height);

                    using (Graphics processImage = Graphics.FromImage(nImage))
                    {

                        processImage.DrawImage(originalImage, 0, 0, width, height);
                    }
                }
            }

            if (OverWrite(imagePath, nImage))
                return true;

            return false;
        }

        /// <summary>
        /// Use this method when need to re-size high quality image to a smaller high quality image, it overwrites existing image if it exists if dosent it creates
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static bool Resize(string imageOriginal, string imageNew, int height, int width)
        {
            Bitmap nImage = null;

            using (Image fullSizeImage = Image.FromFile(imageOriginal))
            {
                using (Bitmap originalImage = new Bitmap(fullSizeImage))
                {
                    nImage = new Bitmap(width, height);

                    using (Graphics processImage = Graphics.FromImage(nImage))
                    {

                        processImage.DrawImage(originalImage, 0, 0, width, height);
                    }
                }
            }

            if (OverWrite(imageNew, nImage))
                return true;

            return false;
        }

        /// <summary>
        /// Crops the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public static bool Crop(string filePath, int x, int y, int width, int height)
        {
            bool methodSuccesful = false;

            using (System.Drawing.Image image = System.Drawing.Image.FromFile(filePath))
            {
                using (Bitmap bitmap = new Bitmap(width, height, image.PixelFormat))
                {
                    bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        graphics.DrawImage
                        (
                            image,

                            new Rectangle(0, 0, width, height),

                            new Rectangle(x, y, width, height),

                            GraphicsUnit.Pixel
                        );

                        image.Dispose();

                        bitmap.Save(filePath);

                        if (OverWrite(filePath, bitmap))
                            methodSuccesful = true;
                    }
                }
            }

            return methodSuccesful;
        }

        /// <summary>
        /// Rounds the corner of the image default cornering of the image precentage is 50%
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="roundPrecentage"></param>
        public static bool Round(string imagePath, int roundPrecentage, Color backgroundColour)
        {
            Bitmap bitmapToSave = null;

            using (Image image = Image.FromFile(imagePath))
            {
                Bitmap bitmap = new Bitmap(image.Width, image.Height);

                Graphics graphics = Graphics.FromImage(bitmap);

                graphics.Clear(backgroundColour);

                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Brush brush = new System.Drawing.TextureBrush(image);

                Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);

                GraphicsPath graphicsPath = new GraphicsPath();
                graphicsPath.AddArc(rectangle.X, rectangle.Y, roundPrecentage, roundPrecentage, 180, 90);
                graphicsPath.AddArc(rectangle.X + rectangle.Width - roundPrecentage, rectangle.Y, roundPrecentage, roundPrecentage, 270, 90);
                graphicsPath.AddArc(rectangle.X + rectangle.Width - roundPrecentage, rectangle.Y + rectangle.Height - roundPrecentage, roundPrecentage, roundPrecentage, 0, 90);
                graphicsPath.AddArc(rectangle.X, rectangle.Y + rectangle.Height - roundPrecentage, roundPrecentage, roundPrecentage, 90, 90);
                graphics.FillPath(brush, graphicsPath);
                graphics.Dispose();
                bitmapToSave = (Bitmap)bitmap;
                //bitmap.Dispose();
                brush.Dispose();
                graphicsPath.Dispose();

            }

            if (OverWrite(imagePath, bitmapToSave))
                return true;

            return false;
        }

        /// <summary>
        /// Images the compression.
        /// </summary>
        /// <param name="imagePath">The image path.</param>
        /// <param name="imageQuality">The image quality.</param>
        /// <returns></returns>
        public static bool ImageCompression(string imagePath, long imageQuality)
        {
            bool isOverWriten = false;

            try
            {

                Bitmap bitmapQuality = null;
                EncoderParameters encoderParameters;
                ImageCodecInfo imageCodeInfo;

                using (Image image = Image.FromFile(imagePath))
                {

                    bitmapQuality = new Bitmap(image);
                    imageCodeInfo = GetEncoder(GetImageFormatFromFileNameExtension(imagePath));
                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    encoderParameters = new EncoderParameters(1);
                    EncoderParameter encoderParameter = new EncoderParameter(encoder, imageQuality);
                    encoderParameters.Param[0] = encoderParameter;
                }

                isOverWriten = OverWrite(imagePath, bitmapQuality, imageCodeInfo, encoderParameters);
            }
            catch (Exception)
            {

            }

            return isOverWriten;
        }

        /// <summary>
        /// Gets the encoder.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns></returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {

                    return codec;
                }
            }

            return null;
        }

        /// <summary>
        /// Overwrites image that is currently placed
        /// </summary>
        /// <param name="imagePath">provide the physical full path of the image on the hard drive</param>
        /// <param name="bitmap">image resource that isloaded in to the memory</param>
        /// <returns></returns>
        private static bool OverWrite(string imagePath, Bitmap bitmap)
        {
            bool isOverWriten = false;

            FileInfo imageFileInformation = new FileInfo(imagePath);

            if (imageFileInformation.Exists)
            {
                imageFileInformation.Delete();
            }

            if (bitmap != null)
            {
                bitmap.Save(imagePath, GetImageFormatFromFileNameExtension(imagePath));
                isOverWriten = true;
                bitmap.Dispose();
            }

            return isOverWriten;
        }

        /// <summary>
        /// Overwrites image and also changes images quality
        /// </summary>
        /// <param name="imagePath">provide the physical full path of the image on the hard drive</param>
        /// <param name="bitmap">image resource that isloaded in to the memory</param>
        /// <returns></returns>
        public static bool OverWrite(string imagePath, Bitmap bitmap, ImageCodecInfo imageCodeInfo, EncoderParameters encoderParameters)
        {
            bool isOverWriten = false;

            try
            {

                FileInfo imageFileInformation = new FileInfo(imagePath);

                if (imageFileInformation.Exists)
                {
                    imageFileInformation.Delete();
                }

                if (bitmap != null)
                {
                    bitmap.Save(imagePath, imageCodeInfo, encoderParameters);
                    isOverWriten = true;
                }
            }
            catch (Exception)
            {
             
            }
            finally
            {
                bitmap.Dispose();
            }

            return isOverWriten;
        }


        /// <summary>
        /// Gets from file to bytes.
        /// </summary>
        /// <param name="ImagePath">The image path.</param>
        /// <returns></returns>
        public static byte[] GetFromFileToBytes(string ImagePath)
        {
            byte[] memoryStream = null;

            using (FileStream fileToLoad = File.OpenRead(ImagePath))
            {
                int length = Convert.ToInt32(fileToLoad.Length);
                memoryStream = new byte[length];
                fileToLoad.Read(memoryStream, 0, length);
            }
            return memoryStream;
        }

        /// <summary>
        /// Thumbnails the callback.
        /// </summary>
        /// <returns></returns>
        private bool ThumbnailCallback()
        {
            return true;
        }

        /// <summary>
        /// Gets the image format from file name extension.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromFileNameExtension(string fileName)
        {
            ImageFormat imageFormat = null;

            try
            {
                //get the file and extension of the file
                FileInfo fileInfo = new FileInfo(fileName);
                string fileExtension = fileInfo.Extension.Replace(FileExtensionDelimiter, string.Empty).ToLower();

                //create a dictionary with different file extensions and image formats
                Dictionary<string, object> imageFormatTypes = new Dictionary<string, object>();
                imageFormatTypes.Add(emf, ImageFormat.Emf);
                imageFormatTypes.Add(png, ImageFormat.Png);
                imageFormatTypes.Add(bmp, ImageFormat.Bmp);
                imageFormatTypes.Add(exif, ImageFormat.Exif);
                imageFormatTypes.Add(icon, ImageFormat.Icon);
                imageFormatTypes.Add(jpg, ImageFormat.Jpeg);
                imageFormatTypes.Add(jpeg, ImageFormat.Jpeg);
                imageFormatTypes.Add(tiff, ImageFormat.Tiff);
                imageFormatTypes.Add(wmf, ImageFormat.Wmf);
                imageFormat = (ImageFormat)imageFormatTypes[fileExtension];
            }
            catch (DirectoryNotFoundException)
            {
                //if key is not found then use jpeg as default compression
                imageFormat = ImageFormat.Jpeg;

            }

            return imageFormat;
        }

    }
}
