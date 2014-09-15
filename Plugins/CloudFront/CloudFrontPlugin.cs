﻿using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
using ImageResizer.Util;

namespace ImageResizer.Plugins.CloudFront {
    /// <summary>
    /// Allows querystrings to be expressed with '/' or ';' instead of '?', allow the querystring to survive the cloudfront guillotine. 
    /// Since IIS can't stand '&' symbols in the path, you have to replace both '?' and '&' with ';'
    /// Later I hope to include control adapters to automate the process.
    /// </summary>
    public class CloudFrontPlugin : IPlugin {

        /// <summary>
        /// Initialize the CLoudFrontPlugin
        /// </summary>
        public CloudFrontPlugin() { }

        Config c;

        /// <summary>
        /// Redirect Through
        /// </summary>
        protected string redirectThrough = null;

        /// <summary>
        /// true if redirect is Permanent redirect
        /// </summary>
        protected bool redirectPermanent = false;

        /// <summary>
        /// Install the plugin to the given config
        /// </summary>
        /// <param name="c">ImageResizer configuration</param>
        /// <returns>plugin that was added to the config</returns>
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            redirectThrough = c.get("cloudfront.redirectthrough", null);
            redirectPermanent = c.get("cloudfront.permanentRedirect", false);
            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            if (redirectThrough != null && c.Pipeline.ModifiedQueryString.Count > 0) {
                //It wasn't a cloudfront URL - it had a normal querystring
                context.Items[c.Pipeline.ModifiedPathKey + ".hadquery"] = true;
            }
            //Transform semicolon querystrings into the normal collection
            TransformCloudFrontUrl(context);
        }

        bool TransformCloudFrontUrl(System.Web.HttpContext context) {
            string s = c.Pipeline.PreRewritePath;
            int semi = s.IndexOf(';');
            if (semi < 0) return false; //No querystring here.
            int question = s.IndexOf('?');
            if (question > -1) throw new ImageProcessingException("ASP.NET failed to parse querystring, question mark remains in path segment: " + s);



            string path = s.Substring(0, semi);

            //Why do we care what image type it is? That gets checked later
            //if (!c.Pipeline.IsAcceptedImageType(path)) return false; //Must be valid image type
            c.Pipeline.PreRewritePath = path; //Fix the path.

            //Parse the fake query, merge it with the real one, and we are done.
            string query = s.Substring(semi);
            NameValueCollection q = Util.PathUtils.ParseQueryStringFriendlyAllowSemicolons(query);

            //Merge the querystring with everything found in PathInfo. THe querystring wins on conflicts
            foreach (string key in c.Pipeline.ModifiedQueryString.Keys)
                q[key] = c.Pipeline.ModifiedQueryString[key];

            c.Pipeline.ModifiedQueryString = q;

            return true;
        }

        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            ///Handle redirectThrough behavior
            if (redirectThrough != null && context.Items[c.Pipeline.ModifiedPathKey + ".hadquery"] != null) {
                //It had a querystring originally - which means the request didn't come from CloudFront, it came directly from the browser. Perform a redirect, rewriting the querystring appropriately
                string finalPath = redirectThrough + e.VirtualPath + PathUtils.BuildSemicolonQueryString(e.QueryString, true);

                //Redirect according to setting
                context.Response.Redirect(finalPath, !redirectPermanent);
                if (redirectPermanent) {
                    context.Response.StatusCode = 301;
                    context.Response.End();
                }
            }

        }

        /// <summary>
        /// Removes the plugin from the given config
        /// </summary>
        /// <param name="c">ImageResizer config</param>
        /// <returns>true if the plugin has been removed</returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
