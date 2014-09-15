// Copyright (c) 2012 Jason Morse
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Xml.Serialization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    /// Page information that can be retrieved from a PDF page
    /// </summary>
    public class PageInfo
    {
        /// <summary>
        /// Page Number
        /// </summary>
        [XmlAttribute("number")]
        public int Number { get; set; }

        /// <summary>
        /// Transparency of the page
        /// </summary>
        [XmlAttribute("transparency")]
        public bool Transparency { get; set; }

        /// <summary>
        /// Degrees the page is Rotated
        /// </summary>
        [XmlAttribute("rotate")]
        public int Rotate { get; set; }

        /// <summary>
        /// Media box can be used to select an image on the page
        /// </summary>
        [XmlElement("mediaBox")]
        public BoundingBox MediaBox { get; set; }

        /// <summary>
        /// Crop box can be used to crop an area of the page
        /// </summary>
        [XmlElement("cropBox")]
        public BoundingBox CropBox { get; set; }
    }
}