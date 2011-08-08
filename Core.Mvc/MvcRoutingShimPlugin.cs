using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;

namespace ImageResizer.Plugins.MvcRoutingShim {
    public class MvcRoutingShimPlugin :IPlugin {
        StopRoutingRoute route = null;
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            route = new StopRoutingRoute(c.Pipeline.StopRoutingKey);   
            RouteTable.Routes.Insert(0, route);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            RouteTable.Routes.Remove(route);
            return true;
        }
    }
}
