/*Copyright (c) 2012 Ben Foster

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ImageResizer.Plugins.AdditionalFileExtensions
{
    /// <summary>
    /// A plugin for registering additional file extensions to be handled by ImageResizer
    /// </summary>
    /// <example>
    /// <add name="AdditionalFileExtensions" fileExtensions="ext1,ext2,ext3" />
    /// </example>
    public class AdditionalFileExtensionsPlugin : IPlugin, IFileExtensionPlugin
    {
        private const string FileExtensionsKey = "fileExtensions";
        private readonly IEnumerable<string> fileExtensions;

        public AdditionalFileExtensionsPlugin(NameValueCollection configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            fileExtensions = ParseFileExtensions(configuration);
        }

        public IEnumerable<string> GetSupportedFileExtensions()
        {
            return fileExtensions;
        }

        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        private static IEnumerable<string> ParseFileExtensions(NameValueCollection collection)
        {
            var fileExtensionsString = collection.Get(FileExtensionsKey);

            if (string.IsNullOrEmpty(fileExtensionsString))
                return Enumerable.Empty<string>();

            return fileExtensionsString.Split(',', ';', '|');
        }
    }
}
