using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using LibCassini;
using System.Diagnostics;
using System.Net;
using LibCassini.Client;
using System.IO;
using System.Threading;
using System.Web;

[assembly: DegreeOfParallelism(20)]

namespace ImageResizer.Core.Tests.SiteMocks {
    
    [System.Web.AspNetHostingPermission(System.Security.Permissions.SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Unrestricted)]
    public class HttpTestingFixture {

        public Server server = null;

        public virtual string ResizerSectionContents() {
            return "";
        }
        
        [FixtureSetUp()]
        public void StartServer() {
            SiteCreator site = new SiteCreator().Create();
            site.WriteWebConfig(new WebConfigBuilder(ResizerSectionContents()).Build());
            string path = site.dir;
            server = new ServerFactory().CreateAndStart(path, "/");
            Debug.WriteLine("Located at " + path);
        }
        [FixtureTearDown()]
        public void StopServer() {
            if (server != null) {
                server.Stop();
                Debug.WriteLine("Server stopped");
            }
            server = null;
        }

        [Test()]
        public void TestOn() {
            Debug.WriteLine("IsListening=" + server.IsListening.ToString());
            Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, this.Request("image.jpg").StatusCode);
        }

        public ClientResponse Request(string relativeUrl) {
            return new ClientRequest(server, relativeUrl).Execute();
        }

    }
}
