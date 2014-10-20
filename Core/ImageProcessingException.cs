using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ImageResizer {
    /// <summary>
    /// Represents an non-recoverable exception that occured while processing the image. 
    /// Possible causes are: a corrupted source file, cache resource timeout (such as a locked file in imagecache),
    /// unusable configuration (for example, no registered encoders or caches), invalid syntax, or a size limit was exceeded and the request was stopped by the SizeLimiting extension.
    /// May also be caused by a missing source file/resource, in the form of the ImageMissingException subclass.
    /// </summary>
    public class ImageProcessingException: HttpException {


        public ImageProcessingException(string message)
            : base(500, message) {
        }


        public ImageProcessingException(int httpCode, string message)
            : base(httpCode,message) {
        }
        public ImageProcessingException(int httpCode, string message, string safeMessage)
            : base(httpCode, message) {
                this.publicSafeMessage = safeMessage;
        }
        public ImageProcessingException(int httpCode, string message, string safeMessage, Exception innerException)
            : base(httpCode, message,innerException) {
                this.publicSafeMessage = safeMessage;
        }
        private string publicSafeMessage = null;
        /// <summary>
        /// This error message is safe to display to the public (should not contain any sensitive information)
        /// </summary>
        protected string PublicSafeMessage {
            get { return publicSafeMessage; }
            set { publicSafeMessage = value; }
        }
    }

    /// <summary>
    /// A source file was corrupted
    /// </summary>
    public class ImageCorruptedException : ImageProcessingException {
        public ImageCorruptedException(string message, Exception innerException) : base(500, message, message, innerException) { }
    }
    /// <summary>
    /// One or more source files was missing
    /// </summary>
    public class ImageMissingException : ImageProcessingException {
        public ImageMissingException(string message) : base(404, message) { }


        public ImageMissingException(string message, string safeMessage)
            : base(404, message, safeMessage) {
        }

        public ImageMissingException(string message, string safeMessage, Exception innerException)
            : base(404, message, safeMessage, innerException) {
        }
    }
}
