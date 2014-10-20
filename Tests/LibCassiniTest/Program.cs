using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibCassini;
using ImageResizer.Core.Tests.SiteMocks;
using System.Diagnostics;

namespace CassiniTest {
    class Program {
        static void Main(string[] args) {

            SiteCreator site = new SiteCreator().Create();
            site.WriteWebConfig(new HttpTestingFixture().buildWebConfig());

            string physical = site.dir;

            Server s = null;
            try {
                s = new ServerFactory().CreateAndStart(physical, "/");
                Console.WriteLine("Started server at " + s.RootUrl);
                Console.WriteLine("Physical location: " + s.PhysicalPath);
                Process.Start(s.PhysicalPath);
                Process.Start(s.RootUrl.TrimEnd('/') + "/image.jpg?width=200");
                Console.WriteLine("Press any key to stop the server");
                Console.ReadKey();
            } finally {
                if (s != null) s.Stop();
            }
        }
    }
}
