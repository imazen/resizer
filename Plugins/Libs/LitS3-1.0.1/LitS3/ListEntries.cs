using System;
using System.Xml;

namespace LitS3
{
    /// <summary>
    /// The base class for items returned by a ListObjectsRequest. The only two concrete subclasses
    /// are ObjectEntry and CommonPrefix.
    /// </summary>
    public abstract class ListEntry
    {
        internal ListEntry() { }

        /// <summary>
        /// Gets the name of this entry, which is the portion of the key or common prefix after the
        /// search prefix.
        /// </summary>
        public string Name { get; protected set; }
    }

    /// <summary>
    /// Represents an S3 Object.
    /// </summary>
    public sealed class ObjectEntry : ListEntry
    {
        /// <summary>
        /// Gets the unique S3 Object key.
        /// </summary>
        public string Key { get; private set; }
        
        /// <summary>
        /// Gets the last modified date of this object, as determined by S3.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Gets the ETag of this object, as computed by S3.
        /// </summary>
        public string ETag { get; private set; }

        /// <summary>
        /// Get the size of this object.
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// Gets the owner of this object.
        /// </summary>
        public Identity Owner { get; private set; }

        internal ObjectEntry(XmlReader reader, string searchPrefix, string delimiter)
        {
            if (reader.IsEmptyElement)
                throw new Exception("Expected a non-empty <Contents> element.");

            reader.ReadStartElement("Contents");
            this.Key = reader.ReadElementContentAsString("Key", "");
            this.LastModified = reader.ReadElementContentAsDateTime("LastModified", "");
            this.ETag = reader.ReadElementContentAsString("ETag", "");
            this.Size = reader.ReadElementContentAsLong("Size", "");

            // this tag may be omitted if you don't have permission to view the owner
            if (reader.Name == "Owner")
                this.Owner = new Identity(reader);

            // this element is meaningless
            if (reader.Name == "StorageClass")
                reader.Skip();

            reader.ReadEndElement();

            this.Name = Key;

            if (!string.IsNullOrEmpty(searchPrefix))
                this.Name = Name.Substring(searchPrefix.Length);
        }

        public override string ToString()
        {
            return string.Format("S3Object \"{0}\"", Name);
        }
    }

    /// <summary>
    /// Represents a common prefix rolled up by a ListObjectsRequest. In a filesystem-like
    /// interpretation of Amazon S3 using a delimiter of "/", you might consider this a "directory".
    /// </summary>
    public sealed class CommonPrefix : ListEntry
    {
        /// <summary>
        /// Gets the prefix common to one or more items found by the ListObjectsRequest.
        /// </summary>
        public string Prefix { get; private set; }

        internal CommonPrefix(XmlReader reader, string searchPrefix, string delimiter)
        {
            if (reader.IsEmptyElement)
                throw new Exception("Expected a non-empty <Prefix> element.");

            this.Prefix = reader.ReadElementContentAsString("Prefix", "");

            this.Name = Prefix;

            if (!string.IsNullOrEmpty(searchPrefix))
                this.Name = Name.Substring(searchPrefix.Length);

            if (!string.IsNullOrEmpty(delimiter))
                this.Name = Name.Substring(0, Name.Length - delimiter.Length);
        }

        public override string ToString()
        {
            return string.Format("Common Prefix \"{0}\"", Name);
        }
    }
}
