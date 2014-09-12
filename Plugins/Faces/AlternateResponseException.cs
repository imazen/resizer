using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.Faces {

    /// <summary>
    /// Custom Exception with added Response data.  Includes the ResponseData, ContentType, and StatusCode.
    /// </summary>
    public class AlternateResponseException:Exception {

        /// <summary>
        /// Byte array of AlternateResponseException data
        /// </summary>
        public byte[] ResponseData { get; set; }

        /// <summary>
        /// string representation of the type of Content in the Exception
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Exception StatusCode
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Generates a AlternateResponseException with an Exception message
        /// </summary>
        /// <param name="message">Exception message data</param>
        public AlternateResponseException(string message) : base(message) { }

    }
}
