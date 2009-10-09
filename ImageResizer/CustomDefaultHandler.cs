/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 **/

using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using fbs;
using System.Threading;
using System.Web.Compilation;
using System.Collections.Specialized;
using System.Text;
using System.Diagnostics;
namespace fbs.Handlers
{
    /// <summary>
    /// The IIS6 pipeline differs from Cassini, IIS5, and IIS7. 
    /// The DefaultHttpHandler passes requests on to IIS6 - ASP.NET loses control of download headers, image resizing, 404s, etc.
    /// This class is designed to rectify those issues.
    /// </summary>
    public class CustomDefaultHandler : DefaultHttpHandler
    {
        public CustomDefaultHandler()
        {
        }


        public override void OnExecuteUrlPreconditionFailure()
        {
            HttpContext.Current.Trace.Warn("OnExecuteUrlPreconditionFailure");
            //IIS won't get this request.
            //Throw and exception if ?thumbanil or ?download gets here
            base.OnExecuteUrlPreconditionFailure();
        }
        protected NameValueCollection IISHeaders = new NameValueCollection();


        [DebuggerHidden()]
        public override IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback callback, object state)
        {


            //First check for 404
            //Check that file exists 
            //Exceptions that won't be caught
            //Files specified by OverrideExecuteUrlPath that don't have Read permissions
            //406....




            bool exists = System.IO.File.Exists(context.Request.PhysicalPath);

            if (!exists)
            {
                throw new HttpException(404, "Page not found");
            }



            // Image resizing (pass to ImageResizingHandler)
            if (context.Request.QueryString["thumbnail"] != null)
            {
                //Downloads (Modify Response headers (ASP.NET only)
                if (context.Request.QueryString["download"] != null)
                {
                    if (!context.Request.QueryString["download"].Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.AppendHeader("Content-disposition", "attachment; filename=\"" + yrl.Current.Name + "\"");
                    }
                }
                //Custom image handler
                ImageResizingHandler.ProcessRequestInternal(context);
                return new HttpAsyncResultClone(callback, state, true, null, null);

            }
            else if (context.Request.QueryString["download"] != null)
            {
                if (!context.Request.QueryString["download"].Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.AppendHeader("Content-disposition", "attachment; filename=\"" + yrl.Current.Name + "\"");
                    //Trigger !CanExecuteUrlForEntireResponse
                    context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    context.Response.Cache.SetCacheability(HttpCacheability.Public);
                }
            }

            //Default handler (IIS6 or StaticFileHandler, depending upon CanExecuteUrlForEntireResponse
            return base.BeginProcessRequest(context, callback, state);
        }

        public override void EndProcessRequest(IAsyncResult result)
        {
            //Doesn't do anything unles base.BeginProccessRequest was called anyway.
            base.EndProcessRequest(result);
        }

     
    }


    /// <summary>
    /// Micorosft marked HttpAsyncResult internal - had to copy with reflector.
    /// </summary>
    public class HttpAsyncResultClone : IAsyncResult
    {
        public HttpAsyncResultClone(AsyncCallback cb, object state, bool completed, object result, Exception error)
        {
            this._callback = cb;
            this._asyncState = state;
            this._completed = completed;
            this._completedSynchronously = completed;
            this._result = result;
            this._error = error;
            if (this._completed && (this._callback != null))
            {
                this._callback(this);
            }
        }

        public HttpAsyncResultClone(AsyncCallback cb, object state)
        {
            this._callback = cb;
            this._asyncState = state;
        }

        private Exception _error;
        internal Exception Error { get { return this._error; } }


        private bool _completedSynchronously;
        public bool CompletedSynchronously { get { return this._completedSynchronously; } }

        private bool _completed;
        public bool IsCompleted { get { return this._completed; } }

        internal void SetComplete()
        {
            this._completed = true;
        }

        private object _asyncState;
        public object AsyncState { get { return this._asyncState; } }

        private AsyncCallback _callback;
        private object _result;

        public WaitHandle AsyncWaitHandle { get { return null; } }


        internal void Complete(bool synchronous, object result, Exception error)
        {
            this._completed = true;
            this._completedSynchronously = synchronous;
            this._result = result;
            this._error = error;
            if (this._callback != null)
            {
                this._callback(this);
            }
        }

        internal object End()
        {
            if (this._error != null)
            {
                throw new HttpException(null, this._error);
            }
            return this._result;
        }
    }


}