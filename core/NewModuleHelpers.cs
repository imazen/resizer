using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Configuration.Performance;
using ImageResizer.Plugins;
using ImageResizer.Util;
using Microsoft.Extensions.Logging;
using Imazen.Common.Extensibility.StreamCache;

namespace ImageResizer
{
    internal class RequestJobInfo
    {
        public string FinalVirtualPath { get; private set; }

        private Dictionary<string,string> FinalQuery { get; set; }
        public bool HasParams { get; }
        
        public string CommandString { get; } = "";
    }
    
    internal class NewModuleHelpers
    { 
        internal static string GetContentTypeFromBytes(byte[] data)
        {
            if (data.Length < 12)
            {
                return "application/octet-stream";
            }
            //TODO: Extract this API so ImageResizer.dll doesn't depend on Imageflow.Net.dll
            return new Imazen.Common.FileTypeDetection.FileTypeDetector().GuessMimeType(data) ?? "application/octet-stream";
        }
        
        
        /// <summary>
        /// Proxies the given stream to the HTTP response, while also setting the content length
        /// and the content type based off the magic bytes of the image
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="response"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static async Task ProxyToStream(Stream sourceStream, HttpResponse response)
        {
            if (sourceStream.CanSeek)
            {
                var contentLength = sourceStream.Length - sourceStream.Position;
                if (contentLength > 0) response.Headers.Add("Content-Length", contentLength.ToString());
            }
            
            // We really only need 12 bytes but it would be a waste to only read that many. 
            const int bufferSize = 4096;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                var bytesRead = await sourceStream.ReadAsync(buffer,0,4096).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException("Source blob has zero bytes.");
                }
                
                response.ContentType = bytesRead >= 12 ? GetContentTypeFromBytes(buffer) : "application/octet-stream";
                response.BufferOutput = false;
                await response.OutputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            await sourceStream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        }
        
        private void SetCachingHeaders(HttpContext context, string etag)
        {
            // var mins = c.get("clientcache.minutes", -1);
            // //Set the expires value if present
            // if (mins > 0)
            //     e.ResponseHeaders.Expires = DateTime.UtcNow.AddMinutes(mins);
            //
            // //NDJ Jan-16-2013. The last modified date sent in the headers should NOT match the source modified date when using DiskCaching.
            // //Setting this will prevent 304s from being sent properly.
            // // (Moved to NoCache)
            //
            // //Authenticated requests only allow caching on the client. 
            // //Anonymous requests get caching on the server, proxy and client
            // if (context.Request.IsAuthenticated)
            //     e.ResponseHeaders.CacheControl = HttpCacheability.Private;
            // else
            //     e.ResponseHeaders.CacheControl = HttpCacheability.Public;
            //
            
            context.Response.Headers["ETag"] = etag;

            //TODO: Add support for max-age and public/private based on authentication.
            
            //context.Response.CacheControl = "max-age=604800";
            // if (options.DefaultCacheControlString != null)
            //     context.Response.Headers["Cache-Control"] = options.DefaultCacheControlString;
        }
        
        // private async Task ServeFileFromDisk(HttpContext context, string path, string etag)
        // {
        //     using (var readStream = File.OpenRead(path))
        //     {
        //         if (readStream.Length < 1)
        //         {
        //             throw new InvalidOperationException("DiskCache file entry has zero bytes");
        //         }
        //
        //         SetCachingHeaders(context, etag);
        //         await ProxyToStream(readStream, context.Response).ConfigureAwait(false);
        //     }
        // }
        //
        public async Task ProcessWithStreamCache(ILogger logger, IStreamCache streamCache, HttpContext context, IAsyncResponsePlan plan)
        {
            
            var cacheHash = PathUtils.Base64Hash(plan.RequestCachingKey);
            var cacheHashQuoted = "\"" + cacheHash + "\"";
            var cacheHashWeakValidation = "W/\"" + cacheHash + "\"";

            // Send 304
            var ifNoneMatch = (IList)context.Request.Headers.GetValues("If-None-Match");
            if (ifNoneMatch != null && (ifNoneMatch.Contains(cacheHash) || ifNoneMatch.Contains(cacheHashQuoted) || ifNoneMatch.Contains(cacheHashWeakValidation)))
            {
                context.Response.StatusCode = 304;
                context.Response.AppendHeader("Content-Length", "0");
                context.Response.End();
                return;
            }
            //
            // context.Response.AppendHeader("ETag", cacheHashWeakValidation);
            //
            
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(plan.RequestCachingKey);
            var typeName = streamCache.GetType().Name;
            
            // use a heuristic to determine if this is likely an image type we support. if not, if it's just some static file,
            // then enable content-type retrieval
            
            var cacheResult = await streamCache.GetOrCreateBytes(keyBytes, async (cancellationToken) =>
            {
                var stream = new MemoryStream(32 * 1024);

                await plan.CreateAndWriteResultAsync(stream, plan);
                var contentType = stream.Length > 12 ? GetContentTypeFromBytes(stream.GetBuffer().Take(12).ToArray()) : null;
                stream.Seek(0, SeekOrigin.Begin);
                return new StreamCacheInput(contentType, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length));
                
                //
                // if (info.HasParams)
                // {
                //     logger?.LogDebug("{CacheName} miss: Processing image {VirtualPath}?{Querystring}", typeName, info.FinalVirtualPath,info.ToString());
                //     var result = await info.ProcessUncached();
                //     if (result.ResultBytes.Array == null)
                //     {
                //         throw new InvalidOperationException("Image job returned zero bytes.");
                //     }
                //     return new StreamCacheInput(result.ContentType, result.ResultBytes);
                // }
                //
                // logger?.LogDebug("{CacheName} miss: Proxying image {VirtualPath}",typeName,  info.FinalVirtualPath);
                // //TODO store the mime-type!
                // var bytes = await info.GetPrimaryBlobBytesAsync();
                // return new StreamCacheInput(null, bytes);
            
            },CancellationToken.None,false);

            if (cacheResult.Status != null)
            {
                GlobalPerf.Singleton.IncrementCounter($"{typeName}_{cacheResult.Status}");
            }
            if (cacheResult.Data != null)
            {
                using (cacheResult.Data)
                {
                    if (cacheResult.Data.Length < 1)
                    {
                        throw new InvalidOperationException($"{typeName} returned cache entry with zero bytes");
                    }
                    SetCachingHeaders(context, cacheHashWeakValidation);
                    await ProxyToStream(cacheResult.Data, context.Response).ConfigureAwait(true);
                }
                // logger?.LogDebug("Serving from {CacheName} {VirtualPath}?{CommandString}", typeName, plan., plan.RewrittenQuerystring);
            }
            else
            {
                // TODO explore this failure path better
                throw new NullReferenceException("Caching failed: " + cacheResult.Status);
            }
        }
        
        
         // ReSharper disable once UnusedMember.Global
        // public async Task Invoke(HttpContext context)
        // {
        //     // For instrumentation
        //     // globalInfoProvider.CopyHttpContextInfo(context);
        //     //
        //     // var path = context.Request.Path;
        //     //
        //     //
        //     // // Delegate to the diagnostics page if it is requested
        //     // if (DiagnosticsPage.MatchesPath(path.Value))
        //     // {
        //     //     await diagnosticsPage.Invoke(context);
        //     //     return;
        //     // }
        //     // // Delegate to licenses page if requested
        //     // if (licensePage.MatchesPath(path.Value))
        //     // {
        //     //     await licensePage.Invoke(context);
        //     //     return;
        //     // }
        //     //
        //     // // Respond to /imageflow.ready
        //     // if ( "/imageflow.ready".Equals(path.Value, StringComparison.Ordinal))
        //     // {
        //     //     options.Licensing.FireHeartbeat();
        //     //     using (new JobContext())
        //     //     {
        //     //         await StringResponseNoCache(context, 200, "Imageflow.Server is ready to accept requests.");
        //     //     }
        //     //     return;
        //     // }
        //     //
        //     // // Respond to /imageflow.health
        //     // if ( "/imageflow.health".Equals(path.Value, StringComparison.Ordinal))
        //     // {
        //     //     options.Licensing.FireHeartbeat();
        //     //     await StringResponseNoCache(context, 200, "Imageflow.Server is healthy.");
        //     //     return;
        //     // }
        //     
        //
        //     // We only handle requests with an image extension or if we configured a path prefix for which to handle
        //     // extensionless requests
        //     
        //     if (!ImageJobInfo.ShouldHandleRequest(context, options))
        //     {
        //         await next.Invoke(context);
        //         return;
        //     }
        //     //
        //     // options.Licensing.FireHeartbeat();
        //     
        //     var imageJobInfo = new ImageJobInfo(context, options, blobProvider);
        //
        //     // if (!imageJobInfo.Authorized)
        //     // {
        //     //     await NotAuthorized(context, imageJobInfo.AuthorizedMessage);
        //     //     return;
        //     // }
        //     //
        //     // if (imageJobInfo.LicenseError)
        //     // {
        //     //     if (options.EnforcementMethod == EnforceLicenseWith.Http422Error)
        //     //     {
        //     //         await StringResponseNoCache(context, 422, options.Licensing.InvalidLicenseMessage);
        //     //         return;
        //     //     }
        //     //     if (options.EnforcementMethod == EnforceLicenseWith.Http402Error)
        //     //     {
        //     //         await StringResponseNoCache(context, 402, options.Licensing.InvalidLicenseMessage);
        //     //         return;
        //     //     }
        //     // }
        //
        //     // If the file is definitely missing hand to the next middleware
        //     // Remote providers will fail late rather than make 2 requests
        //     if (!imageJobInfo.PrimaryBlobMayExist())
        //     {
        //         await next.Invoke(context);
        //         return;
        //     }
        //     
        //     string cacheKey = null;
        //     var cachingPath = imageJobInfo.NeedsCaching() ? options.ActiveCacheBackend : CacheBackend.NoCache;
        //     if (cachingPath != CacheBackend.NoCache)
        //     {
        //         cacheKey = await imageJobInfo.GetFastCacheKey();
        //
        //         if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) && cacheKey == etag)
        //         {
        //             GlobalPerf.Singleton.IncrementCounter("etag_hit");
        //             context.Response.StatusCode = StatusCodes.Status304NotModified;
        //             context.Response.ContentLength = 0;
        //             context.Response.ContentType = null;
        //             return;
        //         }
        //         GlobalPerf.Singleton.IncrementCounter("etag_miss");
        //     }
        //
        //     try
        //     {
        //         switch (cachingPath)
        //         {
        //             case CacheBackend.ClassicDiskCache:
        //                 await ProcessWithDiskCache(context, cacheKey, imageJobInfo);
        //                 break;
        //             case CacheBackend.NoCache:
        //                 await ProcessWithNoCache(context, imageJobInfo);
        //                 break;
        //             case CacheBackend.StreamCache:
        //                 await ProcessWithStreamCache(context, cacheKey, imageJobInfo);
        //                 break;
        //             default:
        //                 throw new ArgumentOutOfRangeException();
        //         }
        //         
        //         GlobalPerf.Singleton.IncrementCounter("middleware_ok");
        //     }
        //     catch (BlobMissingException e)
        //     {
        //         await NotFound(context, e);
        //     }
        //     catch (Exception e)
        //     {
        //         var errorName = e.GetType().Name;
        //         var errorCounter = "middleware_" + errorName;
        //         GlobalPerf.Singleton.IncrementCounter(errorCounter);
        //         GlobalPerf.Singleton.IncrementCounter("middleware_errors");
        //         throw;
        //     }
        //     finally
        //     {
        //         // Increment counter for type of file served
        //         var imageExtension = PathHelpers.GetImageExtensionFromContentType(context.Response.ContentType);
        //         if (imageExtension != null)
        //         {
        //             GlobalPerf.Singleton.IncrementCounter("module_response_ext_" + imageExtension);
        //         }
        //     }
        // }

    }
}