﻿using System.Collections.Specialized;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.AzureReader2;
using Xunit;

namespace ImageResizer.AllPlugins.Tests {
    public class AzureReaderTest {
        public AzureReaderTest() {
        }

        [Fact]
        public void NameValueConstructor() {
            // Arrange
            var settings = this.Settings;

            // Act
            var target = new AzureReader2Plugin(settings);

            // Assert
            Assert.NotNull(target);
            Assert.IsType<AzureReader2Plugin>(target);
        }

        /// <summary>
        /// Call the FileExists method with a null value for the queryString parameter.
        /// </summary>
        /// <remarks>
        /// The queryString parameter is not used. The value passed should not affect the method outcome.
        /// </remarks>
        [Fact]
        public void FileExistsWithNullQueryString() {
            // Arrange
            bool expected = true;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");
            IVirtualImageProvider target = (IVirtualImageProvider)new AzureReader2Plugin(settings);

            // Act
            bool actual = target.FileExists(virtualPath, new NameValueCollection());

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        [Fact]
        public void FileExistsStringId() {
            // Arrange
            // Nathan comments
            Config c = new Config();
            var y = new AzureReader2Plugin(this.Settings);
            y.Install(c);
            var x = c.Plugins.Get<AzureVirtualPathProvider>();
            //c.Plugins.VirtualProviderPlugins
            c.Pipeline.GetFile("x", null);
            bool expected = true;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");

            // Act
            IVirtualImageProvider target = (IVirtualImageProvider)new AzureReader2Plugin(settings);
            bool actual = c.Pipeline.FileExists("x", null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                settings["connectionstring"] = "";
                settings["blobstorageendpoint"] = "";
                settings["endpoint"] = "";
                settings["prefix"] = "~/azure";
                settings["lazyExistenceCheck"] = "false";
                settings["vpp"] = "";

                return settings;
            }
        }
    }
}