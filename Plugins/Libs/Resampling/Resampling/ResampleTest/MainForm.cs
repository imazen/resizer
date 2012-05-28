// Copyright © 2007  by Libor Tinka
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ResampleTest {

  /// <summary>
  /// Main form showing input image and resampled versions.
  /// </summary>
  public class MainForm : Form {

    #region Private Constants

    private const string FILE_INPUT = "input.png";
    private const string FILE_CHECKERBOARD = "checkerboard.png";
    private const int SPACING = 8;

    #endregion

    #region Constructor

    public MainForm() {

      //
      // load and show original image
      //
      Image imgInput = Image.FromFile(FILE_INPUT);
      Image imgCheckerboard = Image.FromFile(FILE_CHECKERBOARD);

      PictureBox pb1 = new PictureBox();
      pb1.BackgroundImage = imgCheckerboard;
      pb1.Image = imgInput;
      pb1.Location = new Point(0, 0);
      pb1.Parent = this;
      pb1.Size = imgInput.Size;
      
      //
      // new image size
      //
      Size nSize = new Size(imgInput.Width * 4, imgInput.Height * 4);

      //
      // resample using GDI+
      //
      Image imgGdi = new Bitmap(nSize.Width, nSize.Height);

      Graphics grfx = Graphics.FromImage(imgGdi);

      grfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

      // necessary setting for proper work with image borders
      grfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

      grfx.DrawImage(imgInput,
        new Rectangle(new Point(0, 0), nSize), new Rectangle(new Point(0, 0), imgInput.Size),
        GraphicsUnit.Pixel);

      grfx.Dispose();

      PictureBox pb2 = new PictureBox();
      pb2.BackgroundImage = imgCheckerboard;
      pb2.Image = imgGdi;
      pb2.Location = new Point(imgInput.Width + SPACING, 0);
      pb2.Parent = this;
      pb2.Size = imgGdi.Size;

      //
      // resample using ResamplingService
      //
      ResamplingService resamplingService = new ResamplingService();

      resamplingService.Filter = ResamplingFilters.CubicBSpline;

      ushort[][,] input = ConvertBitmapToArray((Bitmap)imgInput);

      ushort[][,] output = resamplingService.Resample(input, nSize.Width, nSize.Height);

      Image imgCustom = (Image)ConvertArrayToBitmap(output);

      PictureBox pb3 = new PictureBox();
      pb3.BackgroundImage = imgCheckerboard;
      pb3.Image = imgCustom;
      pb3.Location = new Point(imgInput.Width + SPACING + imgGdi.Width + SPACING, 0);
      pb3.Parent = this;
      pb3.Size = nSize;

      //
      // set main form properties
      //
      this.ClientSize = new Size(
        pb1.Width + SPACING + pb2.Width + SPACING + pb3.Width,
        Math.Max(pb1.Height, pb2.Height));

      this.Text = "Resampling Test";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts Bitmap to array. Supports only Format32bppArgb pixel format.
    /// </summary>
    /// <param name="bmp">Bitmap to convert.</param>
    /// <returns>Output array.</returns>
    private ushort[][,] ConvertBitmapToArray(Bitmap bmp) {

      ushort[][,] array = new ushort[4][,];

      for (int i = 0; i < 4; i++)
        array[i] = new ushort[bmp.Width, bmp.Height];

      BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      int nOffset = (bd.Stride - bd.Width * 4);

      unsafe {

        byte* p = (byte*)bd.Scan0;

        for (int y = 0; y < bd.Height; y++) {
          for (int x = 0; x < bd.Width; x++) {

            array[3][x, y] = (ushort)p[3];
            array[2][x, y] = (ushort)p[2];
            array[1][x, y] = (ushort)p[1];
            array[0][x, y] = (ushort)p[0];

            p += 4;
          }

          p += nOffset;
        }
      }

      bmp.UnlockBits(bd);

      return array;
    }

    /// <summary>
    /// Converts array to Bitmap. Supports only Format32bppArgb pixel format.
    /// </summary>
    /// <param name="array">Array to convert.</param>
    /// <returns>Output Bitmap.</returns>
    private Bitmap ConvertArrayToBitmap(ushort[][,] array) {

      int width = array[0].GetLength(0);
      int height = array[0].GetLength(1);

      Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

      BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
      int nOffset = (bd.Stride - bd.Width * 4);

      unsafe {

        byte* p = (byte*)bd.Scan0;

        for (int y = 0; y < height; y++) {
          for (int x = 0; x < width; x++) {

            p[3] = (byte)Math.Min(Math.Max(array[3][x, y], Byte.MinValue), Byte.MaxValue);
            p[2] = (byte)Math.Min(Math.Max(array[2][x, y], Byte.MinValue), Byte.MaxValue);
            p[1] = (byte)Math.Min(Math.Max(array[1][x, y], Byte.MinValue), Byte.MaxValue);
            p[0] = (byte)Math.Min(Math.Max(array[0][x, y], Byte.MinValue), Byte.MaxValue);

            p += 4;
          }

          p += nOffset;
        }
      }

      bmp.UnlockBits(bd);

      return bmp;
    }

    #endregion

    #region Main

    public static void Main() {

      Application.Run(new MainForm());
    }

    #endregion
  }
}