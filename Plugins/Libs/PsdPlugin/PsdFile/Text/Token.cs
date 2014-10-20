using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace PhotoshopFile.Text
{
    public class TdTaParseException : Exception
    {
            public TdTaParseException(string message):base(message){}
            public TdTaParseException(string message, BinaryReverseReader r)
                : base(message + "\nMore data:" + new string(r.ReadChars((int)Math.Max(100, r.BytesToEnd))))
            {
               
            }
            public TdTaParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// For parsing the TdTa format
    /// </summary>
    public class Token
    {
        public enum TokenType
        {
            Binary,
            StartList, 
            EndList, 
            StartDict,
            EndDict,
            MapKey,
            Whitespace,
            Integer,
            Double,
            Boolean
        }
        public object value;
        public TokenType type;
        /// <summary>
        /// True if this token represents a single value (bool, int, double, binary), 
        /// </summary>
        public bool IsValueType
        {
            get
            {
                return type == TokenType.Boolean || type == TokenType.Binary || type == TokenType.Double || type == TokenType.Integer;
            }
        }

        public Token() { }
        public Token(byte[] binary)
        {
            type = TokenType.Binary;
            value = binary;
        }
        public Token(TokenType type, object value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            if (value is int) return "(" + type.ToString() + "):" + ((int)value).ToString(NumberFormatInfo.InvariantInfo);
            if (value is double) return "(" + type.ToString() + "):" + ((double)value).ToString(NumberFormatInfo.InvariantInfo);
            else return "(" + type.ToString() + "):" + value.ToString();
        }

        /// <summary>
        /// Reads bytes until a unescaped closing parenthesis in found. Unescapes escaped parenthesis.
        /// Leaves reader on the char following the closing parenthesis.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static byte[] parseBinary(BinaryReverseReader r)
        {
            MemoryStream ms = new MemoryStream(128); //For writing the decoded binary
            byte b = 0;
            byte lastb = 0;
            bool hasLastByte = false;
            //Read bytes, keeping a two-byte shifting buffer so we can decode escaped chars.
            while (r.CanReadByte())
            {
                b = r.ReadByte();

                //Look for closing parenthesis
                if ((char)b == ')')
                {
                    if (hasLastByte && (char)lastb == '\\')
                    {
                        //Escaped parenthesis, skip slash
                        lastb = 0;
                        hasLastByte = false;
                    }
                    else
                    {
                        //Unescaped closing parenthesis! We hit the end!
                        if (hasLastByte) ms.WriteByte(lastb); //If we still have a byte in the buffer.
                        return ms.ToArray();//We hit the end, an unescaped closng parenthesis.
                    }
                }

                //Far as I know, nothing else is escaped.

                //Write lastb if present.
                if (hasLastByte) ms.WriteByte(lastb);
                //Shift buffer
                lastb = b;
                hasLastByte = true;
            }
            throw new TdTaParseException("Hit end of stream without finding closing parenthesis!");
        }

        public const NumberStyles floatingPointStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

        /// <summary>
        /// Parses tdta contents into tokens. Throws a generic exception if unrecognized tokens are encountered
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Token nextToken(BinaryReverseReader r)
        {
            if (!r.CanReadByte()) return null;

           char c = r.ReadChar();

            switch (c)
            {
                case '(': return new Token(parseBinary(r)); //Binary data. Leading and trailing parens are stripped, and escaped parens are restored to normal.
                case '[': return new Token(TokenType.StartList, c.ToString()); // [ opening list char
                case ']': return new Token(TokenType.EndList, c.ToString()); //Closing list char
            }
            //<< opening dict
            //>> closing dict
            if (c == '<' || c == '>')
            {
                char c2 = r.ReadChar();
                if (c2 != c) throw new TdTaParseException("Unrecognized token: "  + c + c2,r);

                if (c == '<') return new Token(TokenType.StartDict, "<<");
                else return new Token(TokenType.EndDict, ">>");
            }
            //Dict key (Any non-whitespace, non-[]<> sequence allowed following forward slash)
            if (c == '/'){
                StringBuilder sb = new StringBuilder(40);
                while (r.CanReadByte()){
                    //Peek at the next char to see if it is whitespace
                    char pc = (char)r.PeekChar();
                    if (Char.IsWhiteSpace(pc) || pc == '\n' || pc=='[' || pc==']' || pc=='<' || pc=='>'){
                        //We're done.
                        //Strip the leading slash, create token with name
                        return new Token(TokenType.MapKey,sb.ToString());
                    }else{
                        //A valid char, append it.
                        sb.Append(r.ReadChar());
                    }
                }
            }
            //Integer or double. Ends at whitespace, newline, ], or >
            if (Char.IsDigit(c) || c=='-' || c == '.'){
                StringBuilder sb = new StringBuilder(40);
                sb.Append(c);
                while (r.CanReadByte()){
                    //Peek at the next char to see if it is whitespace
                    char pc = (char)r.PeekChar();
                    if (Char.IsWhiteSpace(pc) || pc == '\n' || pc ==']' || pc =='>'){
                        //We're done. Find out what kind of number it is.
                        string s = sb.ToString();
                        //If it has a decimal, it's a double
                        if (s.Contains('.')){
                            return new Token(TokenType.Double,double.Parse(s, floatingPointStyle,NumberFormatInfo.InvariantInfo));
                        }else{ 
                            return new Token(TokenType.Integer,int.Parse(s,NumberStyles.Integer,NumberFormatInfo.InvariantInfo));
                        }
                        
                    }else if (Char.IsDigit(c) || c=='-' || c == '.'){
                        //A valid char, append it.
                        sb.Append(r.ReadChar());
                    }else{
                        //Unrecognized char in numeric sq
                        throw new TdTaParseException("Unrecognized character in numeric sequence: " + sb.ToString() + pc,r);
                    }
                }
            }
            //Boolean (true|false) parsing
            if (c == 't' || c == 'T' || c == 'f' || c == 'F'){
                string s = c.ToString() + r.ReadChar().ToString() + r.ReadChar().ToString() + r.ReadChar().ToString();
                char pc = (char)r.PeekChar();
                if (s.Equals("true", StringComparison.OrdinalIgnoreCase)){
                    return new Token(TokenType.Boolean,true);
                }else if (s.Equals("fals", StringComparison.OrdinalIgnoreCase) && (pc == 'e' || pc == 'E')){
                    r.ReadChar(); //discard 'e'
                    return new Token(TokenType.Boolean,false);
                }else{
                    throw new TdTaParseException("Unrecognized boolean value: " + s + pc,r);
                }
            }
            //Whitespace parsing
            if (Char.IsWhiteSpace(c) || c == '\n'){
                StringBuilder sb = new StringBuilder(40);
                sb.Append(c);
                while (r.CanReadByte()){
                    //Peek at the next char to see if it is whitespace
                    char pc = (char)r.PeekChar();
                    if (Char.IsWhiteSpace(pc) || pc == '\n'){
                        sb.Append(r.ReadChar());
                    }else{
                        //We done.
                        break;
                    }
                }
                return new Token(TokenType.Whitespace,sb.ToString());
            }
            throw new TdTaParseException("Unrecognized token: " + c,r);
        }
    }
}
