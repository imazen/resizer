// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.Licensed under the Apache License, Version 2.0.
using System;
using ImageResizer.Configuration;
using System.Web;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web.Security;
using System.Web.Hosting;
using System.Diagnostics;
using ImageResizer.Plugins;
using ImageResizer.Encoding;
using ImageResizer.Caching;
using ImageResizer.Util;
using System.Collections.Generic;
using System.IO;
using ImageResizer.Resizing;
using ImageResizer.Plugins.Basic;
using ImageResizer.ExtensionMethods;
using System.Globalization;
using System.Security;
using System.Security.Permissions;

// This namespace contains the most frequently used classes.
namespace ImageResizer {

    /// <summary>
    /// Monitors incoming image requests to determine if resizing (or other processing) is being requested.
    /// </summary>
    public class InterceptModule : IHttpModule {

        /// <summary>
        /// Called when the app is initialized
        /// </summary>
        /// <param name="context"></param>
        void IHttpModule.Init(System.Web.HttpApplication context) {
            
            //We wait until after URL auth happens for security. (although we authorize again since we are doing URL rewriting)
            context.PostAuthorizeRequest -= CheckRequest_PostAuthorizeRequest;
            context.PostAuthorizeRequest += CheckRequest_PostAuthorizeRequest;

            //This is where we set content-type and caching headers. content-type often don't match the
            //file extension, so we have to override them
            context.PreSendRequestHeaders -= context_PreSendRequestHeaders;
            context.PreSendRequestHeaders += context_PreSendRequestHeaders;

            //Say it's installed.
            conf.ModuleInstalled = true;

        }
        void IHttpModule.Dispose() { }
        /// <summary>
        /// Current configuration. Same as Config.Current.Pipeline
        /// </summary>
        protected IPipelineConfig conf { get { return Config.Current.Pipeline; } }

        /// <summary>
        /// This is where we filter requests and intercept those that want resizing performed.
        /// We first strip FakeExtension, then verify the remaining file extension is supported for decoding.
        /// We fire URL rewriting events. If the result includes any supported querystring params afterwards, we process the request. Otherwise we let it fall back to IIS/ASP.NET.
        /// If the file doesn't exist, we also ignore the request. They're going to cause a 404 anyway.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CheckRequest_PostAuthorizeRequest(object sender, EventArgs e) {
            //Skip requests if the Request object isn't populated
            
            HttpApplication app = sender as HttpApplication;
            if (app == null || app.Context == null || app.Context.Request == null) return;


            var ra = new HttpModuleRequestAssistant(app.Context, conf, this);

            var result = ra.PostAuthorize();

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult.UnrelatedFileType){
                return; //Exit early, not our scene
            }

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult.AccessDenied403){
                throw new ImageProcessingException(403, "Access denied", "Access denied");
            }

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult.Complete && ra.RewrittenQueryHasDirective)
            {

                //Does the file exist physically? (false if VppUsage=always or file is missing)
                bool existsPhysically = (conf.VppUsage != VppUsageOption.Always) && System.IO.File.Exists(ra.RewrittenMappedPath);

                //If not present physically (and VppUsage!=never), try to get the virtual file. Null indicates a missing file
                IVirtualFile vf = (conf.VppUsage != VppUsageOption.Never && !existsPhysically) ? conf.GetFile(ra.RewrittenVirtualPath, ra.RewrittenQuery) : null;

                //Only process files that exist
                if (!existsPhysically && vf == null){
                    ra.FireMissing();
                    return;
                }
                
                try{
                    HandleRequest(app.Context,ra,  vf);
                    //Catch not found exceptions
                } catch (System.IO.FileNotFoundException notFound) { //Some VPPs are optimisitic , or could be a race condition
                    if (notFound.Message.Contains(" assembly ")) throw; //If an assembly is missing, it should be a 500, not a 404
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                } catch (System.IO.DirectoryNotFoundException notFound) {
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                }
             
            }

        }

  



        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ra"></param>
        /// <param name="vf"></param>
        protected virtual void HandleRequest(HttpContext context, HttpModuleRequestAssistant ra, IVirtualFile vf)
        {
            if (!ra.CachingIndicated && !ra.ProcessingIndicated){

                //TODO: Pass on to static file handler!
                return;
            }
            
            context.Items[conf.ResponseArgsKey] = ""; //We are handling the request

            //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
            context.Items[conf.StopRoutingKey] = true;


            ra.EstimateResponseInfo();

            //Build CacheEventArgs
            ResponseArgs e = new ResponseArgs();
           
            //Add the modified date to the request key, if present.
            var modDate = (vf == null) ? System.IO.File.GetLastWriteTimeUtc(ra.RewrittenMappedPath) : 
                (vf is IVirtualFileWithModifiedDate ? ((IVirtualFileWithModifiedDate)vf).ModifiedDateUTC : DateTime.MinValue);

            e.RequestKey = ra.GenerateRequestCachingKey(modDate);


            var settings = new ResizeSettings(ra.RewrittenInstructions);

            e.RewrittenQuerystring = settings;
            e.ResponseHeaders.ContentType = ra.EstimatedContentType;
            e.SuggestedExtension = ra.EstimatedFileExtension;


            //A delegate for accessing the source file
            e.GetSourceImage = new GetSourceImageDelegate(delegate() {
                return (vf != null) ? vf.Open() : File.Open(ra.RewrittenMappedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            });

            //Add delegate for writing the data stream
            e.ResizeImageToStream = new ResizeImageDelegate(delegate(System.IO.Stream stream) {
                //This runs on a cache miss or cache invalid. This delegate is preventing from running in more
                //than one thread at a time for the specified cache key
                try {
                    if (!ra.ProcessingIndicated) {
                        //Just duplicate the data
                        using (Stream source = e.GetSourceImage())
                            source.CopyToStream(stream); //4KiB buffer
                        
                    } else {
                        //Process the image
                        if (vf != null)
                            conf.GetImageBuilder().Build(vf, stream, settings);
                        else
                            conf.GetImageBuilder().Build(ra.RewrittenMappedPath, stream, settings); //Use a physical path to bypass virtual file system
                    }

                    //Catch not found exceptions
                } catch (System.IO.FileNotFoundException notFound) {
                    if (notFound.Message.Contains(" assembly ")) throw; //If an assembly is missing, it should be a 500, not a 404
                    
                    //This will be called later, if at all. 
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                    
                } catch (System.IO.DirectoryNotFoundException notFound) {
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                }
            });


            context.Items[conf.ResponseArgsKey] = e; //store in context items

            //Fire events (for client-side caching plugins)
            conf.FirePreHandleImage(this, context, e);

            //Pass the rest of the work off to the caching module. It will handle rewriting/redirecting and everything. 
            //We handle request headers based on what is found in context.Items
            ICache cache = conf.GetCacheProvider().GetCachingSystem(context, e);

            //Verify we have a caching system
            if (cache == null) throw new ImageProcessingException("Image Resizer: No caching plugin was found for the request");
            
            cache.Process(context, e); //TODO: Verify that caching systems serves request or transfers to StaticFileHandler

            

        }

        /// <summary>
        /// We don't actually send the data - but we still want to control the headers on the data.
        /// PreSendRequestHeaders allows us to change the content-type and cache headers at exactly the last moment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void context_PreSendRequestHeaders(object sender, EventArgs e) {
            //Skip requests if the Request object isn't populated
            HttpApplication app = sender as HttpApplication;
            if (app == null || app.Context == null || app.Context.Items == null || app.Context.Request == null || app.Context.Response == null) return;
            HttpContext context = app.Context;
            //Skip requests if we don't have an object to work with.
            if (context.Items[conf.ResponseArgsKey] == null) return;
            //Try to cast the object.
            IResponseArgs obj = context.Items[conf.ResponseArgsKey] as IResponseArgs;
            if (obj == null) return;
            //Apply the headers
            if (obj.ResponseHeaders.ApplyDuringPreSendRequestHeaders)
                obj.ResponseHeaders.ApplyToResponse(obj.ResponseHeaders, app.Context);


        }

    }
}
