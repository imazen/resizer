using System;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;
using System.Drawing;
using fbs.ImageResizer;
using fbs;

namespace SampleProject
{
   
    public class HandlerTest : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            /*
             * This is not a good approach to take!!! 
             * Don't copy and paste this.  This is just a test of NonSeekableStream.
             * 
             * A proper implementation should:
             * Set caching headers.
             * Disk cache resizing results
             * Should be implemented as an HttpModule, not an IHttpHandler, and let the static file handler serve the resulting image from disk.
             * Implementing a HTTP handler that supports range requests, 401, 200, etc, is very complicated.
             * Read "Image Resizing Pitfalls" on nathanaeljones.com for more reasons why a handler is not scalable as a solution (such as memory saturation)
             */

            
            NameValueCollection queryString = new yrl("?maxwidth=80&maxheight=80&format=png").QueryString;
            string sourceFile = new yrl("grass.jpg").Local; //Physical path


            //Determines output format, includes code for saving in a variety of formats.  
            ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromPhysicalPath(sourceFile), queryString);
            //Sets the content type - required or you will have problems with IE and certain other browsers when you switch formats (and the extension is different).
            context.Response.ContentType = ios.GetContentType();

            //Write image
            using (Bitmap img = ImageManager.getBestInstance().BuildImage(sourceFile, queryString))
            {
                ios.SaveImageToNonSeekableStream(context.Response.OutputStream, img);
            } 

            
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
