using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using PhotoshopFile;
using System.Drawing;
using fbs.ImageResizer;
using fbs;


namespace PsdRenderer
{
    public class PsdPluginRenderer: IPsdRenderer
    {
        public Bitmap Render(Stream s, out IList<ITextLayer> textLayers, RenderLayerDelegate showLayerCallback)
        {
            PsdFile file = new PsdFile();
            file.Load(s);
            //Load background layer
            Bitmap b = ImageDecoder.DecodeImage(file.Layers[0]); //Layers collection doesn't include the composed layer
            
            Graphics g = Graphics.FromImage(b);
            using (g){
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                for (int i = 1; i < file.Layers.Count; i++)
                {
                    if (showLayerCallback(i,file.Layers[i].Name,file.Layers[i].Visible)){
                        using (Bitmap frame = ImageDecoder.DecodeImage(file.Layers[i])){
                            g.DrawImage(frame,file.Layers[i].Rect);
                        }
                    }
                }
            }
            textLayers = getTextLayers(file);
            return b;
        }

       
        public IList<ITextLayer> GetTextLayers(Stream s)
        {
            PsdFile file = new PsdFile();
            file.Load(s);
            return getTextLayers(file);
        }
        private IList<ITextLayer> getTextLayers(PsdFile file)
        {
            List<ITextLayer> items = new List<ITextLayer>(file.Layers.Count);
            for (int i = 1; i < file.Layers.Count; i++)
            {
                List<PhotoshopFile.Layer.AdjustmentLayerInfo> adjustments = file.Layers[i].AdjustmentInfo;
                for (int j = 0; j < adjustments.Count; j++)
                {
                    if (adjustments[j].Key.Equals("TySh"))
                    {
                        items.Add(new TextLayer(file.Layers[i],i));
                    }
                }
            }
            return items;
        }

        class TextLayer : TextLayerBase
        {
            public TextLayer(Layer layer, int index)
            {
                _name = layer.Name;
                _rect = layer.Rect;
                _visible = layer.Visible;
                _index = index;
            }
        }

    }
}