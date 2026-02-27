using System;
using System.Linq;
using Xunit;
using ImageResizer.Plugins.Security;

namespace ImageResizer.Plugins.Security.Tests {
    public class SimpleSecureEncryptionTests {

        [Fact]
        public void EncryptDecrypt_RoundTrip_SmallData() {
            var enc = new SimpleSecureEncryption("test-password-32-characters-long!");
            byte[] original = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_EmptyData() {
            var enc = new SimpleSecureEncryption("test-password-32-characters-long!");
            byte[] original = new byte[0];
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_LargerData() {
            var enc = new SimpleSecureEncryption("test-password-32-characters-long!");
            byte[] original = new byte[1024];
            new Random(42).NextBytes(original);
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_ByteArrayKey() {
            byte[] key = System.Text.Encoding.UTF8.GetBytes("another-32-char-key-for-testing!");
            var enc = new SimpleSecureEncryption(key);
            byte[] original = System.Text.Encoding.UTF8.GetBytes("Secret data");
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_ProducesNonEmptyIV() {
            var enc = new SimpleSecureEncryption("test-password");
            byte[] iv;
            enc.Encrypt(new byte[] { 1, 2, 3 }, out iv);
            Assert.NotNull(iv);
            Assert.Equal(16, iv.Length);
        }

        [Fact]
        public void Encrypt_ProducesDifferentIV_EachTime() {
            var enc = new SimpleSecureEncryption("test-password");
            byte[] data = System.Text.Encoding.UTF8.GetBytes("same data");
            byte[] iv1, iv2;
            enc.Encrypt(data, out iv1);
            enc.Encrypt(data, out iv2);
            Assert.False(iv1.SequenceEqual(iv2), "IVs should differ between encryptions");
        }

        [Fact]
        public void Encrypt_ProducesDifferentCiphertext_EachTime() {
            var enc = new SimpleSecureEncryption("test-password");
            byte[] data = System.Text.Encoding.UTF8.GetBytes("same data");
            byte[] iv1, iv2;
            byte[] ct1 = enc.Encrypt(data, out iv1);
            byte[] ct2 = enc.Encrypt(data, out iv2);
            Assert.False(ct1.SequenceEqual(ct2), "Ciphertext should differ due to different IVs");
        }

        [Fact]
        public void Decrypt_WithWrongIV_ProducesDifferentResult() {
            var enc = new SimpleSecureEncryption("test-password");
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello");
            byte[] iv;
            byte[] encrypted = enc.Encrypt(data, out iv);
            // Flip a bit in the IV
            byte[] badIv = (byte[])iv.Clone();
            badIv[0] ^= 0xFF;
            byte[] decrypted = enc.Decrypt(encrypted, badIv);
            Assert.False(data.SequenceEqual(decrypted), "Decryption with wrong IV should produce wrong result");
        }

        [Fact]
        public void Decrypt_WithInvalidIVLength_Throws() {
            var enc = new SimpleSecureEncryption("test-password");
            byte[] data = new byte[] { 1, 2, 3 };
            byte[] shortIv = new byte[] { 1, 2, 3, 4 }; // Should be 16 bytes
            Assert.Throws<ArgumentOutOfRangeException>(() => enc.Decrypt(data, shortIv));
        }

        [Fact]
        public void Decrypt_WithDifferentKey_Fails() {
            var enc1 = new SimpleSecureEncryption("key-one");
            var enc2 = new SimpleSecureEncryption("key-two");
            byte[] data = System.Text.Encoding.UTF8.GetBytes("secret");
            byte[] iv;
            byte[] encrypted = enc1.Encrypt(data, out iv);
            // Decrypting with different key should throw (bad padding) or produce garbage
            Assert.ThrowsAny<Exception>(() => enc2.Decrypt(encrypted, iv));
        }

        [Fact]
        public void SameKey_ProducesSameDecryption() {
            var enc1 = new SimpleSecureEncryption("same-key");
            var enc2 = new SimpleSecureEncryption("same-key");
            byte[] data = System.Text.Encoding.UTF8.GetBytes("consistent");
            byte[] iv;
            byte[] encrypted = enc1.Encrypt(data, out iv);
            byte[] decrypted = enc2.Decrypt(encrypted, iv);
            Assert.Equal(data, decrypted);
        }

        [Fact]
        public void KeySizeInBytes_Is32() {
            var enc = new SimpleSecureEncryption("any-key");
            Assert.Equal(32, enc.KeySizeInBytes);
        }

        [Fact]
        public void BlockSizeInBytes_Is16() {
            var enc = new SimpleSecureEncryption("any-key");
            Assert.Equal(16, enc.BlockSizeInBytes);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_ExactBlockSize() {
            var enc = new SimpleSecureEncryption("test-key");
            // 16 bytes = exactly one block
            byte[] original = System.Text.Encoding.UTF8.GetBytes("0123456789ABCDEF");
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_MultipleBlocks() {
            var enc = new SimpleSecureEncryption("test-key");
            // 48 bytes = exactly 3 blocks
            byte[] original = new byte[48];
            for (int i = 0; i < 48; i++) original[i] = (byte)(i % 256);
            byte[] iv;
            byte[] encrypted = enc.Encrypt(original, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_UTF8String() {
            var enc = new SimpleSecureEncryption("unicode-key");
            string original = "Hello \u00e9\u00e8\u00ea \u4e16\u754c";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(original);
            byte[] iv;
            byte[] encrypted = enc.Encrypt(data, out iv);
            byte[] decrypted = enc.Decrypt(encrypted, iv);
            Assert.Equal(original, System.Text.Encoding.UTF8.GetString(decrypted));
        }
    }
}
