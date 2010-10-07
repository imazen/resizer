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
        public Bitmap Render(Stream s, RenderLayerDelegate showLayerCallback)
        {
            PsdFile file = new PsdFile();
            file.Load(s);

            //Load background layer
            Bitmap b = ImageDecoder.DecodeImage(file.Layers[1]);
            
            Graphics g = Graphics.FromImage(b);
            using (g){
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                for (int i = 2; i < file.Layers.Count; i++)
                {
                    if (showLayerCallback(i,file.Layers[i].Name,file.Layers[i].Visible)){
                        using (Bitmap frame = ImageDecoder.DecodeImage(file.Layers[i])){
                            g.DrawImage(frame,file.Layers[i].Rect);
                        }
                    }
                }
            }
            return b;
        }

       
        public IList<ITextLayer> GetTextLayers(Stream s)
        {
            throw new NotImplementedException();
        }
    }
}