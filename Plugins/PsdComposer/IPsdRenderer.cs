using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace ImageResizer.Plugins.PsdComposer
{
    /// <summary>
    /// delegate shows the image layers
    /// </summary>
    /// <param name="index"></param>
    /// <param name="Name"></param>
    /// <param name="visibleNow"></param>
    /// <returns></returns>
    public delegate bool ShowLayerDelegate(int index, string Name, bool visibleNow);

    /// <summary>
    /// deletegate to compose the layers
    /// </summary>
    /// <param name="g"></param>
    /// <param name="bitmap"></param>
    /// <param name="layer"></param>
    public delegate void ComposeLayerDelegate(Graphics g,Bitmap bitmap,  object layer);
    
    /// <summary>
    /// Render the image
    /// </summary>
    public interface IPsdRenderer
    {
        /// <summary>
        /// Render the image into a Bitmap
        /// </summary>
        /// <param name="s">I/O Stream</param>
        /// <param name="layers">List of PsdLayers</param>
        /// <param name="size">drawing size</param>
        /// <param name="showLayerCallback">show image layers</param>
        /// <param name="modifyLayer">compose the layers</param>
        /// <returns>rendered Bitmap</returns>
        Bitmap Render(Stream s, out IList<IPsdLayer> layers, out Size size, ShowLayerDelegate showLayerCallback, ComposeLayerDelegate modifyLayer);

        /// <summary>
        /// Get layers from the I/O Stream
        /// </summary>
        /// <param name="s">I/O Stream</param>
        /// <returns>Found PsdLayers</returns>
        IList<IPsdLayer> GetLayers(Stream s);

        /// <summary>
        /// Gets layers and show size
        /// </summary>
        /// <param name="s">I/O stream</param>
        /// <param name="size">size of drawing layers</param>
        /// <returns>Found PsdLayers</returns>
        IList<IPsdLayer> GetLayersAndSize(Stream s, out Size size);

    }

    /// <summary>
    /// PsdLayer interface defefining layers
    /// </summary>
    public interface IPsdLayer
    {
        /// <summary>
        /// Index of layer
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Name of Layer
        /// </summary>
        string Name{get;}

        /// <summary>
        /// Rectangle of where layer exists
        /// </summary>
        Rectangle Rect { get; }

        /// <summary>
        /// Text on layer
        /// </summary>
        string Text { get; }

        /// <summary>
        /// True if layer is Visible
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// True if layer is a text layer
        /// </summary>
        bool IsTextLayer { get; }
    }

    /// <summary>
    /// PsdLayerBase Initializes and sets default values for a layer
    /// </summary>
    public class PsdLayerBase : IPsdLayer
    {
        /// <summary>
        /// Name of layer
        /// </summary>
        protected string _name = null;

        /// <summary>
        /// Text on layer
        /// </summary>
        protected string _text = null;

        /// <summary>
        /// Rectangle where layer exists
        /// </summary>
        protected Rectangle _rect = Rectangle.Empty;

        /// <summary>
        /// True if layer is visible
        /// </summary>
        protected bool _visible = false;

        /// <summary>
        /// Index of layer
        /// </summary>
        protected int _index  = 0;

        /// <summary>
        /// True if layer is a text layer
        /// </summary>
        protected bool _isTextLayer = false;

        /// <summary>
        /// True if layer is a text layer
        /// </summary>
        public bool IsTextLayer { get { return _isTextLayer; } }

        /// <summary>
        /// Name of layer
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Rectangle where layer exists
        /// </summary>
        public Rectangle Rect
        {
            get { return _rect; }
        }

        /// <summary>
        /// Text from layer
        /// </summary>
        public string Text
        {
            get { return _text; }
        }

        /// <summary>
        ///  Index of layer
        /// </summary>
        public int Index {
            get{ return _index;}
        }

        /// <summary>
        /// true if layer is visible
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
        }
    }
}