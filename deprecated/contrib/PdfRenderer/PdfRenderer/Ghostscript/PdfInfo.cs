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
#pragma warning disable 1591
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    [XmlRoot("pdf")]
    public class PdfInfo
    {
        public string FileName
        {
            get { return Decode(FileNameData); }
        }

        public string Title
        {
            get { return Decode(TitleData); }
        }

        public string Author
        {
            get { return Decode(AuthorData); }
        }

        public string Subject
        {
            get { return Decode(SubjectData); }
        }

        public string Keywords
        {
            get { return Decode(KeywordsData); }
        }

        public string Creator
        {
            get { return Decode(CreatorData); }
        }

        public string Producer
        {
            get { return Decode(ProducerData); }
        }

        public string CreationDate
        {
            get { return Decode(CreationDateData); }
        }

        public string ModifiedDate
        {
            get { return Decode(ModifiedDateData); }
        }

        [XmlArray("fileNameData"), XmlArrayItem("value")]
        public byte[] FileNameData { get; set; }

        [XmlArray("titleData"), XmlArrayItem("value")]
        public byte[] TitleData { get; set; }

        [XmlArray("authorData"), XmlArrayItem("value")]
        public byte[] AuthorData { get; set; }

        [XmlArray("subjectData"), XmlArrayItem("value")]
        public byte[] SubjectData { get; set; }

        [XmlArray("keywordsData"), XmlArrayItem("value")]
        public byte[] KeywordsData { get; set; }

        [XmlArray("creatorData"), XmlArrayItem("value")]
        public byte[] CreatorData { get; set; }

        [XmlArray("producerData"), XmlArrayItem("value")]
        public byte[] ProducerData { get; set; }

        [XmlArray("creationDateData"), XmlArrayItem("value")]
        public byte[] CreationDateData { get; set; }

        [XmlArray("modifiedDateData"), XmlArrayItem("value")]
        public byte[] ModifiedDateData { get; set; }

        [XmlElement("pageCount")]
        public int PageCount { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Used for deserialization.")]
        [XmlArray("pages"), XmlArrayItem("page")]
        public Collection<PageInfo> Pages { get; set; }

        private string Decode(byte[] data)
        {
            if(data == null) return null;
            if(data.Length == 0) return string.Empty;

            // Try to determine string encoding by detecting Byte Order Mark (BOM)
            // http://en.wikipedia.org/wiki/Byte_order_mark
            // UTF-32
            if(data.Length % 4 == 0)
            {
                if(data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
                {
                    // UTF-32 (Little Endian)
                    return System.Text.Encoding.UTF32.GetString(data.Skip(4).ToArray());
                }
            }
            // UTF-16
            if(data.Length % 2 == 0)
            {
                if(data[0] == 0xFE && data[1] == 0xFF)
                {
                    // UTF-16 (Big Endian)
                    return System.Text.Encoding.BigEndianUnicode.GetString(data.Skip(2).ToArray());
                }
                if(data[0] == 0xFF && data[1] == 0xFE)
                {
                    // UTF-16 (Little Endian)
                    return System.Text.Encoding.Unicode.GetString(data.Skip(2).ToArray());
                }
            }
            // UTF8
            if(data.Length >= 3)
            {
                if(data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                {
                    return System.Text.Encoding.UTF8.GetString(data.Skip(3).ToArray());                    
                }
            }
            return System.Text.Encoding.ASCII.GetString(data);
        }
    }
}
#pragma warning restore 1591