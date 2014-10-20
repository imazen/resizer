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
   
    public class HttpTests: HttpTestingFixture{

        
        [Test]
        public void TestRequest() {
            Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, this.Request("image.jpg?width=100").StatusCode);
        }
    }
}
