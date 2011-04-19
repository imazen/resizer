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
    [TestFixture]
    [System.Web.AspNetHostingPermission(System.Security.Permissions.SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Unrestricted)]
    public class HttpTestingFixture {

        public Server server = null;

        public string webConfigTop = "<?xml version='1.0' encoding='utf-8' ?><configuration><configSections>" + 
            "<section name='resizer' restartOnExternalChanges='true' requirePermission='false' type='ImageResizer.ResizerSection,ImageResizer' />" + 
            "</configSections>";

        public string resizerTop = 
            "<resizer xmlns='http://imageresizingin.net/resizer.xsd'  xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:noNamespaceSchemaLocation='resizer.xsd'>\n\n";

        public string resizerContents = "";

        public string resizerBottom = "\n</resizer>\n";

        public string compilationTop = "<system.web><compilation debug='true'><assemblies>";
        public string webConfigBottom =
            "</assemblies></compilation><httpModules> <add name='ImageResizingModule' type='ImageResizer.InterceptModule'/>\n" +
            "</httpModules></system.web>\n" + 
            "<system.webServer><validation validateIntegratedModeConfiguration='false'/>\n" + 
            "<modules> <add name='ImageResizingModule' type='ImageResizer.InterceptModule'/>\n" +
            "</modules></system.webServer></configuration>\n";

        public virtual string buildWebConfig(){
            return webConfigTop + resizerTop + resizerContents + resizerBottom + compilationTop + webConfigBottom;
        }
        //public virtual string GetAssemblies(string binDir) {
        //    string[] paths = Directory.GetFiles(binDir, "*.dll");
        //    StringBuilder sb = new StringBuilder();
        //    foreach (string path in paths) {
        //        sb.AppendLine("<add assembly='" + Path.GetFileNameWithoutExtension(path) + "'/>");
        //    }
        //    return sb.ToString();
        //}
        
        [FixtureSetUp()]
        public void StartServer() {
            SiteCreator site = new SiteCreator().Create();
            site.WriteWebConfig(buildWebConfig());
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

        [Test()]
        public void TestExternal() {
            Debug.WriteLine("IsListening=" + server.IsListening.ToString());
            //Process.Start(server.RootUrl + "image.jpg");
            //Thread.Sleep(10000);
        }

        public ClientResponse Request(string relativeUrl) {
            return new ClientRequest(server, relativeUrl).Execute();
        }

    }
}
