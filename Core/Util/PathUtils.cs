using System.Web.Hosting;
using System;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ImageResizer.ExtensionMethods;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace ImageResizer.Util {
    /// <summary>
    /// A set of utility methods for manipulating virtual paths
    /// </summary>
    public class PathUtils {

        /// <summary>
        /// Returns HostingEnvironment.ApplicationVirtualPath or "/", if asp.net is not running
        /// </summary>
        public static string AppVirtualPath {
            get {
                return HostingEnvironment.ApplicationVirtualPath != null ? HostingEnvironment.ApplicationVirtualPath : "/";
            }
        }
        /// <summary>
        /// If not running in ASP.NET, returns the folder containing the DLL.
        /// </summary>
        public static string AppPhysicalPath {
            get {
                return HostingEnvironment.ApplicationPhysicalPath != null ? HostingEnvironment.ApplicationPhysicalPath : Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            }
        }

        private static readonly char[] QueryOrFragment = new char[] { '?', '#' };
        private static readonly char[] SpaceOrSlash = new char[] { ' ', '/', '\\' };
        /// <summary>
        /// Should be called SetFullExtension.
        /// Sets the file extension of the specified path to the specified value, returning the result.
        /// If an extension has multiple parts, it will replace all of them.
        /// Leading dots will be stripped from 'newExtension' and re-addd as required.
        /// The querystring and fragment is maintained as-is. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newExtension"></param>
        /// <returns></returns>
        public static string SetExtension(string path, string newExtension) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            //Finds the first character that could possibly be part of the extension (before the query)
            int firstPossibleExtensionChar = path.LastIndexOfAny(SpaceOrSlash, query - 1) + 1;
            int extensionStarts = path.IndexOf('.', firstPossibleExtensionChar, query - firstPossibleExtensionChar);
            if (extensionStarts < 0) extensionStarts = query;

            return path.Substring(0, extensionStarts) + (!string.IsNullOrEmpty(newExtension) ? "." + newExtension.TrimStart('.') : "") + path.Substring(query);

        }

        /// <summary>
        /// Removes all extension segments from the filename or URL, leaving the querystring intact. I.e, image.jpg.bmp.tiff?hi would be image?hi
        /// The querystring and fragment is maintained as-is. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RemoveFullExtension(string path) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            //Finds the first character that could possibly be part of the extension (before the query)
            int firstPossibleExtensionChar = path.LastIndexOfAny(SpaceOrSlash, query - 1) + 1;
            int extensionStarts = path.IndexOf('.', firstPossibleExtensionChar, query - firstPossibleExtensionChar);
            if (extensionStarts < 0) extensionStarts = query;

            return path.Substring(0, extensionStarts) + path.Substring(query);

        }

        /// <summary>
        /// Removes the extension from the filename or URL, leaving the querystring intact, where the extension is only the last extension segment.
        /// I.e, image.jpg.bmp.tiff?hi would be image.jpg.bmp?hi after this call.
        /// The querystring and fragment is maintained as-is. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RemoveExtension(string path) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            //Finds the first character that could possibly be part of the extension (before the query)
            int firstPossibleExtensionChar = path.LastIndexOfAny(SpaceOrSlash, query - 1) + 1;
            int extensionStarts = path.LastIndexOf('.', query - 1, query - firstPossibleExtensionChar);
            if (extensionStarts < 0) extensionStarts = query;

            return path.Substring(0, extensionStarts) + path.Substring(query);

        }

        /// <summary>
        /// Adds the specified extension to path, returning the result. Multiple calls will result in "path.ext.ext.ext.ext".
        /// The querystring and fragment is maintained as-is. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newExtension"></param>
        /// <returns></returns>
        public static string AddExtension(string path, string newExtension) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            return path.Substring(0, query) + "." + newExtension.TrimStart('.') + path.Substring(query);
        }
        /// <summary>
        /// Will return the full extension, like ".jpg.ashx", not just the last bit. 
        ///  Excludes extensions containing spaces or slashes.
        ///  The querystring and fragment is ignored. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFullExtension(string path) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            //Finds the first character that could possibly be part of the extension (before the query)
            int firstPossibleExtensionChar = path.LastIndexOfAny(SpaceOrSlash, query - 1) + 1;
            int extensionStarts = path.IndexOf('.', firstPossibleExtensionChar, query - firstPossibleExtensionChar);
            if (extensionStarts < 0) extensionStarts = query;

            return path.Substring(extensionStarts, query - extensionStarts);

        }
        /// <summary>
        /// Grabs the last segment of the filename extension. Returns an empty string if there is no extension, or if the extension contains a space.
        /// Includes the leading '.'
        /// The querystring and fragment is ignored. Semicolon syntax not supported.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetExtension(string path) {
            int query = path.IndexOfAny(QueryOrFragment);
            if (query < 0) query = path.Length;
            //Finds the first character that could possibly be part of the extension (before the query)
            int firstPossibleExtensionChar = path.LastIndexOfAny(SpaceOrSlash, query - 1) + 1;
            int extensionStarts = path.LastIndexOf('.', query -1, query - firstPossibleExtensionChar );
            if (extensionStarts < 0) extensionStarts = query;

            return path.Substring(extensionStarts, query - extensionStarts);

        }
        /// <summary>
        /// Resolves app-relative paths to virtual paths. Does nothing with virtual or relative paths.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static string ResolveAppRelative(string virtualPath) {
            //resolve tilde
            if (virtualPath.StartsWith("~", StringComparison.OrdinalIgnoreCase))
                return HostingEnvironment.ApplicationVirtualPath != null
                    ? HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/' + virtualPath.TrimStart('~', '/')
                    : '/' + virtualPath.TrimStart('~', '/');
            return virtualPath;
        }

        /// <summary>
        /// Turns relative paths into domain-relative paths.
        /// Turns app-relative paths into domain relative paths.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public static string ResolveAppRelativeAssumeAppRelative(string virtualPath) {
            string applicationVirtualPath = HostingEnvironment.ApplicationVirtualPath ?? string.Empty;
            if (virtualPath.StartsWith("~"))
                return applicationVirtualPath.TrimEnd('/') + "/" + virtualPath.TrimStart('~', '/');
            if (!virtualPath.StartsWith("/"))
                return applicationVirtualPath.TrimEnd('/') + "/" + virtualPath;
            return virtualPath;
        }

        /// <summary>
        /// Joins the path and querystring. If the path already contains a querystring, they are 'append joined' with the correct character. Fragment is maintained as-is. 
        /// Does not support the semicolon syntax. 
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="querystring"></param>
        /// <returns></returns>
        public static string AddQueryString(string virtualPath, string querystring) {
            //Separate the fragment if it's present, and restore it later
            int fragment = virtualPath.IndexOf('#');
            string suffix = "";
            if (fragment < 0) fragment = virtualPath.Length;
            else {
                suffix = virtualPath.Substring(fragment);
                virtualPath = virtualPath.Substring(0, fragment);
            }
            virtualPath = virtualPath.TrimEnd('?');
            if (virtualPath.IndexOf('?') > -1) virtualPath = virtualPath.TrimEnd('&') + '&';
            else virtualPath += '?';

            return virtualPath + querystring.TrimStart('&', '?') + suffix;
        }

        /// <summary>
        /// Removes the query string from the specifed path. If the path is only a querystring, an empty string is returned. Does not support the semicolon syntax.  Fragment is maintained as-is.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RemoveQueryString(string path) {
            //Separate the fragment if it's present, and restore it later
            int fragment = path.IndexOf('#');
            string suffix = "";
            if (fragment < 0) fragment = path.Length;
            else {
                suffix = path.Substring(fragment);
                path = path.Substring(0, fragment);
            }

            int delimiter = path.IndexOf('?');
            return delimiter > -1 ? path.Substring(0, delimiter) + suffix : path;
        }


        /// <summary>
        /// Overwrites exisisting querystring values in 'path' with the values in 'newQuerystring'. Does not support the semicolon syntax. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newQuerystring"></param>
        /// <returns></returns>
        public static string MergeOverwriteQueryString(string path, NameValueCollection newQuerystring) {
            NameValueCollection oldQuery = ParseQueryString(path);
            //Overwrite old with new
            foreach (string key in newQuerystring.Keys)
                if (key != null) oldQuery[key] = newQuerystring[key];

            return AddQueryString(RemoveQueryString(path), BuildQueryString(oldQuery));
        }
        /// <summary>
        /// Adds the querystring values in 'newQuerystring' to the querystring in Path, but does not overwrite values. Does not support the semicolon syntax. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newQuerystring"></param>
        /// <returns></returns>
        public static string MergeQueryString(string path, NameValueCollection newQuerystring) {
            NameValueCollection oldQuery = ParseQueryString(path);
            //Overwrite new with old
            foreach (string key in oldQuery.Keys)
                if (key != null) newQuerystring[key] = oldQuery[key];

            return AddQueryString(RemoveQueryString(path), BuildQueryString(newQuerystring));
        }

        /// <summary>
        /// Returns a string querystring in the form "?key=value&amp;key=value".
        /// Keys and values are UrlEncoded as they should be.
        /// </summary>
        /// <param name="QueryString"></param>
        /// <returns></returns>
        public static string BuildQueryString(NameValueCollection QueryString) {
            return BuildQueryString(QueryString, true);
        }
        /// <summary>
        /// Returns a string querystring in the form "?key=value&amp;key=value".
        /// Keys and values are UrlEncoded if urlEncode=true.
        /// </summary>
        /// <param name="QueryString"></param>
        /// <param name="urlEncode"></param>
        /// <returns></returns>
        public static string BuildQueryString(NameValueCollection QueryString, bool urlEncode) {
            return BuildQueryString(QueryString, urlEncode, true, '?', '&', '=');
        }


        /// <summary>
        /// Returns a string querystring in the form ";key=value;key=value".
        /// Keys and values are UrlEncoded if urlEncode=true.
        /// </summary>
        /// <param name="QueryString"></param>
        /// <param name="urlEncode"></param>
        /// <returns></returns>
        public static string BuildSemicolonQueryString(NameValueCollection QueryString, bool urlEncode) {
            return BuildQueryString(QueryString, urlEncode, true, ';', ';', '=');
        }
        /// <summary>
        /// Build a customized querystring from a NameValueCollection
        /// </summary>
        /// <param name="QueryString"></param>
        /// <param name="urlEncode">True to URL encode all values</param>
        /// <param name="skipNullValues">If true, null-valued keys will be skipped</param>
        /// <param name="firstSeparator">Usually ? or ; </param>
        /// <param name="laterSeparators">Usually &amp; or ;</param>
        /// <param name="equals">Always =</param>
        /// <returns></returns>
        public static string BuildQueryString(NameValueCollection QueryString, bool urlEncode, bool skipNullValues = true, char firstSeparator = '?', char laterSeparators = '&', char equals = '=') {
            if (QueryString.Count < 1) return ""; //No keys? return empty string.

            StringBuilder path = new StringBuilder();
            path.Append(firstSeparator);
            foreach (string key in QueryString.Keys) {
                if (key == null) continue; //Skip null keys
                string value = QueryString[key];
                if (skipNullValues && value == null) continue; //Optionally skip null values
                path.Append(urlEncode ? HttpUtility.UrlEncode(key) : key);
                path.Append(equals);
                path.Append(urlEncode ? HttpUtility.UrlEncode(value) : value);
                path.Append(laterSeparators);
            }
            if (path[path.Length - 1] == laterSeparators) path.Remove(path.Length - 1, 1);

            return path.ToString();
        }



       

        /// <summary>
        /// Like ParseQueryString, but permits the leading '?' to be omitted. Does not support the semicolon syntax.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryStringFriendly(string path) {
            if (path.IndexOf('?') < 0 && path.IndexOf('=') >= 0) path = '?' + path;
            return ParseQueryString(path,false);
        }

        /// <summary>
        /// Like ParseQueryString, but permits the leading '?' to be omitted, and semicolons can be substituted for '&amp;'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryStringFriendlyAllowSemicolons(string path) {
            //Delete the fragment before we start
            int fragment = path.IndexOf('#');
            if (fragment > -1) path = path.Substring(0, fragment);

            int eq = path.IndexOf('=');
            int quest = path.IndexOf('?');
            int semi = path.IndexOf(';');
            if (eq >= 0 && //We have equals, which indicates a query
                quest < 0 && //But no question mark!
                (eq < semi || semi < 0)) //An no semicolon, or else a semicolon *after* the first equals.
                path = '?' + path;
            return ParseQueryString(path,true);
        }

        /// <summary>
        /// Parses the querystring from the given path into a NameValueCollection. 
        /// accepts "file?key=value" and "?key=value&amp;key2=value2" formats. (no path is required)
        /// UrlDecodes keys and values.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string path) {
            return ParseQueryString(path, false);
        }

        /// <summary>
        /// Parses the querystring from the given path into a NameValueCollection. 
        /// accepts "file?key=value" and "?key=value&amp;key2=value2" formats. (no path is required)
        /// UrlDecodes keys and values. Does not enforce correct syntax, I.E. '?key=value?key2=value2' is allowed. However, '&amp;key=value?key2=value' will only get key2 parsed. 
        /// When allowSemicolons is true, semicolon paths like ';key=value;key2=value2' are allowed, as are hybrid paths: ';key=value?key2=value2&amp;key3=value3'.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="allowSemicolons"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string path, bool allowSemicolons) {
            string s, f;
            return ParseQueryString(path, allowSemicolons, out s,out f);
        }

        /// <summary>
        /// Parses the querystring from the given path into a NameValueCollection. 
        /// accepts "file?key=value" and "?key=value&amp;key2=value2" formats. (no path is required)
        /// UrlDecodes keys and values. Does not enforce correct syntax, I.E. '?key=value?key2=value2' is allowed. However, '&amp;key=value?key2=value' will only get key2 parsed. 
        /// When allowSemicolons is true, semicolon paths like ';key=value;key2=value2' are allowed, as are hybrid paths: ';key=value?key2=value2&amp;key3=value3'.
        /// 
        /// Does NOT parse fragments correctly.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="allowSemicolons"></param>
        /// <param name="beforeQuery">Returns the portion of the 'path' before the querystring. May include the scheme, server, port, path and path info, depending upon what 'path' contained.</param>
        /// <param name="fragment"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string path, bool allowSemicolons, out string beforeQuery, out string fragment) {
            //Separate the fragment if it's present, and restore it later
            int frag = path.IndexOf('#');
            if (frag < 0) fragment = "";
            else {
                fragment = path.Substring(frag);
                path = path.Substring(0, frag);
            }


            int firstdelimiter = path.IndexOf('?');
            if (allowSemicolons) {
                //Use the index if whichever is first, and preset
                int firstsemicolon = path.IndexOf(';');
                if (firstdelimiter < 0 || (firstsemicolon >= 0 && firstsemicolon < firstdelimiter)) firstdelimiter = firstsemicolon;
            }
            if (firstdelimiter < 0 || firstdelimiter >= path.Length) {
                //No query string detected
                beforeQuery = path;
                return new NameValueCollection();
            } else {
                beforeQuery = path.Substring(0, firstdelimiter);
            }

            return ParseQueryOnly(path.Substring(firstdelimiter, path.Length - firstdelimiter),allowSemicolons);
        }

        /// <summary>
        /// Parses a querystring into a name/value collection. The given string cannot include path or fragment information - it must be *just* the querystring.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="allowSemicolons"></param>
        /// <param name="urlDecode"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryOnly(string query, bool allowSemicolons = true, bool urlDecode = true) {
            string[] pairs = query.Split(allowSemicolons ? new char[] { '?', '&', ';' } : new char[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
            NameValueCollection c = new NameValueCollection();
            foreach (string s in pairs) {
                string[] namevalue = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (namevalue.Length > 2) {
                    //Handle &key=value=value and &key===value=value -> key : "value=value"
                    string value = s.Substring(s.IndexOf('=')).TrimStart('=');
                    c[urlDecode ? HttpUtility.UrlDecode(namevalue[0]) : namevalue[0]] =
                        urlDecode ? HttpUtility.UrlDecode(value) : value;
                } else if (namevalue.Length == 2) {
                    //Handle key=value (normal)
                    c[urlDecode ? HttpUtility.UrlDecode(namevalue[0]) : namevalue[0]] =
                        urlDecode ? HttpUtility.UrlDecode(namevalue[1]) : namevalue[1];
                } else if (namevalue.Length == 1){
                    //Hanlde &key=&key2= or &key&key2 -> key: "", key2: ""
                    //Setting a null value would be confusing, as that is how we determine
                    //whether a certain paramater exists
                    c[urlDecode ? HttpUtility.UrlDecode(namevalue[0]) : namevalue[0]] = "";
                }
            }
            return c;
        }

        /// <summary>
        /// Converts aribtrary bytes to a URL-safe version of base64 (no = padding, with - instead of + and _ instead of /)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToBase64U(byte[] data) {
            return Convert.ToBase64String(data).Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');
        }
        /// <summary>
        /// Converts a URL-safe version of base64 to a byte array.  (no = padding, with - instead of + and _ instead of /)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] FromBase64UToBytes(string data) {
            data = data.PadRight(data.Length + ((4 - data.Length % 4) % 4), '='); //if there is 1 leftover octet, add ==, if 2, add =. 3 octects = 4 chars. 
            return Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/'));
        }
        /// <summary>
        /// Converts aribtrary strings to a URL-safe version of base64 (no = padding, with - instead of + and _ instead of /)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToBase64U(string data) {
            return ToBase64U(UTF8Encoding.UTF8.GetBytes(data));
        }
        /// <summary>
        /// Converts a URL-safe version of base64 to a string. 64U is (no = padding, with - instead of + and _ instead of /)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string FromBase64UToString(string data) {
            return UTF8Encoding.UTF8.GetString(FromBase64UToBytes(data));
        }
        /// <summary>
        /// Returns the physcial mapped path for the specified virtual path if it starts with ~, otherwise retuns the original path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MapPathIfAppRelative(string path) {
            return (path.StartsWith("~", StringComparison.OrdinalIgnoreCase)) ? HostingEnvironment.MapPath(path) : path;
        }


        /// <summary>
        /// Generates a resized image url for the specified physical or virtual path. 
        /// Not CloudFront compatible. Does not support remote URLs, use RemoteReader for that.
        /// If you're running in IIS classic, add ".ashx" to the 'path' parameter. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string GenerateImageUrl(string path, NameValueCollection query) {
            return ResolveAppRelative(GuessVirtualPath(path)) + BuildQueryString(query); 
        }

        /// <summary>
        /// Attempts to guess the virtual path from physical path. Will be thrwarted if the path is mapped as a virtual folder inside the application.
        /// If the path is a non-physical path, it will be returned as is.
        /// Returns null if the physical path isn't a subfolder of the application's physical path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GuessVirtualPath(string path) {
            //If there's no windows slashes, it's probably virtual.
            if (path.IndexOf('\\') < 0)
                return path;
            
            string appPath = HostingEnvironment.ApplicationPhysicalPath;
            //Otherwise, try to get the full path so we can compare it.
            if (!Path.IsPathRooted(path)) path = Path.Combine(appPath, path);
            path = Path.GetFullPath(path);
            //If it's a match, strip it and convert it to a virtual path.
            if (path.StartsWith(appPath, StringComparison.OrdinalIgnoreCase)) {
                //Convert to app-relative path missing the ~/
                path = path.Remove(0, appPath.Length).TrimStart(new char[] { '/', '\\', '~' }).Replace('\\', '/');
                return PathUtils.ResolveAppRelativeAssumeAppRelative(path);
                
            }
            return null;
        }


        /// <summary>
        /// Replaces variables in paths with their values. Ex. ~/uploads/thumbs/&lt;guid>.&lt;ext>.
        /// Standard variables are &lt;ext> (the default extension for the final file type), &lt;guid>, a randomly generated GUID, 
        /// &lt;filename>, the original filename without it's extension, &lt;path>, the original path and filename without extension, 
        /// &lt;settings.width>, (any specified settings value except preset), &lt;width> (final width), and &lt;height> (final height).
        /// 
        /// </summary>
        /// <param name="pathWithVars"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static string ResolveVariablesInPath(string pathWithVars, VariableResolverCallback resolver) {
            string p = pathWithVars;
            while (p.IndexOf('<') > -1) {
                int start = p.IndexOf('<');
                int stop = p.IndexOf('>',start);
                int bugcheck = p.IndexOf('<',start + 1);
                if (stop < 0 || (bugcheck > -1 && bugcheck < stop) || stop == start + 1) 
                    throw new ImageProcessingException("Destination paths can only contain < and > in matched pairs to designate variables. Path \"" + pathWithVars + "\" has invalid syntax");
                string varName = p.Substring(start + 1,stop - start -1);
                string filter = null;
                //Split off the filter if present.
                int filterIx = varName.IndexOf(':');
                if (filterIx > 0) {
                    filter = varName.Substring(filterIx + 1);
                    varName = varName.Substring(0, filterIx);
                }
                varName = varName.ToLowerInvariant();
                string result = resolver(varName);
                if (!string.IsNullOrEmpty(filter)) {
                    result = RemoveNonMatchingChars(result, filter);
                }
                if (result == null)
                    throw new ImageProcessingException("Invalid variable name \"" + varName + "\" in templated path \"" + pathWithVars + "\". The variable name may be mispelled, or the variable may not be available with the pipeline you are using.");
                p = p.Substring(0,start) + result + p.Substring(stop + 1);
            }
            if (p.IndexOf('>') > -1) throw new ImageProcessingException("Orphaned '>' in template path \"" + pathWithVars + "\".");
            return p;
        }
        /// <summary>
        /// Allows a string to be filtered using the specified whitelisting expression (regex style). 
        /// 
        /// I.e, "hi YOU 3", "a-z3" will produce "hi3". 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static string RemoveNonMatchingChars(string text, string chars) {
            StringBuilder sb = new StringBuilder();
            Regex r = new Regex("[" + Regex.Escape(chars) + "]", RegexOptions.Compiled);
            for (int i = 0; i < text.Length; i++) {
                string chr = text.Substring(i,1);
                if (r.IsMatch(chr)) sb.Append(chr);
            }
            return sb.ToString();
        }

        /// <summary>
        /// A method that resolves variable names to values for the ResolveVariablesInPath method
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public delegate string VariableResolverCallback(string variableName);

        /// <summary>
        /// Returns a new collection containing only the specified keys from the old one
        /// </summary>
        /// <param name="query"></param>
        /// <param name="keepKeys"></param>
        /// <returns></returns>
        public static NameValueCollection FilterQueryString(ResizeSettings query, params string[] keepKeys) {
            return query.Keep(keepKeys);
        }

        /// <summary>
        /// Returns true if the current AppDomain has unrestricted .NET FileIOPermission to the given paths. 
        /// Does NOT check NTFS permissions; that's completely separate.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static bool HasIOPermission(string[] paths){
            var permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, paths));
            return permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
        }
    }
}