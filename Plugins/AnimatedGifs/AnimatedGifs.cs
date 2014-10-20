/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
namespace ImageResizer.Plugins.AnimatedGifs
{
    /// <summary>
    /// Adds support for resizing animated gifs. Once added, animated gifs will be resized while maintaining all animated frames. By default, .NET only saves the first frame of the GIF image.
    /// </summary>
    public class AnimatedGifs : BuilderExtension, IPlugin
    {
        Config c;
        /// <summary>
        /// Creates a new instance of the AnimatedGifs plugin.
        /// </summary>
        public AnimatedGifs(){}
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c; 
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

        /// <summary>
        /// We cannot fix buildToBitmap, only buildToStream
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="job"></param>
        protected override RequestedAction BuildJobBitmapToStream(ImageJob job, Bitmap source, Stream dest)
        {
            //Determines output format, includes code for saving in a variety of formats.
            if (ImageFormat.Gif.Equals(source.RawFormat) && //If it's a GIF
                job.Settings["frame"] == null &&    //With no frame specifier
                source.FrameDimensionsList != null && source.FrameDimensionsList.Length > 0) { //With multiple frames
                IEncoder enc = c.Plugins.EncoderProvider.GetEncoder(job.Settings, source);
                
                if (enc.MimeType.Equals("image/gif", StringComparison.OrdinalIgnoreCase) && source.GetFrameCount(FrameDimension.Time) > 1) {
                    WriteAnimatedGif(source, dest, enc, job.Settings);
                    return RequestedAction.Cancel;
                }

            }
            return RequestedAction.None;
        }
        
        private  void WriteAnimatedGif(Bitmap src, Stream output, IEncoder ios, ResizeSettings queryString)
        {
            //http://www.fileformat.info/format/gif/egff.htm
            //http://www.fileformat.info/format/gif/spec/44ed77668592476fb7a682c714a68bac/view.htm

            //Heavily modified and patched from comments on http://bloggingabout.net/blogs/rick/archive/2005/05/10/3830.aspx
            MemoryStream memoryStream = null;
            BinaryWriter writer = null;
            //Variable declaration
            try
            {
                writer = new BinaryWriter(output);
                memoryStream = new MemoryStream(4096);
                //Write the GIF 89a sig
                writer.Write(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' });
                
                //We parse this from the source image
                int loops = GetLoops(src.PropertyItems);
                int[] delays = GetDelays(src.PropertyItems);//20736 (delays) 20737 (loop);
                
                int frames = src.GetFrameCount(FrameDimension.Time);
                for (int frame = 0; frame < frames; frame++)
                {
                    //Select the frame
                    src.SelectActiveFrame(FrameDimension.Time, frame);

      
                    // http://radio.weblogs.com/0122832/2005/10/20.html
                    //src.MakeTransparent(); This call makes some GIFs replicate the first image on all frames.. i.e. SelectActiveFrame doesn't work.
                    
                    using (Bitmap b = c.CurrentImageBuilder.Build(src,queryString,false)){
                        //Useful to check if animation is occuring - sometimes the problem isn't the output file, but the input frames are 
                        //all the same.
                        //for (var i = 0; i < b.Height; i++) b.SetPixel(frame * 10, i, Color.Green);
                        // b.Save(memoryStream, ImageFormat.Gif);
                        ios.Write(b, memoryStream); //Allows quantization and dithering
                    }
                    
                    GifClass gif = new GifClass();
                    gif.LoadGifPicture(memoryStream);
                    if (frame == 0)
                    {
                        //Only one screen descriptor per file. Steal from the first image
                        writer.Write(gif.m_ScreenDescriptor.ToArray());
                        //How many times to loop the image (unless it is 1) IE and FF3 loop endlessley if loop=1
                        if (loops != 1) 
                            writer.Write(GifCreator.CreateLoopBlock(loops));
                    }
                    //Restore frame delay
                    int delay = 0;
                    if (delays != null && delays.Length > frame) delay = delays[frame];
                    writer.Write(GifCreator.CreateGraphicControlExtensionBlock(delay)); //The delay/transparent color block
                    writer.Write(gif.m_ImageDescriptor.ToArray()); //The image desc
                    writer.Write(gif.m_ColorTable.ToArray()); //The palette
                    writer.Write(gif.m_ImageData.ToArray()); //Image data

                    memoryStream.SetLength(0); //Clear the mem stream, but leave it allocated for now
                    memoryStream.Seek(0, SeekOrigin.Begin); //Reset memory buffer
                }
                
                writer.Write((byte)0x3B); //End file
            }
            finally
            {
               if (memoryStream != null) memoryStream.Dispose();
               //if (writer != null) writer.Close
            }
        }
        /// <summary>
        /// Returns the first PropertyItem with a matching ID
        /// </summary>
        /// <param name="items"></param>
        /// <param name="id"></param>
        /// <returns></returns>
          protected static PropertyItem getById(PropertyItem[] items, int id)
          {
              for (int i = 0; i < items.Length; i++)
               if (items[i] != null && items[i].Id == id)
                   return items[i];

              return null;
          }
          protected static int[] GetDelays(PropertyItem[] items)
          {
              //Property item ID 20736 http://bytes.com/groups/net-vb/692099-problem-animated-gifs
              PropertyItem pi = getById(items, 20736);
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
              PropertyItem pi = getById(items, 20737);
              if (pi == null) return 0;
              //Combine bytes into integers
              return pi.Value[0] + (pi.Value[1] << 8);
          }


          
    }
    
}
