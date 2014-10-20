 /* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.S3Reader2 {
    /// <summary>
    /// Class to allow modifying the bucket and key request path
    /// </summary>
    public class S3PathEventArgs : EventArgs {
  
        /// <summary>
        /// Create a new instance of S3PathEventArgs for modifying the bucket and key of incoming requests
        /// </summary>
        /// <param name="path"></param>
        public S3PathEventArgs(string path) {
            this.Path = path;
        }
        /// <summary>
        /// Path contains the bucket and key in the form "bucket/key".
        /// Where key may contain additional forward slashes.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Stops the request if it doesn't match one of the allowed buckets.
        /// Amazon S3 is case-sensitive. Thus, comparisons are case sensitive as well. To minimize headache, a different exception is thrown when only case differs.
        /// </summary>
        /// <param name="buckets"></param>
        public void AssertBucketMatches(params string[] buckets) {
            bool found = false;
            string b = Bucket;
            //Look for exact match
            foreach (string s in buckets) if (s.Equals(b,StringComparison.InvariantCulture)) { found = true; break; }
            //If no exact matches, but a case-insensitive match exists, tell the user
            if (!found) foreach (string s in buckets) if (s.Equals(b, StringComparison.InvariantCultureIgnoreCase)) { ThrowInvalidCaseException(); }
            //Throw exception if needed
            if (!found) ThrowException();
        }

        /// <summary>
        /// Throws an HTTP 403 Not Authorized exception. Call this if an invalid bucket request comes through.
        /// </summary>
        public void ThrowException() {
            throw new ImageProcessingException(403, "You have not permitted access to this amazon S3 bucket.");
        }

        /// <summary>
        /// Like ThrowException, but hints that the bucket casing is wrong.
        /// </summary>
        public void ThrowInvalidCaseException() {
            throw new FileNotFoundException("Amazon S3 is case sensitive. Check your requested bucket name to verify correct casing.");
        }

        /// <summary>
        /// Prefixes the specified bucket to the requested path.
        /// </summary>
        /// <param name="bucket"></param>
        public void PrefixBucket(String bucket) {
            bucket = bucket.Trim('\\', '/');
            Path = bucket + "/" + Path.TrimStart('/');
        }
        /// <summary>
        /// Parses the bucket from Path
        /// </summary>
        /// <returns></returns>
        public string Bucket {
            get {
                //strip leading slashes
                string path = Path.TrimStart(new char[] { '/', '\\' });

                int keyStartsAt = path.IndexOf('/');
                if (keyStartsAt < 0) return path; //No key present
                //Get bucket
                return path.Substring(0, keyStartsAt);
            }
        }
        /// <summary>
        /// Parses the key from Path
        /// </summary>
        /// <returns></returns>
        public string Key {
            get {
                //strip leading slashes
                string path = Path.TrimStart(new char[] { '/', '\\' });

                int keyStartsAt = path.IndexOf('/');
                if (keyStartsAt < 0) return null; //no key

                //Get key
                return path.Substring(keyStartsAt + 1).TrimStart(new char[] { '/', '\\' });
            }
        }
    }

}
