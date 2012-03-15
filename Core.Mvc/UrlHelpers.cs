using System;
using System.Web.Mvc;
using System.Collections.Specialized;


namespace ImageResizer{

    public static class ImageResizerUrlHelpers {

        //Sources - MongoDB, S3Reader, SqlReader, Gradient, RemoteReader
        // RemoteReader - URL
        // S3Reader - relative path
        // SqlReader - Object and Image Extension
        // Gradient - 2 colors, angle, and width
        // MongoDB - relative path OR string id, + Image extension


        public static string Image(this UrlHelper helper, string imageFileName) {
            return Image(helper, imageFileName, null);
        }

        public static string Image(this UrlHelper helper, string imageFileName, NameValueCollection resizeCommands) {
            // 'imageBasePath' should be replaced with centralized logic from ImageResizer
            // In my case I was using the S3 Reader plugin so this is hard-coded (for the time being)
            const string imageBasePath = "/s3/bucketname/";
            

            if (string.IsNullOrEmpty(imageFileName))
                throw new ArgumentNullException("imageFileName");

            var parameters = (resizeCommands != null) ? "?" + resizeCommands : string.Empty;

            return imageBasePath + imageFileName + parameters;
        }
    }
}
