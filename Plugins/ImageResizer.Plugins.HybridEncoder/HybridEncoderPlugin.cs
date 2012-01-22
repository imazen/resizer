using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Encoding;
using ImageResizer.Resizing;
using System.Drawing;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImageResizer.Util;

namespace ImageResizer.Plugins.HybridEncoder {
    public class HybridEncoderPlugin:BuilderExtension,IPlugin {

        protected override RequestedAction PostFlushChanges(ImageState s) {
            if (!Utils.getBool(s.settings,"hybrid",false)) return RequestedAction.None;


            Color background = s.settings.BackgroundColor;
            if (background == Color.Transparent && !s.supportsTransparency) 
                background = Color.White;

            Separate(s.destBitmap, background, Utils.getInt(s.settings, "lt", 20), Utils.getInt(s.settings, "ut", 200), s.supportsTransparency);
            return RequestedAction.None;
        }


        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


        private int ToBgra(Color c) {
            return ((((((int)c.B << 8) & c.G) << 8) & c.R) << 8) & c.A;
        }


        public void Separate(Bitmap b, Color bgcolor, int lowerThreshold, int upperThreshold, bool keepOutsideThresholds) {
            using (Bitmap energy = Grayscale.CommonAlgorithms.Y.Apply(b)) {
                
              /*  BradleyLocalThresholding blr = new BradleyLocalThresholding();
                blr.WindowSize = 7;
                blr.PixelBrightnessDifferenceLimit = 0.001f;
                blr.ApplyInPlace(energy);*/

                SobelEdgeDetector sed = new SobelEdgeDetector();
                //sed.ScaleIntensity
                sed.ApplyInPlace(energy);



                int height = b.Height;
                int width = b.Width;

                
                
                int replacementColor = bgcolor == Color.Transparent ? 0 : bgcolor.ToArgb(); //TODO: may need to reverse byte order

                UseBitmapData(energy, delegate(BitmapData e) {
                    UseBitmapData(b, delegate(BitmapData img) {

                        long imgStride = img.Stride;
                        long eStride = e.Stride;
                        long eRow = (long)e.Scan0;
                        long imgRow = (long)img.Scan0;
                        long ePixel;
                        long imgPixel;
                        for (int row = 0; row < height; row++) { //rows
                            //Start the pixel pointers at the start of the rows
                            ePixel = eRow;
                            imgPixel = imgRow;
                            for (int col = 0; col < width; col++) { //col

                                if (!KeepBlock((long)e.Scan0, eStride, width, height, col, row, delegate(byte v) {
                                    return (v >= lowerThreshold && v <= upperThreshold) == keepOutsideThresholds;
                                    //return (v == 0) == keepOutsideThresholds;
                                })) {
                                    Marshal.WriteInt32((IntPtr)imgPixel, replacementColor);
                                }
                                
                                imgPixel += 4;
                            }

                            //Update the row pointer
                            imgRow += imgStride;
                            eRow += eStride;
                        }
                    });
                });
            }
        }
        private delegate bool KeepBlockMethod(byte value);

        private bool KeepBlock(long scan0, long stride, int width, int height, int x, int y, KeepBlockMethod method) {
            int xoffset = (x / 8) * 8;
            int yoffset = (y / 8) * 8;
            int boxwidth = Math.Min(width - xoffset, 8);
            int boxheight = Math.Min(height - yoffset, 8);

            byte[] buffer = new byte[8];
            for (int j = 0; j < boxheight; j++) {
                //TODO: Cache last 8 * (width / 8 + 1) arrays or switch everything to arrays.
                Marshal.Copy((IntPtr)((j + yoffset) * stride + scan0 + xoffset), buffer, 0, boxwidth);
                for (int i = 0; i < boxwidth; i++) {
                    if (method(buffer[i])) return true;
                }
            }
            return false;
        }



        private delegate void BitmapDataMethod(BitmapData bd);

        private void UseBitmapData(Bitmap b, BitmapDataMethod method){
            BitmapData bd = null;
            try{
                bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);
                method(bd);
            }finally{
                b.UnlockBits(bd);
            }
        }



    }
}
