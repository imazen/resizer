using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace ImageResizer.Plugins.DiskCache.Tests {
    [TestFixture]
    public class CachedFolderTest {

        CachedFolder f;

        [FixtureSetUp]
        public void start() {
            f = new CachedFolder();
        }
        [Test]
        public void Test() {
            //
            // TODO: Add test logic here
            //
        }
    }
}
