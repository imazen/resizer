/////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006, Frank Blumenberg
// 
// See License.txt for complete licensing and attribution information.
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// 
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using PhotoshopFile;

namespace PaintDotNet.Data.PhotoshopFileType
{
  class ImageDecoderPdn
  {
    public static BitmapLayer DecodeImage(PsdFile psdFile)
    {
      BitmapLayer layer = PaintDotNet.Layer.CreateBackgroundLayer(psdFile.Columns, psdFile.Rows);

      Surface surface = layer.Surface;
      surface.Clear((ColorBgra)0xffffffff);

      for (int y = 0; y < psdFile.Rows; y++)
      {
        unsafe
        {
          int rowIndex = y * psdFile.RowPixels;

          ColorBgra* dstRow = surface.GetRowAddress(y);
          ColorBgra* dstPixel = dstRow;

          for (int x = 0; x < psdFile.Columns; x++)
          {
            int pos = rowIndex + x;
            SetPDNColor(dstPixel, psdFile, pos);
            dstPixel++;
          }
        }
      }

      return layer;
    }

    /////////////////////////////////////////////////////////////////////////// 

    unsafe private static void SetPDNColor(ColorBgra* dstPixel, PsdFile psdFile, int pos)
    {
      switch (psdFile.ColorMode)
      {
        case PsdFile.ColorModes.RGB:
          dstPixel->R = psdFile.ImageData[0][pos];
          dstPixel->G = psdFile.ImageData[1][pos];
          dstPixel->B = psdFile.ImageData[2][pos];
          break;
        case PsdFile.ColorModes.CMYK:
          SetPDNColorCMYK(dstPixel,
              psdFile.ImageData[0][pos],
              psdFile.ImageData[1][pos],
              psdFile.ImageData[2][pos],
              psdFile.ImageData[3][pos]);
          break;
        case PsdFile.ColorModes.Multichannel:
          SetPDNColorCMYK(dstPixel,
              psdFile.ImageData[0][pos],
              psdFile.ImageData[1][pos],
              psdFile.ImageData[2][pos],
              0);
          break;
        case PsdFile.ColorModes.Bitmap:
          byte bwValue = ImageDecoder.GetBitmapValue(psdFile.ImageData[0], pos);
          dstPixel->R = bwValue;
          dstPixel->G = bwValue;
          dstPixel->B = bwValue;
          break;
        case PsdFile.ColorModes.Grayscale:
        case PsdFile.ColorModes.Duotone:
          dstPixel->R = psdFile.ImageData[0][pos];
          dstPixel->G = psdFile.ImageData[0][pos];
          dstPixel->B = psdFile.ImageData[0][pos];
          break;
        case PsdFile.ColorModes.Indexed:
          int index = (int)psdFile.ImageData[0][pos];
          dstPixel->R = (byte)psdFile.ColorModeData[index];
          dstPixel->G = psdFile.ColorModeData[index + 256];
          dstPixel->B = psdFile.ColorModeData[index + 2 * 256];
          break;
        case PsdFile.ColorModes.Lab:
          SetPDNColorLab(dstPixel,
            psdFile.ImageData[0][pos],
            psdFile.ImageData[1][pos],
            psdFile.ImageData[2][pos]);
          break;
      }
    }

    /////////////////////////////////////////////////////////////////////////// 

    public static BitmapLayer DecodeImage(PhotoshopFile.Layer psdLayer)
    {
      BitmapLayer pdnLayer = new BitmapLayer(psdLayer.PsdFile.Columns, psdLayer.PsdFile.Rows);

      Surface surface = pdnLayer.Surface;
      surface.Clear((ColorBgra)0);

      bool hasMaskChannel = psdLayer.SortedChannels.ContainsKey(-2);
      var channels = psdLayer.ChannelsArray;
      var alphaChannel = psdLayer.AlphaChannel;

      int yPsdLayerStart = Math.Max(0, -psdLayer.Rect.Y);
      int yPsdLayerEnd = Math.Min(psdLayer.Rect.Height, surface.Height - psdLayer.Rect.Y);

      for (int yPsdLayer = yPsdLayerStart; yPsdLayer < yPsdLayerEnd; yPsdLayer++)
      {
        unsafe
        {
          ColorBgra* dstRow = surface.GetRowAddress(yPsdLayer + psdLayer.Rect.Y);

          int xPsdLayerStart = Math.Max(0, -psdLayer.Rect.X);
          int xPsdLayerEnd = Math.Min(psdLayer.Rect.Width, psdLayer.PsdFile.Columns - psdLayer.Rect.Left);
          int xPsdLayerEndCopy = Math.Min(xPsdLayerEnd, surface.Width - psdLayer.Rect.X);

          int srcRowIndex = yPsdLayer * psdLayer.Rect.Width;
          int dstIndex = psdLayer.Rect.Left + xPsdLayerStart;
          ColorBgra* dstPixel = dstRow + dstIndex;

          for (int xPsdLayer = xPsdLayerStart; xPsdLayer < xPsdLayerEnd; xPsdLayer++)
          {
            if (xPsdLayer < xPsdLayerEndCopy)
            {
              int srcIndex = srcRowIndex + xPsdLayer;
              SetPDNColor(dstPixel, psdLayer, channels, alphaChannel, srcIndex);
              int maskAlpha = 255;
              if (hasMaskChannel)
              {
                maskAlpha = GetMaskAlpha(psdLayer.MaskData, xPsdLayer, yPsdLayer);
              }
              SetPDNAlpha(dstPixel, alphaChannel, srcIndex, maskAlpha);
            }

            dstPixel++;
          }
        }
      }

      return pdnLayer;
    }

    /////////////////////////////////////////////////////////////////////////// 
    unsafe private static void SetPDNColor(ColorBgra* dstPixel, PhotoshopFile.Layer layer,
        PhotoshopFile.Layer.Channel[] channels, PhotoshopFile.Layer.Channel alphaChannel, int pos)
    {
      switch (layer.PsdFile.ColorMode)
      {
        case PsdFile.ColorModes.RGB:
          dstPixel->R = channels[0].ImageData[pos];
          dstPixel->G = channels[1].ImageData[pos];
          dstPixel->B = channels[2].ImageData[pos];
          break;
        case PsdFile.ColorModes.CMYK:
          SetPDNColorCMYK(dstPixel,
            channels[0].ImageData[pos],
            channels[1].ImageData[pos],
            channels[2].ImageData[pos],
            channels[3].ImageData[pos]);
          break;
        case PsdFile.ColorModes.Multichannel:
          SetPDNColorCMYK(dstPixel,
            channels[0].ImageData[pos],
            channels[1].ImageData[pos],
            channels[2].ImageData[pos],
            0);
          break;
        case PsdFile.ColorModes.Bitmap:
          byte bwValue = ImageDecoder.GetBitmapValue(channels[0].ImageData, pos);
          dstPixel->R = bwValue;
          dstPixel->G = bwValue;
          dstPixel->B = bwValue;
          break;
        case PsdFile.ColorModes.Grayscale:
        case PsdFile.ColorModes.Duotone:
          dstPixel->R = channels[0].ImageData[pos];
          dstPixel->G = channels[0].ImageData[pos];
          dstPixel->B = channels[0].ImageData[pos];
          break;
        case PsdFile.ColorModes.Indexed:
          int index = (int)channels[0].ImageData[pos];
          dstPixel->R = (byte)layer.PsdFile.ColorModeData[index];
          dstPixel->G = layer.PsdFile.ColorModeData[index + 256];
          dstPixel->B = layer.PsdFile.ColorModeData[index + 2 * 256];
          break;
        case PsdFile.ColorModes.Lab:
          SetPDNColorLab(dstPixel,
            channels[0].ImageData[pos],
            channels[1].ImageData[pos],
            channels[2].ImageData[pos]);
          break;
      }
    }
    
    unsafe private static void SetPDNAlpha(ColorBgra* dstPixel,
      PhotoshopFile.Layer.Channel alphaChannel, int srcIndex, int maskAlpha)
    {
      byte alpha = 255;
      if (alphaChannel != null)
        alpha = alphaChannel.ImageData[srcIndex];
      if (maskAlpha < 255)
        alpha = (byte)(alpha * maskAlpha / 255);

      dstPixel->A = alpha;
    }

    /////////////////////////////////////////////////////////////////////////// 

    private static int GetMaskAlpha(PhotoshopFile.Layer.Mask mask, int x, int y)
    {
      int c = 255;
      if (mask.PositionIsRelative)
      {
        x -= mask.Rect.X;
        y -= mask.Rect.Y;
      }
      else
      {
        x = (x + mask.Layer.Rect.X) - mask.Rect.X;
        y = (y + mask.Layer.Rect.Y) - mask.Rect.Y;
      }

      if (y >= 0 && y < mask.Rect.Height &&
           x >= 0 && x < mask.Rect.Width)
      {
        int pos = y * mask.Rect.Width + x;
        if (pos < mask.ImageData.Length)
          c = mask.ImageData[pos];
        else
          c = 255;
      }

      return c;
    }

    /////////////////////////////////////////////////////////////////////////// 

    unsafe private static void SetPDNColorLab(ColorBgra* dstPixel, byte lb, byte ab, byte bb)
    {
      double exL, exA, exB;

      exL = (double)lb;
      exA = (double)ab;
      exB = (double)bb;

      double L_coef, a_coef, b_coef;
      L_coef = 2.55;
      a_coef = 1.00;
      b_coef = 1.00;

      int L = (int)(exL / L_coef);
      int a = (int)(exA / a_coef - 127.5);
      int b = (int)(exB / b_coef - 127.5);

      // For the conversion we first convert values to XYZ and then to RGB
      // Standards used Observer = 2, Illuminant = D65

      const double ref_X = 95.047;
      const double ref_Y = 100.000;
      const double ref_Z = 108.883;

      double var_Y = ((double)L + 16.0) / 116.0;
      double var_X = (double)a / 500.0 + var_Y;
      double var_Z = var_Y - (double)b / 200.0;

      double var_X3 = var_X * var_X * var_X;
      double var_Y3 = var_Y * var_Y * var_Y;
      double var_Z3 = var_Z * var_Z * var_Z;

      if (var_Y3 > 0.008856)
        var_Y = var_Y3;
      else
        var_Y = (var_Y - 16 / 116) / 7.787;

      if (var_X3 > 0.008856)
        var_X = var_X3;
      else
        var_X = (var_X - 16 / 116) / 7.787;

      if (var_Z3 > 0.008856)
        var_Z = var_Z3;
      else
        var_Z = (var_Z - 16 / 116) / 7.787;

      double X = ref_X * var_X;
      double Y = ref_Y * var_Y;
      double Z = ref_Z * var_Z;

      SetPDNColorXYZ(dstPixel, X, Y, Z);
    }

    ////////////////////////////////////////////////////////////////////////////


    unsafe private static void SetPDNColorXYZ(ColorBgra* dstPixel, double X, double Y, double Z)
    {
      // Standards used Observer = 2, Illuminant = D65
      // ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883

      double var_X = X / 100.0;
      double var_Y = Y / 100.0;
      double var_Z = Z / 100.0;

      double var_R = var_X * 3.2406 + var_Y * (-1.5372) + var_Z * (-0.4986);
      double var_G = var_X * (-0.9689) + var_Y * 1.8758 + var_Z * 0.0415;
      double var_B = var_X * 0.0557 + var_Y * (-0.2040) + var_Z * 1.0570;

      if (var_R > 0.0031308)
        var_R = 1.055 * (Math.Pow(var_R, 1 / 2.4)) - 0.055;
      else
        var_R = 12.92 * var_R;

      if (var_G > 0.0031308)
        var_G = 1.055 * (Math.Pow(var_G, 1 / 2.4)) - 0.055;
      else
        var_G = 12.92 * var_G;

      if (var_B > 0.0031308)
        var_B = 1.055 * (Math.Pow(var_B, 1 / 2.4)) - 0.055;
      else
        var_B = 12.92 * var_B;

      int nRed = (int)(var_R * 256.0);
      int nGreen = (int)(var_G * 256.0);
      int nBlue = (int)(var_B * 256.0);

      if (nRed < 0) nRed = 0;
      else if (nRed > 255) nRed = 255;
      if (nGreen < 0) nGreen = 0;
      else if (nGreen > 255) nGreen = 255;
      if (nBlue < 0) nBlue = 0;
      else if (nBlue > 255) nBlue = 255;

      dstPixel->R = (byte)nRed;
      dstPixel->G = (byte)nGreen;
      dstPixel->B = (byte)nBlue;
    }

    ///////////////////////////////////////////////////////////////////////////////
    //
    // The algorithms for these routines were taken from:
    //     http://www.neuro.sfc.keio.ac.jp/~aly/polygon/info/color-space-faq.html
    //
    // RGB --> CMYK                              CMYK --> RGB
    // ---------------------------------------   --------------------------------------------
    // Black   = minimum(1-Red,1-Green,1-Blue)   Red   = 1-minimum(1,Cyan*(1-Black)+Black)
    // Cyan    = (1-Red-Black)/(1-Black)         Green = 1-minimum(1,Magenta*(1-Black)+Black)
    // Magenta = (1-Green-Black)/(1-Black)       Blue  = 1-minimum(1,Yellow*(1-Black)+Black)
    // Yellow  = (1-Blue-Black)/(1-Black)
    //

    unsafe private static void SetPDNColorCMYK(ColorBgra* dstPixel, byte c, byte m, byte y, byte k)
    {
      double C, M, Y, K;

      C = (double)(255 - c) / 255;
      M = (double)(255 - m) / 255;
      Y = (double)(255 - y) / 255;
      K = (double)(255 - k) / 255;

      int nRed = (int)((1.0 - (C * (1 - K) + K)) * 255);
      int nGreen = (int)((1.0 - (M * (1 - K) + K)) * 255);
      int nBlue = (int)((1.0 - (Y * (1 - K) + K)) * 255);

      if (nRed < 0) nRed = 0;
      else if (nRed > 255) nRed = 255;
      if (nGreen < 0) nGreen = 0;
      else if (nGreen > 255) nGreen = 255;
      if (nBlue < 0) nBlue = 0;
      else if (nBlue > 255) nBlue = 255;

      dstPixel->R = (byte)nRed;
      dstPixel->G = (byte)nGreen;
      dstPixel->B = (byte)nBlue;
    }
  }
}