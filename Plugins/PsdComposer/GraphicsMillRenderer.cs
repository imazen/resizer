using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using Aurigma.GraphicsMill.Codecs;

namespace ImageResizer.Plugins.PsdComposer
{
    /// <summary>
    /// An Aurigma.GraphicsMill-based renderer. GraphicsMill was far too limited to do what we needed, so this got scrapped.
    /// </summary>
    public class GraphicsMillRenderer: IPsdRenderer
    {
        /// <summary>
        /// Ignores modifyLayer - not supported by this renderer.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="textLayers"></param>
        /// <param name="showLayerCallback"></param>
        /// <param name="modifyLayer"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap Render(Stream s, out IList<IPsdLayer> layers, RenderLayerDelegate showLayerCallback, ModifyLayerDelegate modifyLayer)
        {
            // Create the resultBitmap object which contains merged bitmap, 
            // and the currentBitmap object which contains current bitmap during iteration. 
            // These object enable you to operate with layers.
            Aurigma.GraphicsMill.Bitmap resultBitmap = new Aurigma.GraphicsMill.Bitmap();
            using (Aurigma.GraphicsMill.Bitmap currentBitmap = new Aurigma.GraphicsMill.Bitmap())
            {
                // Create advanced PSD reader object to read .psd files.
                using (Aurigma.GraphicsMill.Codecs.AdvancedPsdReader psdReader = new Aurigma.GraphicsMill.Codecs.AdvancedPsdReader(s))
                {

                    // Load the background layer which you will put other layers on. 
                    // Remember that the layer on zero position should be skiped 
                    // because it contains merged bitmap.
                    Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame frame;
                    using (frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(1))
                    {
                        frame.GetBitmap(resultBitmap);
                    }

                    //List of layers
                    layers = new List<IPsdLayer>();

                    //This code merges the rest layers with the background layer one by one.
                    for (int i = 2; i < psdReader.FrameCount; i++)
                    {
                        using (frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(i))
                        {

                            // Do not forget to verify the unknown layer type.  
                            if (frame.Type != Aurigma.GraphicsMill.Codecs.PsdFrameType.Unknown)
                            {
                                //Add layers
                                layers.Add(new PsdLayer(frame, i + 1));

                                bool showFrame = showLayerCallback(i - 1, frame.Name, frame.Visible); //Subtract 1 from index so layer 0 is the background layer
                                if (showFrame)
                                {
                                    // Extract the current image from the layer.
                                    frame.GetBitmap(currentBitmap);
                                   
                                    // Draw current layer on the result bitmap. 
                                    // Also check out if the layer is visible or not.
                                    // If the layer is invisible we skip it.
                                    currentBitmap.Draw(currentBitmap, frame.Left, frame.Top, frame.Width, frame.Height, Aurigma.GraphicsMill.Transforms.CombineMode.Alpha, 1, Aurigma.GraphicsMill.Transforms.InterpolationMode.HighQuality);
                                    
                                   
                                }
                            }
                        }
                    }
                }
            }
            return resultBitmap.ToGdiplusBitmapDirectly();
        }

       
        public IList<IPsdLayer> GetLayers(Stream s)
        {
            //List of  layers
            IList<IPsdLayer> layers = new List<IPsdLayer>();
            //Read PSD file
            using (Aurigma.GraphicsMill.Codecs.AdvancedPsdReader psdReader = new Aurigma.GraphicsMill.Codecs.AdvancedPsdReader(s))
            {
                for (int i = 1; i < psdReader.FrameCount; i++) //Start at 1, background frame
                {
                    using (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(i))
                    {
                        layers.Add(new PsdLayer(frame, i + 1));
                    }
                }
            }
            return layers;
        }

        class PsdLayer : PsdLayerBase
        {
            public PsdLayer(Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame frame, int index)
            {
                _name = frame.Name;
                _rect = new System.Drawing.Rectangle(frame.Left, frame.Top, frame.Width, frame.Height);
                _visible = frame.Visible;
                _index = index;
                _isTextLayer = (frame.Type == PsdFrameType.Text);
            }
        }
    }
}