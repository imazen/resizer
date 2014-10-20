using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;
using System.IO;

namespace ImageResizer.Plugins.Security {
    /// <summary>
    /// Provides correct 256-bit AES encryption and decryption for small data sets. 
    /// </summary>
    public class SimpleSecureEncryption {

        /// <summary>
        /// Creates an encryption/decryption system using a 256-bit key derived from the specified byte sequence. 32-bit or longer phrases are suggested.
        /// </summary>
        /// <param name="keyBasis">A password or key</param>
        public SimpleSecureEncryption(byte[] keyBasis) {
            this.keyBasis = keyBasis;
        }
        /// <summary>
        /// Creates an encryption/decryption system using a 256-bit key derived from the specified pass phrase. 32-bit or longer phrases are suggested.
        /// </summary>
        /// <param name="passPhrase"></param>
        public SimpleSecureEncryption(string passPhrase) {
            this.keyBasis = UTF8Encoding.UTF8.GetBytes(passPhrase);
        }

        private byte[] keyBasis;
        /// <summary>
        /// The fixed 16-byte salt used for key derivation. This has to be fixed and unchanging to permit consistent key derivation across servers
        /// </summary>
        private byte[] salt = new byte[] { 240, 193, 22, 63, 44, 83, 186, 251, 74, 193, 241, 209, 220, 199, 37, 76 };

        private int _keyBytes = 32;
        /// <summary>
        /// The number of bytes in the key - defaults to 32 (256-bit AES)
        /// </summary>
        public int KeySizeInBytes { get { return _keyBytes; } }

        private int _blockSizeInBytes = 16;
        /// <summary>
        /// The number of bytes in a single block. Also the size of the IV (Initialization Vector). 
        /// 
        /// </summary>
        public int BlockSizeInBytes { get { return _blockSizeInBytes; } }

        private byte[] key;

        private byte[] GetKey() {
            if (key != null) return key;
            var derive = new Rfc2898DeriveBytes(keyBasis, salt, 1000);
            key = derive.GetBytes(KeySizeInBytes);
            return key;
        }

        private Rijndael GetAlgorithm() {
            var rm = new RijndaelManaged();
            rm.KeySize = KeySizeInBytes * 8;
            rm.Mode = CipherMode.CBC;
            rm.Padding = PaddingMode.PKCS7;
            rm.BlockSize = BlockSizeInBytes * 8;

            //Feedback size not needed - CBC uses entire block

            return rm;
        }
        /// <summary>
        /// Decrypts the specified data using a derived key and the specified IV
        /// </summary>
        /// <param name="data"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] data, byte[] iv) {
            if (iv.Length != BlockSizeInBytes) throw new ArgumentOutOfRangeException("The specified IV is invalid - an " + BlockSizeInBytes + " byte array is required.");

            var rm = GetAlgorithm();
            try {
                using (var decrypt = rm.CreateDecryptor(GetKey(), iv))
                using (var ms = new MemoryStream(data, 0, data.Length, false, true))
                using (var s = new CryptoStream(ms, decrypt, CryptoStreamMode.Read)) {
                    return s.CopyToBytes();

                }
            } finally {
                rm.Clear();
            }

        }

        /// <summary>
        /// Encrypts the specified data using a derived key and a generated IV.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data, out byte[] iv) {
            var rm = GetAlgorithm();
            try {
                rm.GenerateIV();
                iv = rm.IV;
                rm.Key = GetKey();
                using (var encrypt = rm.CreateEncryptor())
                using (var ms = new MemoryStream(data.Length / BlockSizeInBytes)) {
                    using (var s = new CryptoStream(ms, encrypt, CryptoStreamMode.Write)) {
                        s.Write(data, 0, data.Length);
                        s.Flush();
                        s.FlushFinalBlock();
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms.CopyToBytes();
                    }
                }
            } finally {
                rm.Clear();
            }
        }



    }
}
