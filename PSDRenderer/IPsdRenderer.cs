using Aurigma.GraphicsMill.Codecs;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using fbs.ImageResizer;
namespace PsdRenderer
{
    public delegate bool RenderLayerDelegate(int index, string Name, bool visibleNow);
    public interface IPsdRenderer
    {
        

         Bitmap Render(Stream s, RenderLayerDelegate showLayerCallback);

         IList<ITextLayer> GetTextLayers(Stream s);
    }

    public interface ITextLayer
    {
        string name{get;}
        Rectangle rect { get; }
        string text { get; }
    }
}