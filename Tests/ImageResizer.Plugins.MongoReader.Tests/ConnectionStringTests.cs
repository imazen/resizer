using System;
using System.Collections.Specialized;
using MbUnit.Framework;

namespace ImageResizer.Plugins.MongoReader.Tests
{
    [TestFixture]
    public class TestMongoReader
    {
        [Test]
        public void GetConnectionString_ShouldReturnConnectionString_WhenConnectionStringSpecified()
        {
            var expected = "test";
            var args = new NameValueCollection
            {
                {"connectionString", expected}
            };

            var actual = MongoReaderPlugin.GetConnectionString(args);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetConnectionString_ShouldReturnConnectionString_WhenValidConnectionStringNameSpecified()
        {
            var expected = "mongodb://server/database?from=connectionString";
            var args = new NameValueCollection
            {
                {"connectionStringName", "MongoReader.ConnectionString.Name"}
            };

            var actual = MongoReaderPlugin.GetConnectionString(args);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetConnectionString_ShouldThrowException_WhenInvalidConnectionStringNameSpecified()
        {
            var args = new NameValueCollection
            {
                {"connectionStringName", "MongoReader.ConnectionString.InvalidName"}
            };

            Assert.Throws<ApplicationException>(
                () => MongoReaderPlugin.GetConnectionString(args),
                "A connection string named \"{0}\" does not exist.",
                "MongoReader.ConnectionString.InvalidName");
        }

        [Test]
        public void GetConnectionString_ShouldThrowException_WhenValidConnectionStringNameSpecified_WithEmptyValue()
        {
            var args = new NameValueCollection
            {
                {"connectionStringName", "MongoReader.ConnectionString.Empty"}
            };

            Assert.Throws<ApplicationException>(
                () => MongoReaderPlugin.GetConnectionString(args),
                "A connection string named \"{0}\" does not exist.",
                "MongoReader.ConnectionString.InvalidName");
        }

        [Test]
        public void GetConnectionString_ShouldReturnConnectionString_WhenValidAppSettingKeySpecified()
        {
            var expected = "mongodb://server/database?from=appSetting";
            var args = new NameValueCollection
            {
                {"connectionStringAppKey", "MongoReader.ConnectionString.Key"}
            };

            var actual = MongoReaderPlugin.GetConnectionString(args);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetConnectionString_ShouldThrowException_WhenInvalidConnectionStringAppKeySpecified()
        {
            var args = new NameValueCollection
            {
                {"connectionStringAppKey", "MongoReader.ConnectionString.InvalidKey"}
            };

            Assert.Throws<ApplicationException>(
                () => MongoReaderPlugin.GetConnectionString(args),
                "An app setting with key \"{0}\" does not exist.",
                "MongoReader.ConnectionString.InvalidKey");
        }

        [Test]
        public void GetConnectionString_ShouldThrowException_WhenNoConnectionStringSpecified()
        {
            var args = new NameValueCollection();

            Assert.Throws<ApplicationException>(
                () => MongoReaderPlugin.GetConnectionString(args),
                "A MongoDB connection string must be specified.");
        }
    }
}
