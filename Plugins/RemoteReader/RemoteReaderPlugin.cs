using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.Configuration;
using System.Net;
using ImageResizer.Util;
using System.Security.Cryptography;
using ImageResizer.Configuration.Issues;
using System.IO;
using ImageResizer.Resizing;
using System.Web;
using ImageResizer.Configuration.Xml;
using ImageResizer.ExtensionMethods;
using System.Text.RegularExpressions;

namespace ImageResizer.Plugins.RemoteReader {

    public class RemoteReaderPlugin : BuilderExtension, IPlugin, IVirtualImageProvider, IIssueProvider, IRedactDiagnostics {

        public Configuration.Xml.Node RedactFrom(Node resizer) {
            if (resizer != null && resizer.queryFirst("remoteReader") != null) resizer.setAttr("remoteReader.signingKey", "[redacted]");

            return resizer;
        }

        private static string base64UrlKey = "urlb64";

        public static string Base64UrlKey {
            get { return RemoteReaderPlugin.base64UrlKey; }
        }
        private static string hmacKey = "hmac";

        public static string HmacKey {
            get { return RemoteReaderPlugin.hmacKey; }
        }

        /// <summary>
        /// How many redirects to follow before throwing an exception. Defaults to 5.
        /// </summary>
        public int AllowedRedirects { get; set; }

        protected string remotePrefix = "~/remote";
        Config c;
        public RemoteReaderPlugin() {
            AllowedRedirects = 5;
            try {
                remotePrefix = Util.PathUtils.ResolveAppRelativeAssumeAppRelative(remotePrefix);
                //Remote prefix must never end in a slash - remote.jpg syntax...
            } catch { }
        }

        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;
            AllowedRedirects = c.get("remoteReader.allowedRedirects",AllowedRedirects);

            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(IHttpModule sender, HttpContext context) {
            if (IsRemotePath(c.Pipeline.PreRewritePath)) c.Pipeline.SkipFileTypeCheck = true;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return true;
        }

        /// <summary>
        /// Allows .Build and .LoadImage to resize remote URLs
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        /// <param name="disposeStream"></param>
        /// <param name="path"></param>
        /// <param name="restoreStreamPosition"></param>
        /// <returns></returns>
        protected override Stream GetStream(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition) {
            //Turn remote URLs into URI instances
            if (source is string && (((string)source).StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                    ((string)source).StartsWith("https://", StringComparison.OrdinalIgnoreCase))) {
                if (Uri.IsWellFormedUriString((string)source, UriKind.Absolute))
                    source = new Uri((string)source);

            }
            restoreStreamPosition = false;
            path = null;
            //Turn URI instances into streams
            if (source is Uri) {
                using (var s = GetUriStream(source as Uri))
                {
                    return s.CopyToMemoryStream();
                }
            }
            return null;
        }



        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            //Set the XXX of /remote.XXX to the real extension used by the remote file.
            //Allows the output extension and mime-type default to be determined correctly
            if (IsRemotePath(e.VirtualPath) &&
                    !string.IsNullOrEmpty(e.QueryString[Base64UrlKey])) {
                string ext = PathUtils.GetExtension(PathUtils.FromBase64UToString(e.QueryString[Base64UrlKey]));
                if (!string.IsNullOrEmpty(ext) && c.Pipeline.IsAcceptedImageType("." + ext))
                    e.VirtualPath = PathUtils.SetExtension(e.VirtualPath, ext);
            }
        }

        void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            if (IsRemotePath(e.VirtualPath)) {
                //Force images to be processed - don't allow them to only cache it.
                e.QueryString["process"] = ProcessWhen.Always.ToString().ToLowerInvariant();
            }
        }


        /// <summary>
        /// Returns the currently registered RemoteReaderPlugin, or adds a new RemoteReaderPlugin automatically if one is not registered.
        /// 
        /// </summary>
        public static RemoteReaderPlugin Current {
            get {
                return Config.Current.Plugins.GetOrInstall<RemoteReaderPlugin>();
            }
        }

        /// <summary>
        /// Generates a signed domain-relative URL in the form "/app/remote.jpg.ashx?width=200&amp;urlb64=aHnSh3haSh...&amp;hmac=913f3KJGK3hj"
        /// </summary>
        /// <param name="remoteUrl"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public string CreateSignedUrl(string remoteUrl, NameValueCollection settings) {
            settings[Base64UrlKey] = PathUtils.ToBase64U(remoteUrl);
            settings[HmacKey] = SignData(settings[Base64UrlKey]);
            return remotePrefix + ".jpg.ashx" + PathUtils.BuildQueryString(settings);
        }

        public string CreateSignedUrl(string remoteUrl, string settings) {
            return CreateSignedUrl(remoteUrl, new ResizeSettings(settings));
        }

        public string CreateSignedUrlWithKey(string remoteUrl, string settings, string key) {
            NameValueCollection s = new ResizeSettings(settings);
            s[Base64UrlKey] = PathUtils.ToBase64U(remoteUrl);
            s[HmacKey] = SignDataWithKey(s[Base64UrlKey],key);
            return remotePrefix + ".jpg.ashx" + PathUtils.BuildQueryString(s);
        }

        public string SignData(string data) {

            string key = c.get("remoteReader.signingKey", String.Empty);
            if (string.IsNullOrEmpty(key)) throw new ImageResizer.ImageProcessingException("You are required to set a passphrase for securing remote URLs. <resizer><remotereader signingKey=\"put a long and randam passphrase here\" /> </resizer>");
            return SignDataWithKey(data, key);
        }
        public string SignDataWithKey(string data, string key) {

            HMACSHA256 hmac = new HMACSHA256(UTF8Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(UTF8Encoding.UTF8.GetBytes(data));
            //32-byte hash is a bit overkill. Truncation doesn't weaking the integrity of the algorithm.
            byte[] shorterHash = new byte[8];
            Array.Copy(hash, shorterHash, 8);
            return PathUtils.ToBase64U(shorterHash);
        }
        /// <summary>
        /// Parses the specified path and querystring. Verifies the hmac signature for querystring specified paths, parses the
        /// human-friendly syntax for that syntax. Verifies the URL is properly formed. Returns an object containing the remote URL,
        /// querystring remainder, and a flag stating whether the request was signed or not. Incorrectly signed requests immediately throw an exception.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public RemoteRequestEventArgs ParseRequest(string virtualPath, NameValueCollection query) {
            query = new NameValueCollection(query);
            if (!IsRemotePath(virtualPath)) return null;

            RemoteRequestEventArgs args = new RemoteRequestEventArgs();
            args.SignedRequest = false;
            args.QueryString = query;

            if (!string.IsNullOrEmpty(query[Base64UrlKey])) {
                string data = query[Base64UrlKey];
                string hmac = query[HmacKey];
                query.Remove(Base64UrlKey);
                query.Remove(HmacKey);
                if (String.IsNullOrEmpty(hmac))
                {
                    args.SignedRequest = false;
                    args.DenyRequest = true;
                }
                else if (SignData(data).Equals(hmac))
                {
                    args.SignedRequest = true;
                }
                else
                {
                    throw new ImageProcessingException("Invalid request! This request was not properly signed, or has been tampered with since transmission.");
                }
                args.RemoteUrl = PathUtils.FromBase64UToString(data);
            } else {
                args.RemoteUrl = "http://" + ReplaceInLeadingSegment(virtualPath.Substring(remotePrefix.Length).TrimStart('/', '\\'), "_", ".");
                args.RemoteUrl = Uri.EscapeUriString(args.RemoteUrl);
            }
            if (!Uri.IsWellFormedUriString(args.RemoteUrl, UriKind.Absolute))
                throw new ImageProcessingException("Invalid request! The specified Uri is invalid: " + args.RemoteUrl);
            return args;
        }

        private string ReplaceInLeadingSegment(string path, string from, string to) {
            int firstslash = path.IndexOf('/',1);
            if (firstslash < 0) firstslash = path.Length; //If no forward slashes, edit whole string.

            return path.Substring(0, firstslash).Replace(from, to) + path.Substring(firstslash);
        }


        public bool IsRemotePath(string virtualPath) {
            return (virtualPath.Length > remotePrefix.Length && 
                virtualPath.StartsWith(remotePrefix, StringComparison.OrdinalIgnoreCase)
                && (virtualPath[remotePrefix.Length] == '.' || virtualPath[remotePrefix.Length] == '/'));
        }

        public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            return IsRemotePath(virtualPath);
        }

        /// <summary>
        /// Allows you to perform programmatic white or blacklisting of remote URLs
        /// </summary>
        public event RemoteRequest AllowRemoteRequest;

        private bool IsWhitelisted(RemoteRequestEventArgs request) {
            var rr = c.getNode("remotereader");
            var domain = new Uri(request.RemoteUrl).Host;
            if (rr == null || string.IsNullOrEmpty(domain)) return false;

            foreach (Node n in rr.childrenByName("allow")) {
                bool onlyWhenSigned = n.Attrs.Get("onlyWhenSigned", false);
                if (onlyWhenSigned && !request.SignedRequest) continue;

                bool hostMatches = false, regexMatches = false;
                string host = n.Attrs.Get("domain");
                if (!string.IsNullOrEmpty(host)) {
                    if (host.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
                        hostMatches = domain.EndsWith(host.Substring(1), StringComparison.OrdinalIgnoreCase);
                    else
                        hostMatches = domain.Equals(host, StringComparison.OrdinalIgnoreCase);

                    if (!hostMatches) continue;//If any filter doesn't match, skip rule
                }

                string regex = n.Attrs.Get("regex");
                if (!string.IsNullOrEmpty(regex)) {
                    var r = new Regex("^" + regex + "$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    regexMatches = r.IsMatch(request.RemoteUrl);
                    if (!regexMatches) continue;//If any filter doesn't match, skip rule
                }
                //If all specified filters match, allow the request. 
                //This *is* supposed to be || not &&, because we already deal with non-matching filters via 'continue'.
                if (hostMatches || regexMatches) return true;

            }
            return false;
        }

        public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            RemoteRequestEventArgs request = ParseRequest(virtualPath, queryString);
            if (request == null) throw new FileNotFoundException();

            if (request.SignedRequest && c.get("remotereader.allowAllSignedRequests", false)) request.DenyRequest = false;

            //Check the whitelist
            if (request.DenyRequest && IsWhitelisted(request)) request.DenyRequest = false;
     
            //Fire event
            if (AllowRemoteRequest != null) AllowRemoteRequest(this, request);

            if (request.DenyRequest) throw new ImageProcessingException(403, "The specified remote URL is not permitted.");

            return new RemoteSiteFile(virtualPath, request, this);
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            string key = c.get("remoteReader.signingKey", String.Empty);
            if (string.IsNullOrEmpty(key))
                issues.Add(new Issue("You are required to set a passphrase for securing remote URLs. Example: <resizer><remotereader signingKey=\"put a long and randam passphrase here\" /> </resizer>"));
            return issues;

        }

        /// <summary>
        /// Returns a stream of the HTTP response to the specified URL with a 15 second timeout. 
        /// Throws a FileNotFoundException instead of a WebException for 404 errors.
        /// Can throw a variety of exceptions: ProtocolViolationException, WebException,  FileNotFoundException,
        /// SecurityException, NotSupportedException?, and InvalidOperationException?.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="maxRedirects"></param>
        /// <returns></returns>
        public Stream GetUriStream(Uri uri, int maxRedirects = -1) {
            if (maxRedirects == -1) maxRedirects = AllowedRedirects;

            HttpWebResponse response = null;
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Timeout = 15000; //Default to 15 seconds. Browser timeout is usually 30.
                
                //This is IDisposable, but only disposes the stream we are returning. So we can't dispose it, and don't need to
                response = request.GetResponse() as HttpWebResponse;
                return response.GetResponseStream();
            } catch (WebException e) {
                var resp = e.Response as HttpWebResponse;

                if (e.Status == WebExceptionStatus.ProtocolError && resp != null) {
                    if (resp.StatusCode == HttpStatusCode.NotFound) throw new FileNotFoundException("404 error: \"" + uri.ToString() + "\" not found.", e);

                    if (resp.StatusCode == HttpStatusCode.Forbidden) throw new HttpException(403, "403 Not Authorized (from remote server) for : \"" + uri.ToString() + "\".", e);
                    if (resp.StatusCode == HttpStatusCode.Moved ||
                        resp.StatusCode == HttpStatusCode.Redirect) {
                            if (maxRedirects < 1) throw new HttpException(500, "Too many redirects, stopped while looking for \"" + uri.ToString() + "\".");
                            string loc = resp.GetResponseHeader("Location");
                            if (!string.IsNullOrEmpty(loc) && Uri.IsWellFormedUriString(loc, UriKind.RelativeOrAbsolute)) {
                                Uri newLoc = Uri.IsWellFormedUriString(loc, UriKind.Absolute) ? new Uri(loc) : new Uri(uri, new Uri(loc));
                                response.Close(); response = null; 
                                return GetUriStream(newLoc, maxRedirects - 1);
                            }
                    }
                }
                //if (resp != null)resp.Close();
                if (response != null) response.Close();
                throw e;
            } 
        }
    }

    public class RemoteSiteFile : IVirtualFile, IVirtualFileSourceCacheKey {

        protected string virtualPath;
        protected RemoteReaderPlugin parent;
        private RemoteRequestEventArgs request;

        public RemoteSiteFile(string virtualPath, RemoteRequestEventArgs request, RemoteReaderPlugin parent) {
            this.virtualPath = virtualPath;
            this.request = request;
            this.parent = parent;
        }

        public string VirtualPath {
            get { return virtualPath; }
        }

        public System.IO.Stream Open() {
            using (var s = parent.GetUriStream(new Uri(this.request.RemoteUrl)))
            {
                return s.CopyToMemoryStream();
            }
        }

        public string GetCacheKey(bool includeModifiedDate) {
            return this.request.RemoteUrl;
        }
    }
}
