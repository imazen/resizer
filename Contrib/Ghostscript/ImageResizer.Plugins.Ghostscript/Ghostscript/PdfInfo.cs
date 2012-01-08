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

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace ImageResizer.Plugins.Pdf.Ghostscript
{
    [XmlRoot("pdf")]
    public class PdfInfo
    {
        [XmlElement("fileName")]
        public string FileName { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("author")]
        public string Author { get; set; }

        [XmlElement("subject")]
        public string Subject { get; set; }

        [XmlElement("keywords")]
        public string Keywords { get; set; }

        [XmlElement("creator")]
        public string Creator { get; set; }

        [XmlElement("producer")]
        public string Producer { get; set; }

        [XmlElement("creationDate")]
        public string CreationDate { get; set; }

        [XmlElement("modifiedDate")]
        public string ModifiedDate { get; set; }

        [XmlElement("pageCount")]
        public int PageCount { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Used for deserialization.")]
        [XmlArray("pages"), XmlArrayItem("page")]
        public Collection<PageInfo> Pages { get; set; }
    }
}