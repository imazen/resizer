// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using Imazen.Common.Storage;

namespace ImageResizer
{
    /// <summary>
    ///     An alias for AsyncInterceptModule as of v5. Monitors incoming image requests to determine if resizing (or other
    ///     processing) is being requested.
    /// </summary>
    public class InterceptModule : AsyncInterceptModule
    {
    }

    public class AsyncInterceptModule : IHttpModule
    {
        public class AsyncResponsePlan : IAsyncResponsePlan
        {
            public string EstimatedContentType { get; set; }

            public string EstimatedFileExtension { get; set; }

            public string RequestCachingKey { get; set; }

            public NameValueCollection RewrittenQuerystring { get; set; }

            public ReadStreamAsyncDelegate OpenSourceStreamAsync { get; set; }

            public WriteResultAsyncDelegate CreateAndWriteResultAsync { get; set; }
        }

        private static int moduleInstancesActiveSharedCounter = 0;

        public void Init(HttpApplication context)
        {
            Interlocked.Increment(ref moduleInstancesActiveSharedCounter);
                
            var helper = new EventHandlerTaskAsyncHelper(CheckRequest_PostAuthorizeRequest_Async);
            context.AddOnPostAuthorizeRequestAsync(helper.BeginEventHandler, helper.EndEventHandler);

            conf.ModuleInstalled = true;
        }
        public void Dispose()
        {
            if (Interlocked.Decrement(ref moduleInstancesActiveSharedCounter) != 0) return;
            
            //The last instance of the module is being disposed, now we can tear down shared services.
            var tasks = Config.Current.Plugins.GetAll<IPluginRequiresShutdown>().Select(p => p.StopAsync(CancellationToken.None)).ToArray();
            Task.WaitAll(tasks); //Is this OK??
        }


        protected IPipelineConfig conf => Config.Current.Pipeline;


        protected async Task CheckRequest_PostAuthorizeRequest_Async(object sender, EventArgs e)
        {
            //Skip requests if the Request object isn't populated
            var app = sender as HttpApplication;
            if (app?.Context?.Request == null) return;


            var ra = new HttpModuleRequestAssistant(app.Context, conf, this);

            var result = ra.PostAuthorize();

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult
                    .UnrelatedFileType) return; //Exit early, not our scene

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult.AccessDenied403)
            {
                ra.FireAccessDenied();
                throw new ImageProcessingException(403, "Access denied", "Access denied");
            }

            if (result == HttpModuleRequestAssistant.PostAuthorizeResult.Complete)
            {
                //Does the file exist physically? (false if VppUsage=always or file is missing)
                var existsPhysically = conf.VppUsage != VppUsageOption.Always;
                if (existsPhysically) existsPhysically = File.Exists(ra.RewrittenMappedPath);

                //If not present physically (and VppUsage!=never), try to get the virtual file. Null indicates a missing file
                if (conf.VppUsage != VppUsageOption.Never && !existsPhysically)
                    ra.VirtualFile = await conf.GetFileAsync(ra.RewrittenVirtualPath, ra.RewrittenQuery);

                //Only process files that exist
                if (!existsPhysically && ra.VirtualFile == null)
                {
                    ra.FireMissing();
                    return;
                }

                try
                {
                    await HandleRequest(app.Context, ra);
                    //Catch not found exceptions
                    ra.FirePostAuthorizeSuccess();
                }
                catch (BlobMissingException notFound)
                {
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (FileNotFoundException notFound)
                {
                    //Some VPPs are optimistic, or could be a race condition
                    if (notFound.Message.Contains(" assembly "))
                        throw; //If an assembly is missing, it should be a 500, not a 404
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (DirectoryNotFoundException notFound)
                {
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (Exception ex)
                {
                    ra.FirePostAuthorizeRequestException(ex);
                    throw;
                }
            }
        }


        /// <summary>
        ///     Generates the resized image to disk (if needed), then rewrites the request to that location.
        ///     Perform 404 checking before calling this method. Assumes file exists.
        ///     Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ra"></param>
        /// <param name="vf"></param>
        protected virtual async Task HandleRequest(HttpContext context, HttpModuleRequestAssistant ra)
        {
            if (!ra.CachingIndicated && !ra.ProcessingIndicated)
            {
                ra.ApplyRewrittenPath(); //This is needed for both physical and virtual files; only makes changes if needed.
                if (!ra.IsVirtualFile)
                {
                    ra.AssignSFH(); //
                    return;
                }
            }
            
            //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
            context.Items[conf.StopRoutingKey] = true;
            context.Items[conf.ResponseArgsKey] = ""; //We are handling the request


            ra.EstimateResponseInfo();


            //Build CacheEventArgs
            var e = new AsyncResponsePlan();

            var modDate = ra.VirtualFile == null
                ? File.GetLastWriteTimeUtc(ra.RewrittenMappedPath)
                :
                ra.VirtualFile is IVirtualFileWithModifiedDateAsync
                    ?
                    await ((IVirtualFileWithModifiedDateAsync)ra.VirtualFile).GetModifiedDateUTCAsync()
                    : DateTime.MinValue;

            e.RequestCachingKey = ra.GenerateRequestCachingKey(modDate);

            var settings = new ResizeSettings(ra.RewrittenInstructions);

            e.RewrittenQuerystring = settings;
            e.EstimatedContentType = ra.EstimatedContentType;
            e.EstimatedFileExtension = ra.EstimatedFileExtension;

            var vf = ra.VirtualFile;
            //A delegate for accessing the source file
            e.OpenSourceStreamAsync = async delegate()
            {
                return vf != null
                    ? await vf.OpenAsync()
                    : File.Open(ra.RewrittenMappedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            };

            //Add delegate for writing the data stream
            e.CreateAndWriteResultAsync = async delegate(Stream stream, IAsyncResponsePlan plan)
            {
                //This runs on a cache miss or cache invalid. This delegate is preventing from running in more
                //than one thread at a time for the specified cache key
                try
                {
                    if (!ra.ProcessingIndicated)
                    {
                        //Just duplicate the data
                        using (var source = await e.OpenSourceStreamAsync())
                        {
                            await source.CopyToAsync(stream); //4KiB buffer
                        }
                    }
                    else
                    {
                        MemoryStream inBuffer = null;
                        using (var sourceStream = vf != null
                                   ? await vf.OpenAsync()
                                   : File.Open(ra.RewrittenMappedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            inBuffer = new MemoryStream(sourceStream.CanSeek ? (int)sourceStream.Length : 128 * 1024);
                            await sourceStream.CopyToAsync(inBuffer);
                        }

                        inBuffer.Seek(0, SeekOrigin.Begin);

                        var outBuffer = new MemoryStream(32 * 1024);

                        //Handle I/O portions of work asynchronously. 
                        var j = new ImageJob
                        {
                            Instructions = new Instructions(settings),
                            SourcePathData = vf != null ? vf.VirtualPath : ra.RewrittenVirtualPath,
                            Dest = outBuffer,
                            Source = inBuffer
                        };

                        await conf.GetImageBuilder().BuildAsync(j, int.MaxValue, CancellationToken.None);

                        outBuffer.Seek(0, SeekOrigin.Begin);
                        await outBuffer.CopyToAsync(stream);
                    }

                    ra.FireJobSuccess();
                    //Catch not found exceptions
                }
                catch (BlobMissingException notFound)
                {
                    //This will be called later, if at all. 
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (FileNotFoundException notFound)
                {
                    if (notFound.Message.Contains(" assembly "))
                        throw; //If an assembly is missing, it should be a 500, not a 404

                    //This will be called later, if at all. 
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (DirectoryNotFoundException notFound)
                {
                    ra.FireMissing();
                    throw new ImageMissingException("The specified resource could not be located", "File not found",
                        notFound);
                }
                catch (Exception ex)
                {
                    ra.FireJobException(ex);
                    throw;
                }
            };


            //All bad from here down....
            context.Items[conf.ResponseArgsKey] = e; //store in context items

            //Fire events (for client-side caching plugins)
            conf.FirePreHandleImageAsync(this, context, e);

            //Pass the rest of the work off to the caching module. It will handle rewriting/redirecting and everything. 
            //We handle request headers based on what is found in context.Items
            var cache = conf.GetAsyncCacheFor(context, e);

            //Verify we have a caching system
            if (cache == null)
                throw new ImageProcessingException("Image Resizer: No async caching plugin was found for the request");

            await cache.ProcessAsync(context, e);
        }


        protected void FileMissing(HttpContext httpContext, string virtualPath, NameValueCollection q)
        {
            //Fire the event (for default image redirection, etc) 
            conf.FireImageMissing(this, httpContext, new UrlEventArgs(virtualPath, new NameValueCollection(q)));

            //Remove the image from context items so we don't try to write response headers.
            httpContext.Items[conf.ResponseArgsKey] = null;
        }
    }
}