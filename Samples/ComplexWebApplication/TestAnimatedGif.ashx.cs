using System;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer;
using System.Net;
using System.IO;


namespace SampleProject
{
   
    public class TestAnimatedGif : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            WebRequest request = WebRequest.Create("http://images.imageresizing.net/2_computers.gif");
            WebResponse response = request.GetResponse();

            using(Stream input = response.GetResponseStream())
            {
                context.Response.ContentType = "image/gif";
                ImageBuilder.Current.Build(input, context.Response.OutputStream, new ResizeSettings("?width=50")); 
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
