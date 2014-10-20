using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.SeamCarving {
    public class CarveDataPlotter {


        public int BlockCount { get; set; }
        public int Stride { get; set; }
        public int Rows { get; set; }

        public byte[][] Grid { get; set; }

        public void Init(string data) {
            Grid = new byte[Rows][];
            for (int i = 0; i < Rows; i++)
                Grid[i] = new byte[Stride];

            for (int i = 0; i < data.Length; i++) {
                byte v = (byte)(data[i] == '0' ? 0 : (data[i] == '1' ? 1 : (data[i] == '2' ? 2 : 3)));
                if (v == 3) throw new ArgumentOutOfRangeException("data", "Invalid character '" + data[i] + "' at index " + i);
                Grid[i / Stride][i % Stride] = v;
            }
        }


        public void SaveBitmapAs(string path, int width, int height) {
            int blockSize = (int)Math.Floor(Math.Sqrt(width * height / (double)BlockCount));
            using (Bitmap bit = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            using (Graphics g = Graphics.FromImage(bit))
            using (Brush keep = new SolidBrush(Color.FromArgb(255,Color.Green)))
            using (Brush remove = new SolidBrush(Color.FromArgb(255,Color.Red))) {
                g.Clear(Color.Black);
                byte[][] gr = Grid;
                int block = blockSize;
                for (int y = 0; y < Rows; y++) {
                    for (int x = 0; x < Stride; x++) {
                        Brush b = (gr[y][x] == 1) ? keep : ((gr[y][x] == 2) ? remove : null);
                        if (b != null){
                            if ( y < Rows -1 && x < Stride -1)
                                g.FillRectangle(b, new Rectangle(x * block, y * block, block, block));
                            else
                                g.FillRectangle(b, new Rectangle(x * block, y * block, block + Math.Min(0, width - (x + 1) * block), block + Math.Min(0, height - (y + 1) * block)));
                        }
                    }
                }

                g.Flush(System.Drawing.Drawing2D.FlushIntention.Flush);
                
                bit.Save(path, ImageFormat.Bmp);
            }
        }



        /// <summary>
        /// Returns the minimum width (if vertical seams are used) and minimum height (if horizontal seams are used) shrinkage needed to eliminate the marked removal areas.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        public Size GetRemovalSpace(int w, int h, int blockSize) {
            //In an ideal scenario, we just have to count how many remove blocks exist in a single vertical or horizontal line.
            int[] rows = new int[Rows];
            int[] cols = new int[Stride];

            byte[][] gr = Grid;

            for (int y = 0; y < Rows; y++) {
                for (int x = 0; x < Stride; x++) {
                    if ((gr[y][x] == 2)) {
                        if (y < Rows - 1 && x < Stride - 1) {
                            rows[y] += blockSize;
                            cols[x] += blockSize;
                        } else {
                            rows[y] += blockSize + Math.Min(0, w - (x + 1) * blockSize);
                            cols[x] += blockSize + Math.Min(0, h - (y + 1) * blockSize);
                        }
                    }
                }
            }

            int maxWidth = 0;
            int maxHeight = 0;

            foreach (int i in rows)
                if (i > maxWidth) maxWidth = i;

            foreach (int i in cols)
                if (i > maxHeight) maxHeight = i;

            return new Size(maxWidth, maxHeight);
        }
    }
}
