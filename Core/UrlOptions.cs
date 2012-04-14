using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;

namespace ImageResizer {
    public class UrlOptions : NameValueCollection {


        /// <summary>
        /// If set, the named UrlBuilder configuration will be loaded from Web/App.config
        /// </summary>
        public string Config { get { return this.Get("config"); } set { this.SetAsString<string>("config", value); } }


        /// <summary>
        /// If true, the querystring will be generated using the semicolon syntax (;key=value;key2=value) instead of the standard syntax (?key=value&amp;key2=value). 
        /// This must be done if you're using Amazon CloudFront, as it strips querystrings off URLs. 
        /// The imaging server will need to have the CloudFront plugin installed. 
        /// </summary>
        public bool? Semicolons { get { return this.Get<bool>("semicolons"); }set{this.Set<bool>("semicolons",value); }}

        /// <summary>
        /// If true, unspecified settings will be determined based on the plugin configuration from the local application
        /// </summary>
        public bool? InheritFromLocal { get { return this.Get<bool>("inheritFromLocal"); } set { this.Set<bool>("inheritFromLocal", value); } }

        /// <summary>
        /// The ApplicationVirtualPath to use when resolving tildes (app-relative virtual paths)
        /// </summary>
        public string AppPath { get { return this.Get("appPath"); } set { this.SetAsString<string>("appPath", value); } }

        /// <summary>
        /// The fake extension suffix to use when generating image URLs. Usually ".ashx". 
        /// Not required if the target server is running IIS7 Integrated mode or has performed a wildcard or *.jpg, *.jpeg, *.png, *.gif, etc. mappings to the ASP.NET runtime.
        /// See http://imageresizing.net/docs for instructions. 
        /// </summary>
        public string FakeExtension { get { return this.Get("fakeExtension"); } set { this.SetAsString<string>("fakeExtension", value); } }

        /// <summary>
        /// Defaults to ~/remote.jpg.ashx. The handler path to use when generating URLs for the RemoteReader plugin.
        /// </summary>
        public string RemoteReaderPrefix { get { this.Normalize("remoteReaderPrefix","remotePrefix"); return this.Get("remoteReaderPrefix"); } set { this.SetAsString<string>("remoteReaderPrefix", value); } }

        /// <summary>
        /// The signing key to use when building the remote image url. You don't need this when generating local URLs, it will be located for you.
        /// </summary>
        public string RemoteReaderKey { get { return this.Get("remoteReaderKey"); } set { this.SetAsString<string>("remoteReaderKey", value); } }



        /// <summary>
        /// Usually ~/images/enc/. The prefix to use when generating URLs for the Encrypted plugin.
        /// </summary>
        public string EncryptionPrefix { get { return this.Get("encryptionPrefix"); } set { this.SetAsString<string>("encryptionPrefix", value); } }

        /// <summary>
        /// The encryption key to use when encrypting an image url. You don't need this when generating local URLs, it will be located for you based on EncryptionPrefix.
        /// </summary>
        public string EncryptionKey { get { return this.Get("encryptionKey"); } set { this.SetAsString<string>("encryptionKey", value); } }


        /// <summary>
        /// Usually ~/s3/. The prefix to use when generating URLs for the S3Reader plugin. You can modify this to include the bucket if your image URLs lack the bucket prefix.
        /// </summary>
        public string S3Prefix { get { return this.Get("s3Prefix"); } set { this.SetAsString<string>("s3Prefix", value); } }

        /// <summary>
        /// Usually ~/databaseimages/ or ~/sql. The prefix to use when generating URLs for the SqlReader plugin.
        /// </summary>
        public string SqlPrefix { get { return this.Get("sqlPrefix"); } set { this.SetAsString<string>("s3Prefix", value); } }


        
        /// <summary>
        /// Defaults to ~/. 
        /// </summary>
        public string FilePrefix { get { return this.Get("filePrefix"); } set { this.SetAsString<string>("filePrefix", value); } }

        /// <summary>
        /// If null, will be guessed based on which plugin-specific settings are defined. Valid values are "file", "s3", "remote", "sql", "azure", and "mongo". 
        /// </summary>
        public string PathKind { get { return this.Get("pathKind"); } set { this.SetAsString<string>("pathKind", value); } }

        /// <summary>
        /// If null, the scheme from the current (parent) request will be used. This can be unreliable if proxies are involved, so ensure you set it manually if in doubt.
        /// Valid values are "http" and "https".
        /// </summary>
        public string Scheme { get { return this.Get("scheme"); } set { this.SetAsString<string>("scheme", value); } }
       
        /// <summary>
        /// The name of the image server or cloud front distribution to use when generating the URL. Must contain the port information if required. 
        /// Ex. "images.imageresizing.net" or "localhost:5093". Must not include the scheme. If Host and Scheme are null, a domain-relative path will be used.
        /// </summary>
        public string Host { get { return this.Get("host"); } set { this.SetAsString<string>("host", value); } }

       
        /*
         
        // AzureReader
        // MongoDB - relative path OR string id, + Image extension
        */

    }
}
