// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ImageResizer.Configuration;
using ImageResizer.Util;

namespace MvcSample
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Photos", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
            Config.Current.Pipeline.Rewrite += new UrlRewritingEventHandler(Pipeline_Rewrite);
            Config.Current.Pipeline.AuthorizeImage += new UrlAuthorizationEventHandler(Pipeline_AuthorizeImage);
        }

        private static void Pipeline_AuthorizeImage(IHttpModule sender, HttpContext context,
            IUrlAuthorizationEventArgs e)
        {
            if (e.VirtualPath.StartsWith(PathUtils.ResolveAppRelative("~/App_Data/photos/"),
                    StringComparison.OrdinalIgnoreCase)) e.AllowAccess = true;
        }

        private static void Pipeline_Rewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            if (e.VirtualPath.StartsWith(PathUtils.ResolveAppRelative("~/photos/"), StringComparison.OrdinalIgnoreCase))
                e.VirtualPath = PathUtils.ResolveAppRelative("~/App_Data/") +
                                e.VirtualPath.Substring(PathUtils.AppVirtualPath.Length).TrimStart('/');
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}