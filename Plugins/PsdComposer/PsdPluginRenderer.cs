using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using PhotoshopFile;
using System.Drawing;


namespace ImageResizer.Plugins.PsdComposer
{
    public class PsdPluginRenderer: IPsdRenderer
    {
        public Bitmap Render(Stream s, out IList<IPsdLayer> layers, out Size size,ShowLayerDelegate showLayerCallback, ComposeLayerDelegate composeLayer)
        {
            PsdFile file = new PsdFile();
            file.Load(s);
            //Start with a transparent bitmap (not all PSD files have background layers)
            size = new Size(file.Columns, file.Rows);
            Bitmap b = new Bitmap(file.Columns, file.Rows);

            //ImageDecoder.DecodeImage(file.Layers[0]); //Layers collection doesn't include the composed layer
            
            Graphics g = Graphics.FromImage(b);
            using (g){
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                for (int i = 0; i < file.Layers.Count; i++)
                {
                    if (showLayerCallback(i,file.Layers[i].Name,file.Layers[i].Visible)){
                        using (Bitmap frame = ImageDecoder.DecodeImage(file.Layers[i])){
                            composeLayer(g, frame, file.Layers[i]);
                        }
                    }
                }
            }
            layers = getLayers(file);
            return b;
        }

       
        public IList<IPsdLayer> GetLayers(Stream s)
        {
            PsdFile file = new PsdFile();
            file.Load(s);
            return getLayers(file);
        }
        public IList<IPsdLayer> GetLayersAndSize(Stream s, out Size size) {
            PsdFile file = new PsdFile();
            file.Load(s);
            size = new Size(file.Columns,file.Rows);
            return getLayers(file);
        }
        private IList<IPsdLayer> getLayers(PsdFile file)
        {
            List<IPsdLayer> items = new List<IPsdLayer>(file.Layers.Count);
            for (int i = 0; i < file.Layers.Count; i++)
            {
                items.Add(new PsdLayer(file.Layers[i],i));
            }
            return items;
        }

        class PsdLayer : PsdLayerBase
        {
            public PsdLayer(Layer layer, int index)
            {
                _name = layer.Name;
                _rect = layer.Rect;
                _visible = layer.Visible;
                _index = index;

                List<PhotoshopFile.Layer.AdjustmentLayerInfo> adjustments = layer.AdjustmentInfo;
                for (int j = 0; j < adjustments.Count; j++)
                {
                    if (adjustments[j].Key.Equals("TySh"))
                    {
                        this._isTextLayer = true;
                    }
                }
            }
        }

    }
}