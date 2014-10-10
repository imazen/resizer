using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Plugins.TinyCache;

using Xunit;

namespace ImageResizer.ProviderTests
{
    /// <summary>
    /// Test the functionality of the <see cref="TinyCachePlugin"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests exercise the methods from <see cref="ICache"/> as
    /// implemented by <see cref="TinyCachePlugin"/>. Also The method 
    /// implementations of <see cref="IPlugin"/>.
    /// </para>
    /// </remarks>
    public class TinyCacheTests
    {
        private const string Filename = "rose-leaf.jpg";

        private const string ConfigXml = "<resizer><plugins><add name=\"TinyCache\" /></plugins></resizer>";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureReaderTest"/> class.
        /// </summary>
        public TinyCacheTests()
        {
            HttpContext.Current = new HttpContext(
           new HttpRequest(string.Empty, "http://tempuri.org", string.Empty),
           new HttpResponse(new StringWriter(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Instantiate a new  <see cref="TinyCachePlugin"/> object and test for success.
        /// </summary>
        [Fact]
        public void SimpleConstructorTest()
        {
            // Arrange

            // Act
            var target = new TinyCachePlugin();

            // Assert
            Assert.NotNull(target);
            Assert.IsType<TinyCachePlugin>(target);
        }

        /// <summary>
        /// Test <see cref="TinyCachePlugin"/> install capabilities.
        /// This plugin can not be installed more than once.
        /// </summary>
        [Fact]
        public void InstallTwiceTest()
        {
            // Arrange
            var c = new Config();
            var target = new TinyCachePlugin();

            // Act
            var dummy = target.Install(c);
            var actual = target.Install(c);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<TinyCachePlugin>(actual);
        }

        /// <summary>
        /// Test <see cref="TinyCachePlugin"/> install capabilities.
        /// Can only be installed more than once.
        /// </summary>
        [Fact]
        public void InstallTwiceUninstallOnceTest()
        {
            // Arrange
            var c = new Config();
            var target = new TinyCachePlugin();

            // Act
            var dummy = target.Install(c);
            var wasUninstalled = target.Uninstall(c);
            var actual = target.Install(c);

            // Assert
            Assert.True(wasUninstalled);
            Assert.NotNull(actual);
            Assert.IsType<TinyCachePlugin>(actual);
        }

        /// <summary>
        /// Test <see cref="TinyCachePlugin"/> constructor and install capabilities.
        /// Should not uninstall if not installed. Failure is silent.
        /// </summary>
        [Fact]
        public void UninstallWithoutInstallingTest()
        {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var target = new TinyCachePlugin();

            // Act
            bool actual = target.Uninstall(c);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void CanProcessAlwaysWithNullContext()
        {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            ICache target = (ICache)c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Always;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;

            // Act
            bool actual = target.CanProcess(null, args);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void CanProcessNoWithNullContext()
        {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            ICache target = (ICache)c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.No;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;

            // Act
            bool actual = target.CanProcess(null, args);

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void CanProcessNoWithNullArgs()
        {
            // Arrange
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            ICache target = (ICache)c.Plugins.Get<TinyCachePlugin>();


            // Act
            var actual = Assert.Throws<ArgumentNullException>(() => target.CanProcess(null, null));

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ArgumentNullException>(actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void ReadAndWriteVirtualCacheFile()
        {
            // Arrange
            string expected = "/folder/name.txt";
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            TinyCachePlugin target = c.Plugins.Get<TinyCachePlugin>();

            // Act
            target.VirtualCacheFile = expected;
            var actual = target.VirtualCacheFile;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void ReadAndWritePhysicalCacheFile()
        {
            // Arrange
            string expected = "folder/name.txt";
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            TinyCachePlugin target = c.Plugins.Get<TinyCachePlugin>();

            // Act
            target.VirtualCacheFile = "~/" + expected;
            var actual = target.PhysicalCacheFile;

            // Assert
            Assert.True(actual.EndsWith(expected));
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void ReadPhysicalCacheFileWithNullVirtualCacheFile()
        {
            // Arrange
            string expected = null;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            TinyCachePlugin target = c.Plugins.Get<TinyCachePlugin>();

            // Act
            target.VirtualCacheFile = expected;
            var actual = target.PhysicalCacheFile;

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void ReadPhysicalCacheFileWithEmptyVirtualCacheFile()
        {
            // Arrange
            string expected = string.Empty;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            TinyCachePlugin target = c.Plugins.Get<TinyCachePlugin>();

            // Act
            target.VirtualCacheFile = expected;
            var actual = target.PhysicalCacheFile;

            // Assert
            Assert.Null(actual);
        }

        /// <summary>
        /// The CanProcess method does not use the Context.
        /// </summary>
        [Fact]
        public void CanOperateWithEmptyVirtualCacheFile()
        {
            // Arrange
            bool expected = false;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            TinyCachePlugin target = c.Plugins.Get<TinyCachePlugin>();
            target.VirtualCacheFile = string.Empty;

            // Act
            var actual = target.CanOperate;

            // Assert
            Assert.Equal<bool>(expected, actual);
        }

        /// <summary>
        /// Test the Process method for normal behavior.
        /// </summary>
        /// <remarks>
        /// There is no external indication that the method has actually done
        /// anything. We therefore assume success, but the test will report any
        /// exceptions that are thrown.
        /// </remarks>
        [Fact]
        public void ProcessTest()
        {
            // Arrange
            bool expected = true;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            ICache target = c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;
            args.RequestKey = "test";
            args.ResizeImageToStream = (Stream ms) =>
            {
                ms.WriteByte(99);
            };

            // Act
            target.Process(HttpContext.Current, args);

            // Assert
            Assert.True(expected);
        }

        /// <summary>
        /// Test the Process method for normal behavior.
        /// </summary>
        /// <remarks>
        /// The test should create a cache file on disk. We check for its
        /// existence. We can't access the ChangeThreshold value. So if it is 
        /// changed to a higher value than used here, this test will fail.
        /// </remarks>
        [Fact]
        public void ProcessForceWriteCountFlushTest()
        {
            // Arrange
            int expected = 60;
            int actual = 0;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var target = c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;

            File.Delete(target.PhysicalCacheFile);
            target.VirtualCacheFile = "folder/ImFlushed.txt";
            Directory.Delete(Path.GetDirectoryName(target.PhysicalCacheFile), true);

            // Act
            for (int i = 0; i < expected; i++)
            {
                args.ResizeImageToStream = (Stream ms) =>
                {
                    ms.WriteByte(99);
                    actual++;
                };
                args.RequestKey = string.Format(CultureInfo.InvariantCulture, "test{0}", i);
                target.Process(HttpContext.Current, args);
            }

            // Assert
            Assert.True(File.Exists(target.PhysicalCacheFile));
            Assert.Equal<int>(expected, actual);
        }

        /// <summary>
        /// Test the Process method for normal behavior.
        /// </summary>
        /// <remarks>
        /// The test should create a cache file on disk. We check for its
        /// existence. We can't access the ChangeThreshold value. So if it is 
        /// changed to a higher value than used here, this test will fail.
        /// </remarks>
        [Fact]
        public void ProcessReadCacheFileTest()
        {
            // Arrange
            int expected = 0;
            int actual = 0;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var target = c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;

            File.Delete(target.PhysicalCacheFile);
            target.VirtualCacheFile = "folder/ImFlushed.txt";
            if (Directory.Exists(Path.GetDirectoryName(target.PhysicalCacheFile)))
            {
                Directory.Delete(Path.GetDirectoryName(target.PhysicalCacheFile), true);
            }

            for (int i = 0; i < 60; i++)
            {
                args.ResizeImageToStream = (Stream ms) =>
                {
                    ms.WriteByte(99);
                };
                args.RequestKey = string.Format(CultureInfo.InvariantCulture, "test{0}", i);
                target.Process(HttpContext.Current, args);
            }

            target = new TinyCachePlugin();
            target.VirtualCacheFile = "folder/ImFlushed.txt";
            args.ResizeImageToStream = (Stream ms) =>
            {
                ms.WriteByte(99);
                actual++;
            };

            // Act
            args.RequestKey = "test0";
            target.Process(HttpContext.Current, args);

            // Assert
            Assert.True(File.Exists(target.PhysicalCacheFile));
            Assert.Equal<int>(expected, actual);
        }

        /// <summary>
        /// Test the Process method for normal behavior.
        /// </summary>
        /// <remarks>
        /// The test should create a cache file on disk. We check for its
        /// existence. We put enough entries in the cache to force it to remove some.
        /// </remarks>
        [Fact]
        public void ProcessForceWriteCacheRemovalsTest()
        {
            // Arrange
            int actual = 0;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var target = c.Plugins.Get<TinyCachePlugin>();
            int expected = target.MaxItems + 50;

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;

            File.Delete(target.PhysicalCacheFile);
            target.VirtualCacheFile = "folder/TooMany.txt";
            if (Directory.Exists(Path.GetDirectoryName(target.PhysicalCacheFile)))
            {
                Directory.Delete(Path.GetDirectoryName(target.PhysicalCacheFile), true);
            }

            // Act
            for (int i = 0; i < target.MaxItems + 50; i++)
            {
                args.ResizeImageToStream = (Stream ms) =>
                {
                    ms.WriteByte(99);
                    actual++;
                };
                args.RequestKey = string.Format(CultureInfo.InvariantCulture, "test{0}", i);
                target.Process(HttpContext.Current, args);
            }

            // Assert
            Assert.True(File.Exists(target.PhysicalCacheFile));
            Assert.Equal<int>(expected, actual);
        }

        /// <summary>
        /// Test the Process method to see if it actually caches data.
        /// </summary>
        /// <remarks>
        /// The call back to create the image data should only be called once.
        /// </remarks>
        [Fact]
        public void ProcessWithCachedDataTest()
        {
            // Arrange
            int expected = 1;
            int actual = 0;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            ICache target = c.Plugins.Get<TinyCachePlugin>();

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;
            args.RequestKey = "test";
            args.ResizeImageToStream = (Stream ms) =>
            {
                ms.WriteByte(99);
                actual++;
            };
            target.Process(HttpContext.Current, args);

            // Act
            target.Process(HttpContext.Current, args);

            // Assert
            Assert.Equal<int>(expected, actual);
        }

        /// <summary>
        /// Test the Process method to see if it actually caches data.
        /// </summary>
        /// <remarks>
        /// The test should create a cache file on disk. We check for its
        /// existence. We put enough data in the cache to force it to remove
        /// some because the cache is too big.
        /// </remarks>
        [Fact]
        public void ProcessWithTooMuchCachedDataTest()
        {
            // Arrange
            int actual = 0;
            var rs = new ResizerSection(ConfigXml);
            var c = new Config(rs);
            var target = c.Plugins.Get<TinyCachePlugin>();
            int expected = 52;

            ResizeSettings settings = new ResizeSettings();
            settings.Cache = ServerCacheMode.Default;
            ResponseArgs args = new ResponseArgs();
            args.RewrittenQuerystring = settings;
            args.RequestKey = "test";

            File.Delete(target.PhysicalCacheFile);
            target.VirtualCacheFile = "folder/TooMuch.txt";
            if (Directory.Exists(Path.GetDirectoryName(target.PhysicalCacheFile)))
            {
                Directory.Delete(Path.GetDirectoryName(target.PhysicalCacheFile), true);
            }

            byte[] data = new byte[1024 * 1024];

            // Act
            for (int i = 0; i < expected; i++)
            {
                args.ResizeImageToStream = (Stream ms) =>
                {
                    ms.Write(data, 0, data.Length);
                    actual++;
                };
                args.RequestKey = string.Format(CultureInfo.InvariantCulture, "test{0}", i);
                target.Process(HttpContext.Current, args);
            }

            // Assert
            Assert.Equal<int>(expected, actual);
        }
    }
}