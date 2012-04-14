using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;

namespace ImageResizer {
    public class UrlOptions : NameValueCollection {


        /// <summary>
        /// If true, the querystring will be generated using the semicolon syntax (;key=value;key2=value) instead of the standard syntax (?key=value&amp;key2=value)
        /// The host server will need to have the CloudFront plugin installed. 
        /// </summary>
        public bool Semicolons { get { return this.Get<bool>("semicolons", false); }set{this.Set<bool>("semicolons",value); }

        //Host - cloud front or image server
        //Semicolons 
        //PathKind - RemoteImage, WebsiteScreenshot, Gradient,

        /*
         * 
MongoReader - ?
RemoteReader - Encode and sign remote URL
ScreenCapture - Encode and sign remote URL
Gradient - 
S3Reader
SqlReader
AzureReader

CloudFront - Set server, use semicolons
Sign - Sign path and/or querystring
Compress - compress path and querystring
Encrypt - Encrypt path and  querystring


*/

    }
}
