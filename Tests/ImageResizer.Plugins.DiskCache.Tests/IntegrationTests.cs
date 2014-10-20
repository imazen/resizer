using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Core.Tests.SiteMocks;
using MbUnit.Framework;
using LibCassini.Client;
using System.Web;
using System.Diagnostics;

namespace ImageResizer.Plugins.DiskCache.Tests {
    public class IntegrationTests : HttpTestingFixture {
        public override string ResizerSectionContents() {
            return "<plugins><add name='DiskCache' /></plugins>";
        }

        [Test]
        public void TestAppDomains() {
            ClientResponse r = this.Request("image.jpg?height=200");
            Debug.WriteLine(AppDomain.CurrentDomain.FriendlyName);
            Debug.WriteLine(server.GetHost().ExecuteDelegate(delegate() {
                return AppDomain.CurrentDomain.FriendlyName;
            }));
        }
    }
}
