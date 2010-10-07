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
using PaintDotNet.Threading;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;

using PhotoshopFile;

namespace PaintDotNet.Data.PhotoshopFileType
{
  public class PhotoshopFileTypes
      : IFileTypeFactory
  {
    public static readonly FileType Psd = new PhotoshopFileType();

    private static FileType[] fileTypes = new FileType[] { Psd };

    internal FileTypeCollection GetFileTypeCollection()
    {
      return new FileTypeCollection((ICollection<FileType>)fileTypes);
    }

    public FileType[] GetFileTypeInstances()
    {
      return (FileType[])fileTypes.Clone();
    }
  }


  public class PhotoshopFileType : FileType
  {
    public PhotoshopFileType()
      : base("Photoshop",
             FileTypeFlags.SupportsLoading |
               FileTypeFlags.SupportsSaving |
               FileTypeFlags.SavesWithProgress |
               FileTypeFlags.SupportsLayers,
             new string[] { ".psd" })
    {
    }

    public override SaveConfigWidget CreateSaveConfigWidget()
    {
      return new PsdSaveConfigWidget();
    }

    protected override SaveConfigToken OnCreateDefaultSaveConfigToken()
    {
      return new PsdSaveConfigToken(true);
    }

    protected override void OnSave(Document input, System.IO.Stream output, SaveConfigToken token, Surface scratchSurface, ProgressEventHandler callback)
    {
      PsdSaveConfigToken psdToken = (PsdSaveConfigToken)token;
      PsdFile psdFile = new PsdFile();

      //-----------------------------------------------------------------------

      psdFile.Rows = input.Height;
      psdFile.Columns = input.Width;

      // we have an Alpha channel which will be saved, 
      // we have to add this to our image resources
      psdFile.Channels = 4;

      // for now we oly save the images as RGB
      psdFile.ColorMode = PsdFile.ColorModes.RGB;

      psdFile.Depth = 8;

      //-----------------------------------------------------------------------
      // no color mode Data

      //-----------------------------------------------------------------------

      ResolutionInfo resInfo = new ResolutionInfo();

      resInfo.HeightUnit = ResolutionInfo.Unit.In;
      resInfo.WidthUnit = ResolutionInfo.Unit.In;

      if (input.DpuUnit == MeasurementUnit.Inch)
      {
        resInfo.HResUnit = ResolutionInfo.ResUnit.PxPerInch;
        resInfo.VResUnit = ResolutionInfo.ResUnit.PxPerInch;

        resInfo.HRes = (short)input.DpuX;
        resInfo.VRes = (short)input.DpuY;
      }
      else
      {
        resInfo.HResUnit = ResolutionInfo.ResUnit.PxPerCent;
        resInfo.VResUnit = ResolutionInfo.ResUnit.PxPerCent;


        resInfo.HRes = (short)(input.DpuX / 2.54);
        resInfo.VRes = (short)(input.DpuY / 2.54);
      }

      psdFile.Resolution = resInfo;
      //-----------------------------------------------------------------------

      psdFile.ImageCompression = psdToken.RleCompress ? ImageCompression.Rle : ImageCompression.Raw;

      int size = psdFile.Rows * psdFile.Columns;

      psdFile.ImageData = new byte[psdFile.Channels][];
      for (int i = 0; i < psdFile.Channels; i++)
      {
        psdFile.ImageData[i] = new byte[size];
      }

      using (RenderArgs ra = new RenderArgs(scratchSurface))
      {
        input.Flatten(scratchSurface);
      }

      unsafe
      {
        for (int y = 0; y < psdFile.Rows; y++)
        {
          int rowIndex = y * psdFile.Columns;
          ColorBgra* srcRow = scratchSurface.GetRowAddress(y);
          ColorBgra* srcPixel = srcRow;

          for (int x = 0; x < psdFile.Columns; x++)
          {
            int pos = rowIndex + x;

            psdFile.ImageData[0][pos] = srcPixel->R;
            psdFile.ImageData[1][pos] = srcPixel->G;
            psdFile.ImageData[2][pos] = srcPixel->B;
            psdFile.ImageData[3][pos] = srcPixel->A;
            srcPixel++;
          }
        }
      }

      PaintDotNet.Threading.PrivateThreadPool threadPool = new PaintDotNet.Threading.PrivateThreadPool();
      foreach (BitmapLayer layer in input.Layers)
      {
        PhotoshopFile.Layer psdLayer = new PhotoshopFile.Layer(psdFile);
        BlendOpToBlendModeKey(layer.BlendOp, psdLayer);

        SaveLayerPixelsContext slc = new SaveLayerPixelsContext(layer, psdFile, input, psdLayer, psdToken);
        WaitCallback waitCallback = new WaitCallback(slc.SaveLayer);
        threadPool.QueueUserWorkItem(waitCallback);
      }
      threadPool.Drain();

      psdFile.Save(output);
    }

    public static void SaveLayerPixels(BitmapLayer layer, PsdFile psdFile,
        Document input, PhotoshopFile.Layer psdLayer, PsdSaveConfigToken psdToken)
    {      
      Surface surface = layer.Surface;

      int rectLeft = input.Width;
      int rectTop = input.Height;
      int rectRight = 0;
      int rectBottom = 0;

      // Determine the real size of this layer, i.e., the smallest rectangle
      // that includes all all non-invisible pixels
      unsafe
      {
        for (int y = 0; y < psdFile.Rows; y++)
        {
          int rowIndex = y * psdFile.Columns;
          ColorBgra* srcRow = surface.GetRowAddress(y);
          ColorBgra* srcPixel = srcRow;

          for (int x = 0; x < psdFile.Columns; x++)
          {
            int pos = rowIndex + x;

            // Found a non-transparent pixel, potentially increase the size of the rectangle
            if (srcPixel->A > 0)
            {
              // Expand the rectangle
              if (x < rectLeft)
                rectLeft = x;
              if (x > rectRight)
                rectRight = x;
              if (y < rectTop)
                rectTop = y;
              if (y > rectBottom)
                rectBottom = y;
            }

            srcPixel++;
          }
        }
      }

      psdLayer.Rect = new Rectangle(rectLeft, rectTop, rectRight - rectLeft + 1, rectBottom - rectTop + 1);
      psdLayer.Name = layer.Name;
      psdLayer.Opacity = layer.Opacity;
      psdLayer.Visible = layer.Visible;
      psdLayer.MaskData = new PhotoshopFile.Layer.Mask(psdLayer);
      psdLayer.BlendingRangesData = new PhotoshopFile.Layer.BlendingRanges(psdLayer);

      int layerSize = psdLayer.Rect.Width * psdLayer.Rect.Height;

      for (int i = -1; i < 3; i++)
      {
        PhotoshopFile.Layer.Channel ch = new PhotoshopFile.Layer.Channel((short)i, psdLayer);

        ch.ImageCompression = psdToken.RleCompress ? ImageCompression.Rle : ImageCompression.Raw;
        ch.ImageData = new byte[layerSize];
      }

      var channels = psdLayer.ChannelsArray;
      var alphaChannel = psdLayer.AlphaChannel;

      unsafe
      {
        int rowIndex = 0;
        for (int y = 0; y < psdLayer.Rect.Height; y++)
        {
          ColorBgra* srcRow = surface.GetRowAddress(y + psdLayer.Rect.Top);
          ColorBgra* srcPixel = srcRow + psdLayer.Rect.Left;

          for (int x = 0; x < psdLayer.Rect.Width; x++)
          {
            int pos = rowIndex + x;

            channels[0].ImageData[pos] = srcPixel->R;
            channels[1].ImageData[pos] = srcPixel->G;
            channels[2].ImageData[pos] = srcPixel->B;
            alphaChannel.ImageData[pos] = srcPixel->A;
            srcPixel++;
          }
          rowIndex += psdLayer.Rect.Width;
        }
      }
    }

    private void BlendOpToBlendModeKey(UserBlendOp op, PhotoshopFile.Layer layer)
    {

      switch (op.ToString())
      {
        case "Normal":
          layer.BlendModeKey = "norm";
          break;
        case "Multiply":
          layer.BlendModeKey = "mul ";
          break;
        case "Additive":
          layer.BlendModeKey = "norm";
          break;
        case "ColorBurn":
          layer.BlendModeKey = "div ";
          break;
        case "ColorDodge":
          layer.BlendModeKey = "idiv";
          break;
        case "Reflect":
          layer.BlendModeKey = "norm";
          break;
        case "Glow":
          layer.BlendModeKey = "norm";
          break;
        case "Overlay":
          layer.BlendModeKey = "over";
          break;
        case "Difference":
          layer.BlendModeKey = "diff";
          break;
        case "Negation":
          layer.BlendModeKey = "norm";
          break;
        case "Lighten":
          layer.BlendModeKey = "lite";
          break;
        case "Darken":
          layer.BlendModeKey = "dark";
          break;
        case "Screen":
          layer.BlendModeKey = "scrn";
          break;
        case "Xor":
          layer.BlendModeKey = "norm";
          break;
        default:
          layer.BlendModeKey = "norm";
          break;
      }
    }

    private UserBlendOp BlendModeKeyToBlendOp(PhotoshopFile.Layer layer)
    {
      UserBlendOp blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.NormalBlendOp));
      switch (layer.BlendModeKey)
      {
        case "norm":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.NormalBlendOp));
          break;
        case "dark":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.DarkenBlendOp));
          break;
        case "lite":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.LightenBlendOp));
          break;
        case "hue ":
          break;
        case "sat ":
          break;
        case "colr":
          break;
        case "lum ":
          break;
        case "mul ":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.MultiplyBlendOp));
          break;
        case "scrn":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.ScreenBlendOp));
          break;
        case "diss":
          break;
        case "over":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.OverlayBlendOp));
          break;
        case "hLit":
          break;
        case "sLit":
          break;
        case "diff":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.DifferenceBlendOp));
          break;
        case "smud":
          break;
        case "div ":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.ColorDodgeBlendOp));
          break;
        case "idiv":
          blendOp = UserBlendOps.CreateBlendOp(typeof(UserBlendOps.ColorBurnBlendOp));
          break;
      }
      return blendOp;
    }

    protected override Document OnLoad(System.IO.Stream input)
    {
      PsdFile psdFile = new PsdFile();

      psdFile.Load(input);

      Document document = new Document(psdFile.Columns, psdFile.Rows);

      if (psdFile.Resolution != null)
      {
        document.DpuUnit = MeasurementUnit.Inch;
        document.DpuX = psdFile.Resolution.HRes;
        document.DpuY = psdFile.Resolution.VRes;
      }

      if (psdFile.Layers.Count == 0)
      {
        BitmapLayer layer = ImageDecoderPdn.DecodeImage(psdFile);
        document.Layers.Add(layer);
      }
      else
      {
        PaintDotNet.Threading.PrivateThreadPool threadPool = new PaintDotNet.Threading.PrivateThreadPool();
        var layersList = new List<Layer>();
        foreach (PhotoshopFile.Layer l in psdFile.Layers)
        {
          if (!l.Rect.IsEmpty)
          {
            layersList.Add(null);
            LoadLayerContext llc = new LoadLayerContext(l, document, BlendModeKeyToBlendOp(l), layersList, layersList.Count - 1);
            WaitCallback waitCallback = new WaitCallback(llc.LoadLayer);
            threadPool.QueueUserWorkItem(waitCallback); 
          }
        }
        threadPool.Drain();

        foreach (var layer in layersList)
        {
          document.Layers.Add(layer);
        }

      }
      return document;
    }

    private class SaveLayerPixelsContext
    {
      private BitmapLayer layer;
      private PsdFile psdFile;
      private Document input;
      private PsdSaveConfigToken psdToken;
      PhotoshopFile.Layer psdLayer;

      public SaveLayerPixelsContext(BitmapLayer layer, PsdFile psdFile,
        Document input, PhotoshopFile.Layer psdLayer, PsdSaveConfigToken psdToken)
      {
        this.layer = layer;
        this.psdFile = psdFile;
        this.input = input;
        this.psdToken = psdToken;
        this.psdLayer = psdLayer;
      }

      public void SaveLayer(object context)
      {
        SaveLayerPixels(layer, psdFile, input, psdLayer, psdToken);
      }
    }

    private class LoadLayerContext
    {
      PhotoshopFile.Layer psdLayer;
      Document document;
      UserBlendOp blendOp;
      List<Layer> layersList;
      int idxLayersList;

      public LoadLayerContext(PhotoshopFile.Layer psdLayer, Document document, UserBlendOp blendOp, List<Layer> layersList, int idxLayersList)
      {
        this.psdLayer = psdLayer;
        this.document = document;
        this.blendOp = blendOp;
        this.layersList = layersList;
        this.idxLayersList = idxLayersList;
      }

      public void LoadLayer(object context)
      {
        var layer = ImageDecoderPdn.DecodeImage(psdLayer);
        layer.Name = psdLayer.Name;
        layer.Opacity = psdLayer.Opacity;
        layer.Visible = psdLayer.Visible;
        layer.SetBlendOp(blendOp);

        layersList[idxLayersList] = layer;
      }
    }
  }

}
