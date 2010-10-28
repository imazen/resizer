using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoshopFile.Text
{
    public class TdTaParser
    {
        BinaryReverseReader r;
        public TdTaParser(BinaryReverseReader r)
        {
            this.r = r;
        }
        /// <summary>
        /// Parses one tree from the stream. If there are multiple root nodes, you may have to call this multiple times, but you will get an exception if the stream is empty.
        /// </summary>
        /// <returns></returns>
        public object ParseOneTree()
        {
            return parse();
        }

        protected Token ReadToken()
        {
            return Token.nextToken(r);
        }
        protected Token ReadTokenIgnoreWhitespace()
        {
            Token t = ReadToken();
            //Fast forward through whitespace tokens
            while (t.type == Token.TokenType.Whitespace) { t = ReadToken(); }
            return t;
        }
        /// <summary>
        /// Can start parsing anywhere, as long as it isn't before a map key.
        /// </summary>
        /// <returns></returns>
        protected object parse()
        {
            //Ignore whitespace
            Token t = ReadTokenIgnoreWhitespace();
            //Return the primitive value types
            if (t.IsValueType) return t.value;
            if (t.type == Token.TokenType.StartList) return parseList();
            if (t.type == Token.TokenType.StartDict) return parseDict();
            if (t.type == Token.TokenType.EndList || t.type == Token.TokenType.EndDict) return t;
            throw new TdTaParseException("Unexpected token " + t);
        }
        /// <summary>
        /// Parses a list of objects from the stream. Assumes opening token was alread read.
        /// </summary>
        /// <returns></returns>
        protected List<object> parseList()
        {
            List<object> list = new List<object>();

            while (true)
            {
                object o = parse(); //Parse an object
                if (o is Token)
                {
                    //Must be end of list token, any other is an error
                    if (((Token)o).type != Token.TokenType.EndList)
                        throw new TdTaParseException("Unexpected token " + (Token)o + " in list");
                    //end of list
                    return list;
                }
                else
                {
                    list.Add(o);
                }
            }
        }
        /// <summary>
        /// Parses a dictionary (map,object) from the stream. Assumes opening token was alread read.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string,object> parseDict()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            while (true)
            {
                //parse key
                //Ignore whitespace
                Token key = ReadTokenIgnoreWhitespace();

                //Check for end of dict
                if (key.type == Token.TokenType.EndDict) return dict;
                
                //Check for invalid token
                if (key.type != Token.TokenType.MapKey)
                    throw new TdTaParseException("Unexpected token " + key + " in dictionary, expected a dictionary key like /key");

                //Parse the value (as an object) (can be another dict, will handle itself)
                object o = parse(); 

                //Shouldn't be a token
                if (o is Token) throw new TdTaParseException("Unexpected token " + (Token)o + " in dict! Expected value.");
                
                //Add pair
                dict.Add((string)key.value, o);
            }
        }
    }
}
