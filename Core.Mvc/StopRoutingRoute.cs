﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;

namespace ImageResizer.Plugins.MvcRoutingShim {
    public class StopRoutingRoute : RouteBase {

        private string _contextItemsFlag = null;
        /// <summary>
        /// Creates a route that matches any request where context.Items[contextItemsFlag] is (non-null).
        /// </summary>
        /// <param name="contextItemsFlag"></param>
        public StopRoutingRoute(string contextItemsFlag) {
            _contextItemsFlag = contextItemsFlag;
        }
        public override RouteData GetRouteData(System.Web.HttpContextBase httpContext) {
            try {
                if (httpContext.Items[_contextItemsFlag] != null)
                    return new RouteData(this, new StopRoutingHandler());
            } catch (NotImplementedException) { } //For compatibility with Kendo UI and unit test/mocks which dont' implement .Items

            return null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values) {
            return null;
        }
    }
}
