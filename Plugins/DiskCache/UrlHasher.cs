/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Globalization;

namespace ImageResizer.Plugins.DiskCache {
    [Obsolete("This class has been moved to the DiskCache plugin, as it is only used there. ")]
    public class UrlHasher {
        public UrlHasher() {
        }

        /// <summary>
        /// Builds a key for the cached version, using the hashcode of the normalized URL.
        /// if subfolders > 1, dirSeparator will be used to separate the subfolder and the key. 
        /// No extension is appended.
        /// I.e, a13514\124211ab132592 or 12412ababc12141
        /// </summary>
        /// <param name="data"></param>
        /// <param name="subfolders"></param>
        /// <param name="dirSeparator"></param>
        /// <returns></returns>
        public string hash(string data, int subfolders, string dirSeparator) {
            
            SHA256 h = System.Security.Cryptography.SHA256.Create();
            byte[] hash = h.ComputeHash(new System.Text.UTF8Encoding().GetBytes(data));

            //If configured, place files in subfolders.
            string subfolder = "";
            if (subfolders > 1) {
                subfolder = getSubfolder(hash, subfolders) + dirSeparator;
            }
            
            //Can't use base64 hash... filesystem has case-insensitive lookup.
            //Would use base32, but too much code to bloat the resizer. Simple base16 encoding is fine
            return subfolder + Base16Encode(hash);
        }
        /// <summary>
        /// Returns a string for the subfolder name. The bits used are from the end of the hash - this should make
        /// the hashes in each directory more unique, and speed up performance (8.3 filename calculations are slow when lots of files share the same first 6 chars.
        /// Returns null if not configured. Rounds subfolders up to the nearest power of two.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="subfolders"></param>
        /// <returns></returns>
        protected string getSubfolder(byte[] hash, int subfolders) {
            int bits = (int)Math.Ceiling(Math.Log(subfolders, 2)); //Log2 to find the number of bits. round up.
            Debug.Assert(bits > 0);
            Debug.Assert(bits <= hash.Length * 8);

            byte[] subfolder = new byte[(int)Math.Ceiling((double)bits / 8.0)]; //Round up to bytes.
            Array.Copy(hash, hash.Length - subfolder.Length, subfolder, 0, subfolder.Length);
            subfolder[0] = (byte)((int)subfolder[0] >> ((subfolder.Length * 8) - bits)); //Set extra bits to 0.
            return Base16Encode(subfolder);
            
        }


        protected string Base16Encode(byte[] bytes) {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x", NumberFormatInfo.InvariantInfo).PadLeft(2, '0'));
            return sb.ToString();
        }
    }
}
