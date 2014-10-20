using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Imaging;

namespace ImageResizer.Plugins.RedEye {
    public class GridSearch {

        public unsafe Rectangle FindBrightestSquare(UnmanagedImage image, int cx, int cy, int gridWidth = 32, int gridHeight = 16) {
            //It is important that the grid have either a 1x2 or 2x1 aspect ratio

            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;
            if (pixelSize > 4) throw new Exception("Invalid pixel depth");


            //Default factor
            int factor = 2;

            int overxbounds = Math.Max(0, Math.Max(cx - (gridWidth * factor / 2), (cx + gridWidth * factor / 2) - image.Width - 1));
            int overybounds = Math.Max(0, Math.Max(cy - (gridHeight * factor / 2), (cy + gridHeight * factor / 2) - image.Height - 1));

            //TODO - shrink factor, followed by gridWidth until overxbounds and overybounds <= 0.

            //Establish grid bounds
            int top = cy - (gridHeight * factor / 2);
            int bottom = cy + gridHeight * factor / 2;
            int left = cx - (gridWidth * factor / 2);
            int right = (cx + gridWidth * factor / 2);
            //TODO: shouldn't the grid be offset to the left or right instead of x centered?


            long scan0 = (long)image.ImageData;
            long stride = image.Stride;

            // do the job
            byte* src;

            //Build grid 1 using RMS
            byte[] grid0 = new byte[gridWidth * gridHeight];
            double v;
            double rms;
            double distance;

            double xCenter = gridWidth / 2;
            double yCenter = gridHeight / 2;
            double xScale = gridHeight / gridWidth; xScale *= xScale;
            double maxDistance = Math.Sqrt(xCenter * xCenter * xScale + yCenter * yCenter);


            //Loop through the grid
            for (int y = 0; y < gridHeight; y++) {
                for (int x = 0; x < gridWidth; x++) {
                    rms = 0;
                    //Loop through the pixels
                    for (int p = 0; p < factor * factor; p++) {
                        //Create a distance factor. 0 is where the user clicked, 1 is as far away as possible
                        distance = Math.Sqrt((y - yCenter) * (y - yCenter) + (x - xCenter) * (x - xCenter) * xScale) / maxDistance;

                        src = (byte*)(scan0 + (top + (y * factor) + p / factor) * stride + (left + x * factor + p % factor) * pixelSize);
                        if (src[RGB.R] == 0) continue;
                        v = (src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) / (double)src[RGB.R] * 255;
                        v = v > 0 ? (1 - distance) * v : 0;
                        rms += v * v;

                    }
                    grid0[y * gridWidth + x] = (byte)Math.Sqrt(rms / (factor * factor));
                }
            }

            List<Grid> grids = GenerateGrids(grid0, gridWidth, gridHeight, factor, left, top);

            double maxRatio = -1;
            Grid max = grids[0];
            for (int i = 0; i < grids.Count; i++) {
                grids[i].FindTop2Values();
                if (grids[i].top2ratio > maxRatio) {
                    max = grids[i];
                    maxRatio = max.top2ratio;
                }
            }
            return new Rectangle(max.pixelX + (max.maxValueIndex % max.width * max.factor), max.pixelY + (max.maxValueIndex / max.width * max.factor), max.factor, max.factor);
        }

        private List<Grid> GenerateGrids(byte[] grid0, int gridWidth, int gridHeight, int factor, int pixelX, int pixelY) {
            List<Grid> grids = new List<Grid>();
            Grid g0 = new Grid();
            g0.width = gridWidth;
            g0.height = gridHeight;
            g0.factor = factor;
            g0.pixelX = pixelX;
            g0.pixelY = pixelY;
            g0.data = grid0;
            grids.Add(g0);

            //Create 4 versions of scaled down grid
            for (int i = 0; i < 4; i++) {
                Grid g = new Grid(g0.width / 2, g0.height / 2);
                g.factor = g0.factor * 2;
                int ox = i % 2;
                int oy = i / 2;
                int scan = g0.width;
                g.pixelX = g0.pixelX + ox * g0.factor;
                g.pixelY = g0.pixelY + oy * g0.factor;
                grids.Add(g);
                byte v;
                int rms;
                int edges = 0;
                int src;
                for (int y = 0; y < g.height; y++) {
                    for (int x = 0; x < g.width; x++) {
                        src = (y * 2 + oy) * scan + x * 2 + ox;
                        v = grid0[src];
                        rms = v * v;
                        edges = 0;
                        if (ox > 0 && x >= g.width - 1) edges ^= 1;
                        if (oy > 0 && y >= g.height - 1) edges ^= 2;
                        if (edges == 0 || edges == 2) {
                            v = grid0[src + 1];
                            rms += v * v;
                        }
                        if (edges == 0) {
                            v = grid0[src + scan + 1];
                            rms += v * v;
                        }
                        if (edges == 0 || edges == 1) {
                            v = grid0[src + scan];
                            rms += v * v;
                        }
                        g.data[y * g.width + x] = (byte)Math.Sqrt(rms / (edges == 3 ? 1 : (edges == 0) ? 4 : 2));
                    }
                }
                if (g.width > 1 && g.height > 1) {
                    //Recursively make more grids until we hit 1 wide or tall
                    grids.AddRange(GenerateGrids(g.data, g.width, g.height, g.factor, g.pixelX, g.pixelY));
                }
            }
            return grids;
        }

        private class Grid {
            public Grid() { }
            public Grid(int width, int height) {
                this.width = width;
                this.height = height;
                data = new byte[width * height];
            }
            public int width;
            public int height;
            public byte[] data;
            public int pixelX;
            public int pixelY;
            public int factor;
            public int maxValueIndex;
            public int maxValue2Index;
            public double top2ratio;
            public void FindTop2Values() {
                KeyValuePair<int, int> results = FindTop2Values(this);
                maxValueIndex = results.Key;
                maxValue2Index = results.Value;
                top2ratio = (double)data[maxValueIndex] - (double)data[maxValue2Index];
            }
            private KeyValuePair<int, int> FindTop2Values(Grid g) {
                int length = g.data.Length;
                byte[] d = g.data;
                byte max = 0;
                int ixMax = 0;
                byte max2 = 0;
                int ixMax2 = 1;

                byte v;
                for (int i = 0; i < length; i++) {
                    v = d[i];
                    if (v > max2) {
                        if (v > max) {
                            ixMax2 = ixMax;
                            max2 = max;
                            max = v;
                            ixMax = i;
                        } else {
                            ixMax2 = i;
                            max2 = v;
                        }
                    }
                }
                return new KeyValuePair<int, int>(ixMax, ixMax2);
            }

        }
    }
}
