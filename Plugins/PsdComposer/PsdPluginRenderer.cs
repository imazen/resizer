using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using PhotoshopFile;
using System.Drawing;


namespace ImageResizer.Plugins.PsdComposer
{
    /// <summary>
    /// PsdPlugin renderer used to render a Bitmap image
    /// </summary>
    public class PsdPluginRenderer: IPsdRenderer
    {
        /// <summary>
        /// Render the image into a Bitmap
        /// </summary>
        /// <param name="s">I/O Stream</param>
        /// <param name="layers">List of PsdLayers</param>
        /// <param name="size">drawing size</param>
        /// <param name="showLayerCallback">show image layers</param>
        /// <param name="composeLayer">compose the layers</param>
        /// <returns>rendered Bitmap</returns>
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

       /// <summary>
       /// Gets layers from an I/O stream of an image
       /// </summary>
       /// <param name="s">I/O Stream of an image</param>
       /// <returns>List of PsdLayers</returns>
        public IList<IPsdLayer> GetLayers(Stream s)
        {
            PsdFile file = new PsdFile();
            file.Load(s);
            return getLayers(file);
        }

        /// <summary>
        /// Gets Layers and size from an I/O stream of an image
        /// </summary>
        /// <param name="s">I/O Stream of an image</param>
        /// <param name="size">size of image</param>
        /// <returns>List of PsdLayers</returns>
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