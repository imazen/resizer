using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.SeamCarving {
    /// <summary>
    /// A quirky version of LZW supporting only 32K dictionary sizes, expecting a UTF8-like encoding method, and offering custom alphabet support
    /// </summary>
    public class LzwDecoder {

        private string alphabet;
        public LzwDecoder(){
        }

        public LzwDecoder(string customAlphabet){
            this.alphabet = customAlphabet;
        }

        public string Decode(byte[] data) {
            var dict = new Dictionary<int, string>();

            var currChar = alphabet != null ? alphabet[data[0] -1] : data[0] -1;
            var oldPhrase = Char.ConvertFromUtf32(currChar);
            var sb = new StringBuilder();
            sb.Append(Char.ConvertFromUtf32(currChar));
            var asis = alphabet != null ? alphabet.Length : 256;
            var code = asis;
            string phrase;
            // debugger;
            for (var i = 1; i < data.Length; i++) {
                var currCode = (int)data[i];
                if (currCode >= 128 && i < data.Length - 1) {
                    currCode &= 0xEF; //Drop leading bit
                    currCode = currCode << 8; //Shift to MSB
                    currCode |= (int)data[i + 1]; //Combine with LSB.
                    i++; //Skip forward so we don't read duplicate.
                }
                currCode--; //Since we offset everthing +1 to avoid char 0

                if (currCode < asis) {
                    phrase = Char.ConvertFromUtf32(alphabet != null ? alphabet[currCode] : currCode);
                } else {
                    if (!dict.TryGetValue(currCode, out phrase))
                        phrase = oldPhrase + Char.ConvertFromUtf32(currChar);
                }
                sb.Append(phrase);
                currChar = phrase[0];
                dict[code] = oldPhrase + phrase[0];
                code++;
                oldPhrase = phrase;
            }
            return sb.ToString();
        }
    }
}
