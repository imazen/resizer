using System;

using System.Collections.Generic;

using System.IO;

using System.Text;

namespace GifCreator
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

        public static byte[] CreateGraphicControlExtensionBlock(int delay)
        {
            /*320:   21 F9 04                Graphic Control Extension frame #1
323:   08                       - no transparency
324:   09 00                    - 0.09 sec duration
325:   00                       - no transparent color
327:   00                       - end
*/

            byte[] result = new byte[8];

            // Split the delay into high- and lowbyte

            byte d1 = (byte)(delay % 256);

            byte d2 = (byte)(delay / 256);

            result[0] = (byte)0x21; // Start ExtensionBlock

            result[1] = (byte)0xF9; // GraphicControlExtension

            result[2] = (byte)0x04; // Size of DataBlock (4)

            result[3] = 9;// 8 is best for animation to display 25;// 8 + 16 adds dispose (cleanup for animation) 9;// 1;//(byte)0x00; //1 enables transparency

            result[4] = d1; //Duration

            result[5] = d2;

            result[6] = (byte)0x00; //Transparent color - only if transparency bit set.

            result[7] = (byte)0x00; //end

            return result;

        }

        public static byte[] CreateLoopBlock()

        { return CreateLoopBlock(0); }

        public static byte[] CreateLoopBlock(int numberOfRepeatings)
        {
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