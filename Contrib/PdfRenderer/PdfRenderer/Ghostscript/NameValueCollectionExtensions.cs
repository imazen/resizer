﻿// Copyright (c) 2012 Jason Morse
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Specialized;
using System.Globalization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    /// Ghostcript extensions that aid in getting data from the given collection
    /// </summary>
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Get page or bit data from the given NameValueCollection
        /// </summary>
        /// <param name="collection">collection that contains the data</param>
        /// <param name="name">Name of Item requested</param>
        /// <param name="defaultValue">default value to return if item can not be parsed</param>
        /// <returns>value of given item in collection</returns>
        public static int GetValueOrDefault(this NameValueCollection collection, string name, int defaultValue)
        {
            if(collection != null)
            {
                if(!string.IsNullOrEmpty(collection[name]))
                {
                    int value;
                    if(int.TryParse(collection[name],NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value))
                    {
                        return value;
                    }
                }
            }
            return defaultValue;
        }
    }
}