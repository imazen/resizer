using System.Xml.Serialization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    /// Bounding box for the pdf.
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// Declares the coordinates for the left side of the bounding box.
        /// </summary>
        [XmlAttribute("left")]
        public double Left { get; set; }

        /// <summary>
        /// Declares the coordinates for the top of the bounding box.
        /// </summary>
        [XmlAttribute("top")]
        public double Top { get; set; }

        /// <summary>
        /// Declares the total width of the bounding box.
        /// </summary>
        [XmlAttribute("width")]
        public double Width { get; set; }
        
        /// <summary>
        /// Declares the total height of the bounding box.
        /// </summary>
        [XmlAttribute("height")]
        public double Height { get; set; }
    }
}