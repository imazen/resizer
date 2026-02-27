// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using LibCassini;
using System.Diagnostics;
using System.Net;
using LibCassini.Client;
using System.IO;
using System.Threading;
using System.Web;

namespace ImageResizer.Core.Tests.SiteMocks {

    [System.Web.AspNetHostingPermission(System.Security.Permissions.SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Unrestricted)]
    public class HttpTestingFixture : IDisposable {

        public Server server = null;

        public virtual string ResizerSectionContents() {
            return "";
        }

        public HttpTestingFixture() {
            SiteCreator site = new SiteCreator().Create();
            site.WriteWebConfig(new WebConfigBuilder(ResizerSectionContents()).Build());
            string path = site.dir;
            server = new ServerFactory().CreateAndStart(path, "/");
            Debug.WriteLine("Located at " + path);
        }

        public void Dispose() {
            if (server != null) {
                server.Stop();
                Debug.WriteLine("Server stopped");
            }
            server = null;
        }

        [Fact]
        public void TestOn() {
            Debug.WriteLine("IsListening=" + server.IsListening.ToString());
            Assert.Equal(HttpStatusCode.OK, this.Request("image.jpg").StatusCode);
        }

        public ClientResponse Request(string relativeUrl) {
            return new ClientRequest(server, relativeUrl).Execute();
        }

    }
}
