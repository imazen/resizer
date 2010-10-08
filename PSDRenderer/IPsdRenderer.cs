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
        

         Bitmap Render(Stream s, out IList<ITextLayer> textLayers, RenderLayerDelegate showLayerCallback);

         IList<ITextLayer> GetTextLayers(Stream s);
    }

    public interface ITextLayer
    {
        int Index { get; }
        string Name{get;}
        Rectangle Rect { get; }
        string Text { get; }
        bool Visible { get; }
    }

    public class TextLayerBase : ITextLayer
    {
        protected string _name = null;
        protected string _text = null;
        protected Rectangle _rect = Rectangle.Empty;
        protected bool _visible = false;
        protected int _index  = 0;

        public string Name
        {
            get { return _name; }
        }

        public Rectangle Rect
        {
            get { return _rect; }
        }

        public string Text
        {
            get { return _text; }
        }
        public int Index {
            get{ return _index;}
        }
        public bool Visible
        {
            get { return _visible; }
        }
    }
}