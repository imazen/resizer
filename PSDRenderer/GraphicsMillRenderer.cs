using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using Aurigma.GraphicsMill.Codecs;

namespace PsdRenderer
{
    public class GraphicsMillRenderer: IPsdRenderer
    {
        public System.Drawing.Bitmap Render(Stream s, RenderLayerDelegate showLayerCallback)
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

                    //This code merges the rest layers with the background layer one by one.
                    for (int i = 2; i < psdReader.FrameCount; i++)
                    {
                        using (frame = (Aurigma.GraphicsMill.Codecs.AdvancedPsdFrame)psdReader.LoadFrame(i))
                        {

                            // Do not forget to verify the unknown layer type.  
                            if (frame.Type != Aurigma.GraphicsMill.Codecs.PsdFrameType.Unknown)
                            {
                                bool showFrame = showLayerCallback(i - 1, frame.Name, frame.Visible); //Subtract 1 from index so layer 0 is the background layer
                                if (showFrame)
                                {
                                    // Extract the current image from the layer.
                                    frame.GetBitmap(currentBitmap);

                                    // Draw current layer on the result bitmap. 
                                    // Also check out if the layer is visible or not.
                                    // If the layer is invisible we skip it.
                                    currentBitmap.Draw(resultBitmap, frame.Left, frame.Top, frame.Width, frame.Height, Aurigma.GraphicsMill.Transforms.CombineMode.Alpha, 1, Aurigma.GraphicsMill.Transforms.InterpolationMode.HighQuality);
                                }
                            }
                        }
                    }
                }
            }
            return resultBitmap.ToGdiplusBitmapDirectly();
        }

       
        public IList<ITextLayer> GetTextLayers(Stream s)
        {
            throw new NotImplementedException();
        }
    }
}