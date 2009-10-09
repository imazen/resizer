using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using System.Drawing;
using GifCreator;

namespace fbs.ImageResizer
{
    public class AnimatedImageManager
    {
          /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// This is the only method that supports animated GIF output. GDI has no native support for animated GIFs, so we have to take a different code path.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public static void BuildImage(string sourceFile, string targetFile, NameValueCollection queryString)
        {
            bool useICM = true;
            if ("true".Equals(queryString["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;


            //Load image
            System.Drawing.Bitmap b = null;
            try
            {
                b = new System.Drawing.Bitmap(sourceFile,useICM);
            }
            catch (ArgumentException ae)
            {
                ae.Data.Add("path", sourceFile);
                ae.Data.Add("possiblereason",
                    "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels.");
                throw ae;
            }
            if (b == null) throw new IOException("Could not read the specified image! Image invalid or something.");
            


            //Resize image 
            using (b)
            {
                //Determines output format, includes code for saving in a variety of formats.
                ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromPhysicalPath(sourceFile),queryString);

                if (ios.OutputFormat == ImageFormat.Gif && queryString["frame"] == null && b.FrameDimensionsList != null && b.FrameDimensionsList.Length > 0)
                {
                    try
                    {
                        if (b.GetFrameCount(FrameDimension.Time) > 1)
                        {
                            WriteAnimatedGif(b, targetFile, ios, queryString);
                            return;
                        }
                    }
                    catch (System.Runtime.InteropServices.ExternalException)
                    {
                    }

                }

                    
                //Open stream and save format.
                System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                using (fs)
                {
                    ios.SaveImage(fs,ImageManager.BuildImage(b,ios.OriginalFormat,queryString));
                }
                
            }
        }

        private static void WriteAnimatedGif(Bitmap src, string targetFile, ImageOutputSettings ios, NameValueCollection queryString)
        {
            System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
            using (fs)
            {
               WriteAnimatedGif(src,fs,ios,queryString);
            }
        }


          private static void WriteAnimatedGif(Bitmap src, FileStream output, ImageOutputSettings ios, NameValueCollection queryString)
        {
            //http://www.fileformat.info/format/gif/egff.htm
            //http://www.fileformat.info/format/gif/spec/44ed77668592476fb7a682c714a68bac/view.htm


            //HEavily modified and patched from comments on http://bloggingabout.net/blogs/rick/archive/2005/05/10/3830.aspx

            MemoryStream memoryStream = null;
            BinaryWriter writer = null;
            //Variable declaration
            try
            {
                writer = new BinaryWriter(output);
                memoryStream = new MemoryStream(4096);

                byte[] gif_Signature = new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };

                writer.Write(gif_Signature);

                int loops = 0;
                int[] delays = null;
                
                int frames = src.GetFrameCount(FrameDimension.Time);
                for (int frame = 0; frame < frames; frame++)
                {
                    src.SelectActiveFrame(FrameDimension.Time, frame);

                    if (frame == 0)
                    {
                        delays = GetDelays(src.PropertyItems);
                        loops = GetLoops(src.PropertyItems);
                    }
                    //20736 (delays) 20737 (loop)
                    
                    //Only works for a selected few images. http://radio.weblogs.com/0122832/2005/10/20.html
                    //src.MakeTransparent(); This call makes some GIFs replicate the first image on all frames.. i.e. SelectActiveFrame doesn't work.

                    using (Bitmap b = ImageManager.BuildImage(src, -1, -1, new ResizeSettings(queryString), new ImageSettings(queryString), new ImageFilter(queryString), ios))
                    {
                        //Useful to check 
                        //for (var i = 0; i < b.Height; i++) b.SetPixel(frame * 10, i, Color.Green);
                        // b.Save(memoryStream, ImageFormat.Gif);
                        ios.SaveImage(memoryStream, b); //Allows quantization and dithering
                    }

                    

                    GifClass gif = new GifClass();

                    gif.LoadGifPicture(memoryStream);

                    if (frame == 0)
                    {
                        writer.Write(gif.m_ScreenDescriptor.ToArray());
                        writer.Write(GifCreator.GifCreator.CreateLoopBlock(loops)); //Changed to fit wikipedia structure 

                    }

                    //Restore frame delay
                    int delay = 0;
                    if (delays != null && delays.Length > frame) delay = delays[frame];

                    writer.Write(GifCreator.GifCreator.CreateGraphicControlExtensionBlock(delay));

                    writer.Write(gif.m_ImageDescriptor.ToArray());

                    writer.Write(gif.m_ColorTable.ToArray());

                    writer.Write(gif.m_ImageData.ToArray());



                    memoryStream.SetLength(0); //Clear the mem stream, but leave it allocated for now
                    memoryStream.Seek(0, SeekOrigin.Begin); //Reset memory buffer
                }
                
                writer.Write((byte)0x3B); //End file

            }
            finally
            {
               if (memoryStream != null) memoryStream.Dispose();
               if (writer != null) writer.Close();
            }
        }

          protected static int[] GetDelays(PropertyItem[] items)
          {
              //Property item ID 20736 http://bytes.com/groups/net-vb/692099-problem-animated-gifs
              PropertyItem pi = null;
              for (int i =0; i < items.Length;i++){
                  if (items[i] != null && items[i].Id == 20736)
                  {
                      pi = items[i]; break;
                  }
              }
              if (pi == null) return null;

              //Combine bytes into integers
              int[] vals = new int[pi.Value.Length / 4];
              for (int i = 0; i < pi.Value.Length; i+=4){
                  vals[i / 4] = BitConverter.ToInt32(pi.Value, i);
              }
              return vals;
              
          }
          protected static int GetLoops(PropertyItem[] items)
          {

            // http://weblogs.asp.net/justin_rogers/archive/2004/01/19/60424.aspx
              //Property item ID 20737
              PropertyItem pi = null;
              for (int i = 0; i < items.Length; i++)
              {
                  if (items[i] != null && items[i].Id == 20737)
                  {
                      pi = items[i]; break;
                  }
              }
              if (pi == null) return 0;

              //Combine bytes into integers
              return pi.Value[0] + (pi.Value[1] << 8);
          }



    }
    
}
