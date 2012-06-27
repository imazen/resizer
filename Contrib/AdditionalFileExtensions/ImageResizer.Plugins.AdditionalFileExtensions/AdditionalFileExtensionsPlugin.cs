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
