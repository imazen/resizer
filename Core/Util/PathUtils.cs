using System.Web.Hosting;
using System;
using System.Text;
using System.Collections.Specialized;
using System.Web;
public class PathUtils {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="virtualPath"></param>
    /// <returns></returns>
    public static string ResolveAppRelative(string virtualPath) {
        //resolve tilde
        if (virtualPath.StartsWith("~", StringComparison.OrdinalIgnoreCase)) return HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/' + virtualPath.TrimStart('~', '/');
        return virtualPath;
    }


    /// <summary>
    /// Joins the path and querystring. If the path already contains a querystring, they are 'append joined' with the correct character.
    /// </summary>
    /// <param name="virtualPath"></param>
    /// <param name="querystring"></param>
    /// <returns></returns>
    public static string AddQueryString(string virtualPath, string querystring) {
        if (virtualPath.IndexOf('?') > -1) virtualPath = virtualPath.TrimEnd('&') + '&';
        else virtualPath += '?';

        return virtualPath + querystring.TrimStart('&', '?');
    }

    /// <summary>
    /// Overwrites exisisting querystring values in 'path' with the values in 'newQuerystring'
    /// </summary>
    /// <param name="path"></param>
    /// <param name="newQuerystring"></param>
    /// <returns></returns>
    public static string MergeOverwriteQueryString(string path, NameValueCollection newQuerystring) {
        NameValueCollection oldQuery = ParseQueryString(path);
        //Overwrite old with new
        foreach (string key in newQuerystring.AllKeys)
            oldQuery[key] = newQuerystring[key];

        return AddQueryString(RemoveQueryString(path), BuildQueryString(oldQuery));
    }
    /// <summary>
    /// Adds the querystring values in 'newQuerystring' to the querystring in Path, but does not overwrite values.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="newQuerystring"></param>
    /// <returns></returns>
    public static string MergeQueryString(string path, NameValueCollection newQuerystring) {
        NameValueCollection oldQuery = ParseQueryString(path);
        //Overwrite new with old
        foreach (string key in oldQuery.AllKeys)
           newQuerystring[key] =  oldQuery[key];

        return AddQueryString(RemoveQueryString(path), BuildQueryString(newQuerystring));
    }


    /// <summary>
    /// Returns a string querystring in the form "?key=value&amp;key=value".
    /// Keys and values are UrlEncoded as they should be.
    /// </summary>
    /// <param name="QueryString"></param>
    /// <returns></returns>
    public static string BuildQueryString(NameValueCollection QueryString) {
        StringBuilder path = new StringBuilder();
        if (QueryString.Count > 0) {
            path.Append('?');
            foreach (string key in QueryString.Keys) {
                string value = QueryString[key];

                path.Append(HttpUtility.UrlEncode(key));
                path.Append('=');
                path.Append(HttpUtility.UrlEncode(value));
                path.Append('&');
            }
            if (path[path.Length - 1] == '&') path.Remove(path.Length - 1, 1);
        }
        return path.ToString();
    }

    /// <summary>
    /// Removes the query string from the specifed path. If the path is only a querystring, an empty string is returned.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string RemoveQueryString(string path) {
        int delimiter = path.IndexOf('?');
        return delimiter > -1 ? path.Substring(0,delimiter) : path;
    }

    /// <summary>
    /// Like ParseQueryString, but permits the leading '?' to be omitted.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static NameValueCollection ParseQueryStringFriendly(string path) {
        if (path.IndexOf('?') < 0) path = '?' + path;
        return ParseQueryString(path);
    }
    /// <summary>
    /// Parses the querystring from the given path into a NameValueCollection. 
    /// accepts "file?key=value" and "?key=value&key2=value2" formats. (no path is required)
    /// UrlDecodes keys and values.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static NameValueCollection ParseQueryString(string path) {
        NameValueCollection c = new NameValueCollection();
        int firstdelimiter = path.IndexOf('?');
        if (firstdelimiter < 0) return c;//Nothing to parse.

        string querystring = "";
        if (firstdelimiter < path.Length) querystring = path.Substring(firstdelimiter, path.Length - firstdelimiter);
        if (querystring.Length > 0) {
            string[] pairs = querystring.Split(new char[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in pairs) {
                string[] namevalue = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (namevalue.Length == 2) {
                    c[HttpUtility.UrlDecode(namevalue[0])] =
                        HttpUtility.UrlDecode(namevalue[1]);
                } else {
                    //No value, so we set a blank value
                    //Setting a null value would be confusing, as that is how we determine
                    //whether a certain paramater exists
                    c[HttpUtility.UrlDecode(namevalue[0])] = "";

                }
            }
        }
        return c;

    }

}