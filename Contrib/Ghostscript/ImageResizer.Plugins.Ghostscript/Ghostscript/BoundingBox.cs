using System.Xml.Serialization;

namespace ImageResizer.Plugins.Pdf.Ghostscript
{
    public class BoundingBox
    {
        [XmlAttribute("left")]
        public double Left { get; set; }

        [XmlAttribute("top")]
        public double Top { get; set; }
        
        [XmlAttribute("width")]
        public double Width { get; set; }
        
        [XmlAttribute("height")]
        public double Height { get; set; }
    }
}