using System;
using System.Xml;

namespace LitS3
{
    /// <summary>
    /// Represents a bucket hosted by Amazon S3 that contains objects.
    /// </summary>
    public sealed class Bucket
    {
        /// <summary>
        /// The name of this bucket.
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// The creation time of this bucket, as determined by S3.
        /// </summary>
        public DateTime CreationDate { get; private set; }

        internal Bucket(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                throw new Exception("Expected a non-empty <Bucket> element.");

            // Example:
            // <Bucket>
            //     <Name>quotes;/Name>
            //     <CreationDate>2006-02-03T16:45:09.000Z</CreationDate>
            // </Bucket>
            reader.ReadStartElement("Bucket");
            this.Name = reader.ReadElementContentAsString("Name", "");
            this.CreationDate = reader.ReadElementContentAsDateTime("CreationDate", "");
            reader.ReadEndElement();
        }

        public override string ToString()
        {
            return string.Format("Bucket \"{0}\"", Name);
        }
    }
}
