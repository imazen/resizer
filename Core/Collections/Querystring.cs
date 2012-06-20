using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Collections {
    public class QuerystringBase<TK>: NameValueCollection where TK:QuerystringBase<TK>
    {
         public QuerystringBase():base()
         {
         }

        public QuerystringBase(NameValueCollection q):base(q)
    {
    }


        public T? Get<T>(string name) where T : struct, IConvertible {
            return this.Get<T>(name, null);
        }

        public T? Get<T>(string name, T? defaultValue) where T : struct, IConvertible {
            return NameValueCollectionExtensions.ParsePrimitive<T>(this[name], defaultValue);
        }

        public T Get<T>(string name, T defaultValue) where T : struct, IConvertible {
            return NameValueCollectionExtensions.ParsePrimitive<T>(this[name], defaultValue).Value;
        }

        public TK SetAsString<T>(string name, T val) where T : class
        {
            return (TK) NameValueCollectionExtensions.SetAsString(this, name, val);
        }

        /// <summary>
        /// Provides culture-invariant serialization of value types, in lower case for querystring readability. Setting a key to null removes it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public TK Set<T>(string name, T? val) where T : struct, IConvertible
        {
            return (TK) NameValueCollectionExtensions.Set(this,name, val);
        }


        /// <summary>
        /// Returns true if any of the specified keys contain a value
        /// </summary>
        /// <param name="q"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public  bool IsOneSpecified( params string[] keys)
        {
            return NameValueCollectionExtensions.IsOneSpecified(this, keys);
        }



        /// <summary>
        /// Normalizes a command that has two possible names. 
        /// If either of the commands has a null or empty value, those keys are removed. 
        /// If both the the primary and secondary are present, the secondary is removed. 
        /// Otherwise, the secondary is renamed to the primary name.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public TK Normalize(string primary, string secondary)
        {
            return (TK) NameValueCollectionExtensions.Normalize(this, primary, secondary);
        }

        public T[] GetList<T>(string name, T? fallbackValue, params int[] allowedSizes) where T : struct, IConvertible
        {
            return NameValueCollectionExtensions.GetList(this, name, fallbackValue, allowedSizes);
        }

        public TK SetList<T>(string name, T[] values, bool throwExceptions, params int[] allowedSizes) where T : struct, IConvertible
        {
            return (TK) NameValueCollectionExtensions.SetList(this, name, values, throwExceptions, allowedSizes);
        }

    }
}
