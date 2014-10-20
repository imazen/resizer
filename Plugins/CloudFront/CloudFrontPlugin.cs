using System;
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
    /// Since IIS can't stand ampersand symbols in the path, you have to replace both '?' and '&amp;' with ';'
    /// Later I hope to include control adapters to automate the process.
    /// </summary>
    public class CloudFrontPlugin : IPlugin {

        /// <summary>
        /// Creates a new instance of the CloutFront Plugin
        /// </summary>
        public CloudFrontPlugin() { }

        Config c;

        protected string redirectThrough = null;
        protected bool redirectPermanent = false;
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
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
            //Handle redirectThrough behavior
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
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
