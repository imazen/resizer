using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using LibCassini;
using ImageResizer.Core.Tests.SiteMocks;
using System.Diagnostics;
using System.Net;

namespace ImageResizer.Core.Tests {
    [TestFixture]
    public class HttpTests: HttpTestingFixture{

        [Test]
        public void TestOn() {
            Debug.WriteLine(server.RootUrl);
            Debug.WriteLine(server.PhysicalPath);
           
        }
        [Test]
        public void TestRequest() {
            Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, this.Request("image.jpg").StatusCode);
        }
    }
}
