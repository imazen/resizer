/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace ImageResizer.Plugins.AnimatedGifs
{
    public enum GIFVersion
    {
        GIF87a,
        GIF89a
    }
    public enum GIFBlockType
    {
        ImageDescriptor = 0x2C,
        Extension = 0x21,
        Trailer = 0x3B
    }
    /// <summary>
    /// Provides methods for creating application and graphics blocks needed to write a animated GIF.
    /// </summary>
    public class GifCreator
    {
        public static void CreateAnimatedGif(List<string> gifFiles, int delay, string outputFile)
        {
            BinaryWriter writer = new BinaryWriter(new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite));
            byte[] gif_Signature = new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };
            writer.Write(gif_Signature);
            for (int i = 0; i < gifFiles.Count; i++)
            {
                GifClass gif = new GifClass();
                gif.LoadGifPicture(gifFiles[i]);
                if (i == 0)
                    writer.Write(gif.m_ScreenDescriptor.ToArray());
                writer.Write(GifCreator.CreateGraphicControlExtensionBlock(delay));
                writer.Write(gif.m_ImageDescriptor.ToArray());
                writer.Write(gif.m_ColorTable.ToArray());
                writer.Write(gif.m_ImageData.ToArray());
            }
            writer.Write(GifCreator.CreateLoopBlock());
            writer.Write((byte)0x3B); //End file
            writer.Close();
        }
        /// <summary>
        ///  Written before each frame - specifies the frame's delay and the index of the transparent color (0)
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static byte[] CreateGraphicControlExtensionBlock(int delay)
        {
            return CreateGraphicControlExtensionBlock(delay, 0);
        }
        /// <summary>
        /// Written before each frame - specifies the frame's delay and the index of the transparent color
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="transparentColorIndex"></param>
        /// <returns></returns>
        public static byte[] CreateGraphicControlExtensionBlock(int delay, byte transparentColorIndex)
        {
            return CreateGraphicControlExtensionBlock(delay, transparentColorIndex, true);
        }
        /// <summary>
        /// Written before each frame - specifies the frame's delay and the index of the transparent color
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="transparentColorIndex"></param>
        /// <param name="enableTransparency"></param>
        public static byte[] CreateGraphicControlExtensionBlock(int delay, byte transparentColorIndex, bool enableTransparency)
        {
            /*320:   21 F9 04                Graphic Control Extension frame #1
            323:   08                       - no transparency
            324:   09 00                    - 0.09 sec duration
            325:   00                       - no transparent color
            327:   00                       - end
            */
            byte[] result = new byte[8];
          
    
            result[0] = (byte)0x21; // Start ExtensionBlock
            result[1] = (byte)0xF9; // GraphicControlExtension
            result[2] = (byte)0x04; // Size of DataBlock (4)
            // 1 enables transparency
            // 8 clears to bgcolor after each frame.
            // 25 (8 + 16) = restore to previous data (doesn't work in FF)
            result[3] = (byte)(8 | (enableTransparency ? 1 : 0)); //Transparent, clear to bgcolor (works good for transparent animations

            // Split the delay into high- and lowbyte
            result[4] = (byte)(delay % 256); //Duration - lsb
            result[5] = (byte)(delay / 256); //msb
            result[6] = transparentColorIndex; //Transparent color - only if transparency bit set.
            result[7] = (byte)0x00; //end
            return result;
        }
        /// <summary>
        /// Creates a Loop Block (Netscape application extension) for infinite looping. Written after the last frame's image data
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateLoopBlock()
        { return CreateLoopBlock(0); }
        /// <summary>
        /// Creates a Loop block for the specified number of repeats. Written after the last frame's image data. 
        /// Do NOT call this if numberOfRepeatings=1. IE and FF &lt; 3 (incorrectly) loop infinitely when loops=1. Simply omit the extension in that case.
        /// </summary>
        /// <param name="numberOfRepeatings"></param>
        /// <returns></returns>
        public static byte[] CreateLoopBlock(int numberOfRepeatings)
        {
            //http://www.let.rug.nl/kleiweg/gif/netscape.html
            /*30D:   21 FF 0B                Application Extension
            310:   4E 45 54
                   53 43 41
                   50 45 32
                   2E 30        NETSCAPE2.0
            31B:   03 01                    - data follows
            31D:   FF FF                    - loop animation
            31F:   00                       - end
            */
            byte rep1 = (byte)(numberOfRepeatings % 256);
            byte rep2 = (byte)(numberOfRepeatings / 256);
            byte[] result = new byte[19];
            result[0] = (byte)0x21; // Start ExtensionBlock
            result[1] = (byte)0xFF; // ApplicationExtension
            result[2] = (byte)0x0B; // Size of DataBlock (11) for NETSCAPE2.0)
            result[3] = (byte)'N';
            result[4] = (byte)'E';
            result[5] = (byte)'T';
            result[6] = (byte)'S';
            result[7] = (byte)'C';
            result[8] = (byte)'A';
            result[9] = (byte)'P';
            result[10] = (byte)'E';
            result[11] = (byte)'2';
            result[12] = (byte)'.';
            result[13] = (byte)'0';
            result[14] = (byte)0x03; // Size of Loop Block
            result[15] = (byte)0x01; // Loop Indicator
            result[16] = (byte)rep1; // Number of repetitions
            result[17] = (byte)rep2; // 0 for endless loop
            result[18] = (byte)0x00;
            return result;
        }
    }
}