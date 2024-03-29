// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Web;
using System.Web.Routing;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    public class MvcRoutingShimPlugin : IPlugin
    {
        private StopRoutingRoute route = null;

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            route = new StopRoutingRoute(c.Pipeline.StopRoutingKey);
            using (RouteTable.Routes.GetWriteLock())
            {
                RouteTable.Routes.Insert(0, route);
            }

            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            using (RouteTable.Routes.GetWriteLock())
            {
                RouteTable.Routes.Remove(route);
            }

            return true;
        }

        public class StopRoutingRoute : RouteBase
        {
            private string _contextItemsFlag = null;

            /// <summary>
            ///     Creates a route that matches any request where context.Items[contextItemsFlag] is (non-null).
            /// </summary>
            /// <param name="contextItemsFlag"></param>
            public StopRoutingRoute(string contextItemsFlag)
            {
                _contextItemsFlag = contextItemsFlag;
            }

            public override RouteData GetRouteData(HttpContextBase httpContext)
            {
                try
                {
                    if (httpContext.Items[_contextItemsFlag] != null)
                        return new RouteData(this, new StopRoutingHandler());
                }
                catch (NotImplementedException)
                {
                } //For compatibility with Kendo UI and unit test/mocks which don't implement .Items

                return null;
            }

            public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
            {
                return null;
            }
        }
    }
}