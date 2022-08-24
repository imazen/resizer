// Copyright (c) 2012 Jason Morse
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace ImageResizer.Plugins.PdfRenderer.Ghostscript
{
    /// <summary>
    ///   Ghostscript settings collection.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [Serializable]
    public class GhostscriptSettings : NameValueCollection
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "GhostscriptSettings" /> class that is empty, has the default initial 
        ///   capacity and uses the default case-insensitive hash code provider and the default case-insensitive comparer.
        /// </summary>
        public GhostscriptSettings()
        {
        }

        /// <summary>
        ///   Copies the entries from the specified <see cref = "GhostscriptSettings" /> to a new <see cref = "GhostscriptSettings" /> 
        ///   with the same initial capacity as the number of entries copied and using the same hash code provider and the same comparer 
        ///   as the source collection.
        /// </summary>
        /// <param name = "items">The <see cref = "GhostscriptSettings" /> to copy to the new <see cref = "GhostscriptSettings" /> 
        ///   instance.</param>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "items" /> is null.</exception>
        public GhostscriptSettings(NameValueCollection items)
            : base(items)
        {
        }

        protected GhostscriptSettings(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Adds an entry with the specified name and string representation of the value to the <see cref="GhostscriptSettings" />.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string" /> key of the entry to add. The key can be null.
        /// </param>
        public void Add(string name)
        {
            base.Add(name, null);
        }

        /// <summary>
        /// Adds an entry with the specified name and string representation of the value to the <see cref="GhostscriptSettings" />.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string" /> key of the entry to add. The key can be null.
        /// </param>
        /// <param name="value">
        /// The string representation of <see cref="object" /> value of the entry to add. The value can be null.
        /// </param>
        public void Add<T>(string name, T value)
        {
            string textValue = Convert.ToString(value, CultureInfo.InvariantCulture);
            base.Add(name, textValue);
        }
    }
}