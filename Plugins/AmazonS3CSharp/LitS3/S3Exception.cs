using System;
using System.Net;
using System.Xml;
using System.IO;

namespace LitS3
{
    /// <summary>
    /// The exception that is thrown when the S3 server returns a specially formatted error object
    /// that we can parse.
    /// </summary>
    public sealed class S3Exception : Exception
    {
        /// <summary>
        /// Gets the error code returned by S3.
        /// </summary>
        public S3ErrorCode ErrorCode { get; private set; }

        /// <summary>
        /// Gets the bucket name this error pertains to, if applicable.
        /// </summary>
        public string BucketName { get; private set; }

        /// <summary>
        /// Gets the ID of the request associated with the error.
        /// </summary>
        public string RequestID { get; private set; }

        /// <summary>
        /// Gets the ID of the host that returned the error.
        /// </summary>
        public string HostID { get; private set; }

        public S3Exception(S3ErrorCode errorCode, string bucketName, string message, WebException innerException)
            : base(message, innerException)
        {
            this.ErrorCode = errorCode;
            this.BucketName = bucketName;
        }

        internal static S3Exception FromErrorResponse(XmlReader reader, WebException exception)
        {
            if (reader.IsEmptyElement)
                throw new Exception("Expected a non-empty <Error> element.");

            reader.ReadStartElement("Error");

            S3ErrorCode errorCode = S3ErrorCode.Unknown;
            string message = null, bucketName = null, requestID = null, hostID = null;
            
            while (reader.Name != "Error")
            {
                switch (reader.Name)
                {
                    case "Code":
                        errorCode = ParseCode(reader.ReadElementContentAsString());
                        break;
                    case "Message":
                        message = reader.ReadElementContentAsString();
                        break;
                    case "BucketName":
                        bucketName = reader.ReadElementContentAsString();
                        break;
                    case "RequestID":
                        requestID = reader.ReadElementContentAsString();
                        break;
                    case "HostID":
                        hostID = reader.ReadElementContentAsString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new S3Exception(errorCode, bucketName, message, exception)
            {
                RequestID = requestID,
                HostID = hostID
            };
        }

        internal static S3Exception FromWebException(WebException exception)
        {
            HttpWebResponse response = (HttpWebResponse)exception.Response;

            // we need to check the response stream first to make sure there's actually
            // XML in the response. S3 sometimes sends error responses with content-type XML
            // and content-encoding chunked, then no actual data!
            var streamReader = new StreamReader(response.GetResponseStream());

            if (streamReader.EndOfStream)
                return null;
            
            var xmlReader = new XmlTextReader(streamReader)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Namespaces = false
            };
            
            xmlReader.MoveToContent();
            return FromErrorResponse(xmlReader, exception);
        }

        static S3ErrorCode ParseCode(string code)
        {
            if (Enum.IsDefined(typeof(S3ErrorCode), code))
                return (S3ErrorCode)Enum.Parse(typeof(S3ErrorCode), code);
            else
                return S3ErrorCode.Unknown;
        }
    }
}
