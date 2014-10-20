using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace ImageResizer.Plugins.PsdComposer
{
    public delegate bool ShowLayerDelegate(int index, string Name, bool visibleNow);
    public delegate void ComposeLayerDelegate(Graphics g,Bitmap bitmap,  object layer);
    public interface IPsdRenderer
    {
        Bitmap Render(Stream s, out IList<IPsdLayer> layers, out Size size, ShowLayerDelegate showLayerCallback, ComposeLayerDelegate modifyLayer);

        IList<IPsdLayer> GetLayers(Stream s);

        IList<IPsdLayer> GetLayersAndSize(Stream s, out Size size);

    }

    public interface IPsdLayer
    {
        int Index { get; }
        string Name{get;}
        Rectangle Rect { get; }
        string Text { get; }
        bool Visible { get; }
        bool IsTextLayer { get; }
    }

    public class PsdLayerBase : IPsdLayer
    {
        protected string _name = null;
        protected string _text = null;
        protected Rectangle _rect = Rectangle.Empty;
        protected bool _visible = false;
        protected int _index  = 0;
        protected bool _isTextLayer = false;

        public bool IsTextLayer { get { return _isTextLayer; } }

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