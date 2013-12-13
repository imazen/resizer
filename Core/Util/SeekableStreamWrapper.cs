using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util
{
    /// <summary>
    /// A Stream utility class that helps to provide seekability to any source
    /// stream.
    /// </summary>
    public class SeekableStreamWrapper : MemoryStream
    {
        private Stream inner;
        private bool disposeInner;
        private bool writable;

        /// <summary>
        /// Returns a seekable stream for a given source stream.  If wrapped,
        /// the source stream will be disposed when the returned stream is.
        /// </summary>
        /// <param name="sourceStream">The stream to wrap or return.</param>
        /// <returns>If the source stream is already seekable, it is returned
        /// unwrapped.  If not, it is wrapped in a <c>SeekableStreamWrapper</c>.</returns>
        public static Stream FromStream(Stream sourceStream)
        {
            bool disposeStream = true;
            return FromStream(sourceStream, ref disposeStream);
        }

        /// <summary>
        /// Returns a seekable stream for a given source stream.  If wrapped,
        /// the source stream will be disposed when the returned stream is.
        /// </summary>
        /// <param name="sourceStream">The stream to wrap or return.</param>
        /// <param name="disposeStream">Whether to dispose the source stream
        /// when the wrapper is disposed.  If a wrapper is created, this
        /// parameter will be <c>true</c> on the return to ensure the wrapper
        /// gets properly disposed.</param>
        /// <returns>If the source stream is already seekable, it is returned
        /// unwrapped.  If not, it is wrapped in a <c>SeekableStreamWrapper</c>.</returns>
        public static Stream FromStream(Stream sourceStream, ref bool disposeStream)
        {
            // If the stream is already seekable, we can just return it directly.
            if (sourceStream.CanSeek)
            {
                return sourceStream;
            }

            var disposeInner = disposeStream;
            disposeStream = true;

            return new SeekableStreamWrapper(sourceStream, disposeInner);
        }

        private SeekableStreamWrapper(Stream inner, bool disposeInner)
        {
            this.inner = inner;
            this.disposeInner = disposeInner;

            // Copy the original stream, and reset the position back to the
            // beginning.  We *could* immediately dispose the inner stream, but
            // we're keeping it around so that its lifetime is consistent with
            // what it would be if the wrapper weren't being used.
            this.writable = true;
            StreamExtensions.CopyToStream(inner, this);
            this.writable = false;

            this.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Disposes the seekable stream wrapper, and the wrapped stream if
        /// originally requested.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.disposeInner)
                {
                    this.inner.Dispose();
                }

                this.inner = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets whether the stream wrapper can be written to or not.
        /// This is only <c>true</c> while the wrapper is being initialized, and
        /// otherwise <c>false</c> for all other callers.
        /// </summary>
        public override bool CanWrite { get { return this.writable; } }
    }
}
