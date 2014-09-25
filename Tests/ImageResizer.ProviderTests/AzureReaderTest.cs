using System;
using System.Collections.Specialized;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.AzureReader2;
using Xunit;

namespace ImageResizer.ProviderTests {
    public class AzureReaderTest {
        public AzureReaderTest() {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            // In unit tests the DataDirecry path used by connection strings is
            // null. We set the path here to ensure that connection strings 
            // that use DataDirectory function as expected.
            AppDomain.CurrentDomain.SetData(
                ".appPath",
               AppDomain.CurrentDomain.BaseDirectory);
            AppDomain.CurrentDomain.SetData(".appVPath", "/");
            AppDomain.CurrentDomain.CreateInstanceAndUnwrap(typeof(HostingEnvironment).Assembly.FullName, typeof(HostingEnvironment).FullName) as HostingEnvironment;
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
            bool expected = true;
            var rs = new ResizerSection("<resizer><plugins><add name=\"AzureReader2\" connectionString=\"UseDevelopmentStorage=true\" endpoint=\"http://127.0.0.1:10000/devstoreaccount1/\" /></plugins></resizer>");
            var c = new Config(rs);
            IVirtualImageProvider target = (IVirtualImageProvider)c.Plugins.VirtualProviderPlugins.First;
            var settings = this.Settings;
            string virtualPath = Path.Combine(settings["prefix"], "{89A5100C-48F2-4024-AF9E-6AE662F720A2}");

            // Act
            bool actual = target.FileExists("x", null);

            // Assert
            Assert.StrictEqual<bool>(expected, actual);
        }

        private NameValueCollection Settings {
            get {
                var settings = new NameValueCollection();
                settings["connectionstring"] = "";
                settings["blobstorageendpoint"] = "http://127.0.0.1:10000/devstoreaccount1/";
                settings["endpoint"] = "";
                settings["prefix"] = "~/azure";
                settings["lazyExistenceCheck"] = "false";
                settings["vpp"] = "false";

                return settings;
            }
        }
    }
}