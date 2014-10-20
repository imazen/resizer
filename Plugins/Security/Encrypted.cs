using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.Util;
using ImageResizer.Configuration;
using System.Security.Cryptography;
using System.IO;
using System.Web.Security;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins.Security;
using System.Diagnostics;
using ImageResizer.Plugins.Security.Cryptography;

namespace ImageResizer.Plugins.Encrypted {
    public class EncryptedPlugin:IssueSink, IPlugin, IMultiInstancePlugin {


        public static EncryptedPlugin First {
            get {
                Config.Current.Plugins.LoadPlugins();
                return Config.Current.Plugins.Get<EncryptedPlugin>();
            }
        }


        public EncryptedPlugin()
            : base("Encrypted plugin") {
                VirtualPrefix = VirtualPrefix;
        }

        private string _virtualPrefix = "~/images/enc/";
        /// <summary>
        /// Requests starting with this path will be decrypted. Should be in app-relative form: "~/s3/". Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
        /// </summary>
        public string VirtualPrefix {
            get { return _virtualPrefix; }
            set { if (!value.EndsWith("/")) value += "/"; _virtualPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value); }
        }

        private byte[] _encryptionKey = null;
       

        private SimpleSecureEncryption _enc;

        protected SimpleSecureEncryption Enc { get {
            if (_enc == null) _enc = new SimpleSecureEncryption(new AppDataKeyProvider().GetKey("EncryptedPlugin",32));
            return _enc; } }

        public EncryptedPlugin(string prefix, string key)
            : base("Encrypted plugin") {
            VirtualPrefix = prefix;
            _encryptionKey = UTF8Encoding.UTF8.GetBytes(key);
        }

        public EncryptedPlugin(NameValueCollection args):base("Encrypted plugin") {
            if (!string.IsNullOrEmpty(args["prefix"])) VirtualPrefix = args["prefix"];
            else VirtualPrefix = VirtualPrefix;

            if (!string.IsNullOrEmpty(args["key"])) {
                _encryptionKey = UTF8Encoding.UTF8.GetBytes(args["key"]); 
            }

            if (_encryptionKey == null || _encryptionKey.Length < 16) this.AcceptIssue(new Issue("Please specify an encryption key that is at least 16 characters and optimally 32.", IssueSeverity.Critical));

            _enc = new SimpleSecureEncryption(_encryptionKey);
        }

        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            this.c.Plugins.add_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            return this;
        }


        public string EncryptPathAndQuery(string virtualPath, NameValueCollection query) {
            return EncryptPathAndQuery(virtualPath + PathUtils.BuildQueryString(query));
        }
        public string EncryptPathAndQuery(string virtualPathAndQuery) {
            
            if (virtualPathAndQuery.StartsWith(PathUtils.AppVirtualPath)){
                virtualPathAndQuery = "~/" + virtualPathAndQuery.Substring(PathUtils.AppVirtualPath.Length).TrimStart('/');
            }

            if (!virtualPathAndQuery.StartsWith("~/")) throw new ArgumentException();


            return VirtualPrefix.TrimEnd('/') + '/' + Encrypt(virtualPathAndQuery.Substring(1).TrimStart('/')) + ".ashx";

        }

        private string Encrypt(string text) {
            byte[] iv;
            byte[] data = Enc.Encrypt(UTF8Encoding.UTF8.GetBytes(text), out iv);
            return PathUtils.ToBase64U(iv) + '/' + PathUtils.ToBase64U(data);
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            if (!c.Pipeline.PreRewritePath.StartsWith(VirtualPrefix, StringComparison.OrdinalIgnoreCase)) return;
            //Okay, decrypt
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string both = c.Pipeline.PreRewritePath.Substring(VirtualPrefix.Length); //Strip prefix
            string[] parts = both.Split('/'); //Split

            if (parts.Length != 2) return; //There must be exactly two parts

            parts[1] = PathUtils.RemoveFullExtension(parts[1]); //Remove the .ashx or .jpg.ashx or whatever it is.

            byte[] iv = PathUtils.FromBase64UToBytes(parts[0]);
            if (iv.Length != 16) return; //16-byte IV required
            byte[] data = PathUtils.FromBase64UToBytes(parts[1]);

            string result = UTF8Encoding.UTF8.GetString(Enc.Decrypt(data, iv));

            string path;
            string fragment;
            //We do not merge the old and new query strings. We do not accept plaintext additions to an encrypted URL
            c.Pipeline.ModifiedQueryString = PathUtils.ParseQueryString(result, true, out path, out fragment);
            c.Pipeline.PreRewritePath = path;
            sw.Stop();
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }



    }


}
