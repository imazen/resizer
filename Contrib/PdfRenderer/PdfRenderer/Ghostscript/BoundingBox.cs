using System.Xml.Serialization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    /// BoundingBox defines a boxed area in a PDF that can be selected
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// Left side coordinate of BoundingBox
        /// </summary>
        [XmlAttribute("left")]
        public double Left { get; set; }

        /// <summary>
        /// Right side coordinate of BoundingBox
        /// </summary>
        [XmlAttribute("top")]
        public double Top { get; set; }
        
        /// <summary>
        /// Width of Bounding box
        /// </summary>
        [XmlAttribute("width")]
        public double Width { get; set; }
        
        /// <summary>
        /// Height of bounding box
        /// </summary>
        [XmlAttribute("height")]
        public double Height { get; set; }
    }
}