/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * 
 **/

using System;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;

//System.Web.VirtualPathUtility

namespace ImageResizer.Util
{


 /*
 * Notes: 
 * 
 * This class is designed to standardize path interpretation and conversion.

 * This class will convert any fully qualified url to a relative path, even if the domain doesn't match. 
 * So: If you are going to validate and use path parameters, convert to yrl form BEFORE performing validation, not
 * after. If you use the yrl class all the way through, you shouldn't have problems.
 * 
 * This class will throw an exception if passed invalid input.
 * 
 * TODO: Remember to account for &AspxAutoDetectCookieSupport=1 
 * 
 */

    /// <summary>
    /// Enapsulates a mutable (changable) site-relative URL. Note that "" is equivalent to the application root directory in YRL notation (the ~/ is implicit, always).
    /// QueryFindYrlVerifyID can be removed if external dependencies aren't allowed. It uses fbs.Articles.Index.FindPathByID()
    /// This class is designed to standardize path interpretation and conversion.
    /// </summary>
    [Serializable()]
    public class yrl
    {
        /// <summary>
        /// The base file, usually a .aspx page. Ex. 'home.aspx' or 'admin/new.aspx'. Can also be a base directory, such as articles
        /// </summary>
        public string BaseFile = "";
        /// <summary>
        /// A collection of name/value query paramaters. Do NOT UrlEncode/Decode these! That step will be done for you.
        /// </summary>
        public NameValueCollection QueryString = new NameValueCollection();

        // Summary:
        //     Gets the entry at the specified index of the System.Collections.Specialized.NameValueCollection.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the entry to locate in the collection.
        //
        // Returns:
        //     A System.String that contains the comma-separated list of values at the specified
        //     index of the collection.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is outside the valid range of indexes for the collection.
        public string this[int index] { get { return QueryString[index]; } }
        //
        // Summary:
        //     Gets or sets the entry with the specified key in the System.Collections.Specialized.NameValueCollection.
        //
        // Parameters:
        //   name:
        //     The System.String key of the entry to locate. The key can be null.
        //
        // Returns:
        //     A System.String that contains the comma-separated list of values associated
        //     with the specified key, if found; otherwise, null.
        //
        // Exceptions:
        //   System.NotSupportedException:
        //     The collection is read-only and the operation attempts to modify the collection.
        public string this[string name] { get { return QueryString[name]; } set { QueryString[name] = value; } }




        #region Constructor
        /// <summary>
        /// Creates a default instance of yrl which  points to the application root directory.
        /// </summary>
        public yrl()
        {
        }
        public yrl(string path)
        {
            if (path == null) throw new ArgumentNullException("path", "Cannot create a yrl from a null string");
            yrl temp = FromString(path);
            if (temp == null) throw new ArgumentException("The specified path (" + path + ") could not be parsed! It may be outside the bounds of the application, or it may contain an invalid character, such as a tilde in the middle of the path.", "path");
            this.BaseFile = temp.BaseFile;
            this.QueryString = temp.QueryString;
        }

        #endregion

        #region yrl Constructor components
        /// <summary>
        /// Returns a yrl object from a path. Returns null if the path cannot be parsed.
        /// (Out of application bounds, or an invalid character). The tilde is an invalid character unless used as the app-relative specifier.
        /// </summary>
        /// <param name="path">A path
        /// Each query paramater will be UrlDecoded.
        /// </param>
        /// <returns></returns>
        public static yrl FromString(string path)
        {
            //crashes on ~$lioFlatFile.rtf
            PathType pathtype = GetPathType(path);
            if (pathtype == PathType.Root) return yrl.Root;
			
			//OSX Mono uses physical paths in the unix style - fools the comparison. So, to handle physical paths right, we have to try that first.
			
			yrl test = FromPhysicalString(path);
			if (test != null) return test;
			
            if (pathtype == PathType.File || pathtype == PathType.PathForwardslashPath)
            {
                return FromYrlString(path);
            }
            
            if (pathtype == PathType.ForwardslashPath)
            {
                return FromYrlString(TrimStartAppFolder(path).TrimStart(new char[] { '/' }));
            }
            if (pathtype == PathType.ServerForwardslashPath)
            {
                int tilde = path.IndexOf('~');
                if (tilde > 0)
                {
                    return FromYrlString(TrimStartAppFolder(TrimStartServer(path).TrimStart(new char[] { '~', '/' })).TrimStart(new char[] { '/' }));
                }
                else
                    return FromYrlString(TrimStartAppFolder(TrimStartServer(path)).TrimStart(new char[] { '/' }));
            }
            if (pathtype == PathType.TildeForwardslashPath)
            {
                //return FromYrlString(TrimStartAppFolder(path.TrimStart(new char[] { '~', '/' })).TrimStart(new char[] { '/' }));
                return FromYrlString(path.TrimStart(new char[] { '~', '/' }));
            }
            if (pathtype == PathType.DriveletterColonBackslashPath)
            {
                return FromPhysicalString(path);
            }
            if (pathtype == PathType.BackslashPath || pathtype == PathType.PathBackslashPath)
            {
                return FromRelativePhysicalString(path);
            }
            return null;
        }
        /// <summary>
        /// Creates an instance from a yrl-syntax string (virtual, but without ~/). Use FromString if you're not sure what type of syntax is used.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static yrl FromYrlString(string path)
        {
            if (path.Length == 0) return yrl.Root;
            yrl temp = new yrl();
            int firstdelimiter = path.IndexOfAny(new char[] { '?', '&' });
            if (firstdelimiter < 0) firstdelimiter = path.Length;
            if (path.IndexOfAny(new char[] { '/', '\\', '~', ':' }) == 0) return null;

            //throw new Exception("The specified path \"" + path + "\" starts with an invalid character! yrl paths cannot begin with any sort of slash, tilde, or colon!");
            temp.BaseFile = path.Substring(0, firstdelimiter);

            string querystring = "";
            if (firstdelimiter < path.Length) querystring = path.Substring(firstdelimiter, path.Length - firstdelimiter);
            if (querystring.Length > 0)
            {
                string[] pairs = querystring.Split(new char[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in pairs)
                {
                    string[] namevalue = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (namevalue.Length == 2)
                    {
                        temp.QueryString[HttpUtility.UrlDecode(namevalue[0])] = 
                            HttpUtility.UrlDecode(namevalue[1]);
                    }
                    else
                    {
                        //No value, so we set a blank value
                        //Setting a null value would be confusing, as that is how we determine
                        //whether a certain paramater exists
                        temp.QueryString[HttpUtility.UrlDecode(namevalue[0])] = "";
                        //throw new Exception("The specified path \"" + path + "\" contains a query paramater pair \"" + s + "\" that does not parse!");
                    }
                }
            }
            return temp;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static yrl FromRelativePhysicalString(string path)
        {
            string temp = path;
            if (temp[0] != '\\') temp = "\\" + temp;
            temp = TrimStartAppFolder(temp);
            string newpath = HostingEnvironment.MapPath("~") + temp;
            return FromPhysicalString(newpath);
        }
        /// <summary>
        /// Creates a yrl Instance from the specified physical path. Throws an exception if the path is outside the application structure.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static yrl FromPhysicalString(string path)
        {
            string temp = path;
            string localapproot = HostingEnvironment.MapPath("~").TrimEnd(new char[] { '/', '\\', '~' });

            if (temp.StartsWith(localapproot, StringComparison.OrdinalIgnoreCase))
            {
                temp = temp.Remove(0, localapproot.Length);
            }
            else
            {
                return null;
                //throw new Exception("The specified path \"" + path + "\" is outside the bounds of the application (\"" + localapproot + "\")and cannot be handled!");
            }
            temp = temp.TrimStart(new char[] { '/', '\\', '~' }).Replace('\\', '/');

            return FromYrlString(temp);
        }
        /// <summary>
        /// either '/' or '/folder' like '/yf' or sometimes '/folder/folder'
        /// </summary>
        /// <returns></returns>
        public static string GetAppFolderName()
        {

            return HostingEnvironment.ApplicationVirtualPath;
        }
        /// <summary>
        /// Removes the application folder from the specified path. Leaves the leading forwardslash. 
        /// Assuming the Application path is /yf, this function will transform /yf/home.aspx to /home.aspx and /yf/css/john.css to /css/john.css
        /// If the application is located in '/', nothing will be done to the path. Transforms yf/home.aspx to /home.aspx, yf\home.aspx to \home.aspx, \yf\home.aspx to \home.aspx
        /// Won't work if the app folder has a child directory named the same!
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string TrimStartAppFolder(string path)
        {
            string appfoldername = GetAppFolderName();
            if (appfoldername.Length <= 1) return path;

            //Remove /appfolder
            if (path.StartsWith(appfoldername, StringComparison.InvariantCultureIgnoreCase))
                return path.Remove(0, appfoldername.Length);

            //remove apppfolder
            string appfolderwithoutslash = appfoldername.TrimStart(new char[] { '/' });
            if (path.StartsWith(appfolderwithoutslash, StringComparison.InvariantCultureIgnoreCase))
                return path.Remove(0, appfolderwithoutslash.Length);

            //remove \appfolder
            string appfolderwithbackslash = appfoldername.Replace('/', '\\');
            if (path.StartsWith(appfolderwithbackslash, StringComparison.InvariantCultureIgnoreCase))
                return path.Remove(0, appfolderwithbackslash.Length);

            //remove ~/appfolder
            string appfolderwithtilde = '~' + appfoldername;
            if (path.StartsWith(appfolderwithtilde, StringComparison.InvariantCultureIgnoreCase))
                return path.Remove(0, appfolderwithtilde.Length);
            //return result
            return path;
        }

        public static string TrimStartServer(string path)
        {
            int firstdoubleforwardslash = path.IndexOf("//");
            if (firstdoubleforwardslash < 0) return path;
            if (path.Length < firstdoubleforwardslash + 3) return path;

            int nextforwardslash = path.IndexOf("/", firstdoubleforwardslash + 2);
            if (nextforwardslash < 0)
            {
                //It's all server
                return "";
            }
            return path.Remove(0, nextforwardslash);
        }
        #endregion

        #region Path Type Analysis
        /// <summary>
        /// Path syntaxes, determined by patterns.
        /// </summary>
        public enum PathType
        {
            /// <summary>
            /// \\server\share\
            /// </summary>
            UNCPath,
            /// <summary>
            /// A path like '\temp\file.htm'
            /// </summary>
            BackslashPath,
            /// <summary>
            /// A path like 'c:\program files\temp'
            /// </summary>
            DriveletterColonBackslashPath,
            /// <summary>
            /// a path like 'img\file.img'
            /// </summary>
            PathBackslashPath,
            /// <summary>
            /// a path like '~/home.aspx'
            /// </summary>
            TildeForwardslashPath,
            /// <summary>
            /// a path like '/home.aspx'
            /// </summary>
            ForwardslashPath,
            /// <summary>
            /// a path like 'img/banner.jpg'
            /// </summary>
            PathForwardslashPath,
            /// <summary>
            /// a path like http://www.branham.org/home.aspx
            /// </summary>
            ServerForwardslashPath,
            /// <summary>
            /// A filename with no path, like 'test.exe' or 'home.aspx'
            /// </summary>
            File,
            /// <summary>
            /// Specifies the application root
            /// </summary>
            Root,
            /// <summary>
            /// Indeterminant form
            /// </summary>
            Invalid
        }
        /// <summary>
        /// Returns the type of path 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static PathType GetPathType(string path)
        {
            if (path == null) return PathType.Invalid;
            if (path.Trim(new char[] { '\\', '/', '~' }).Length == 0) return PathType.Root;
            int firstbackslash = path.IndexOf('\\');
            int firstforwardslash = path.IndexOf('/');
            int firsttilde = path.IndexOf('~');
            int firstcolon = path.IndexOf(':');
            int firstdoubleforwardslash = path.IndexOf("//");
            if (firstbackslash == 0 && path[1] == '\\')
            {
                return PathType.UNCPath;
            }
            if (firstbackslash == 0 && firstforwardslash < 0 && firstcolon < 0)
            {
                return PathType.BackslashPath;
            }
            if (firstcolon == 1 && firstbackslash == 2)
            {
                return PathType.DriveletterColonBackslashPath;
            }
            if (firstcolon < 0 && firstbackslash > 0 && firstforwardslash < 0)
            {
                return PathType.PathBackslashPath;
            }
            if (firsttilde == 0 && firstforwardslash == 1)
            {
                return PathType.TildeForwardslashPath;
            }
            if (firstforwardslash == 0 && firsttilde != 0 && firstdoubleforwardslash < 0)
            {
                return PathType.ForwardslashPath;
            }
            if (firstcolon > 0 && firstdoubleforwardslash > 0 && firsttilde != 0)
            {
                return PathType.ServerForwardslashPath;
            }
            if (firsttilde != 0 && firstforwardslash < 0 && firstbackslash < 0 && firstcolon < 0)
            {
                return PathType.File;
            }
            if (firstforwardslash > 0)
            {
                return PathType.PathForwardslashPath;
            }
            return PathType.Invalid;
        }
        #endregion

        #region Properties and methods

        /// <summary>
        /// yrl.QueryString["id"] Returns -1 if not found, -2 if unparseable
        /// </summary>
        public int QueryID
        {
            get
            {
                
                if (QueryString["id"] == null) return -1;
                int temp = 0;
                if (!int.TryParse(QueryString["id"], out temp))
                {
                    return -2;
                }
                return temp;
            }
            set
            {
                QueryString["id"] = value.ToString();
            }
        }
        /// <summary>
        /// yrl.QueryString["path"]. Returns null if not found, throws exception if not parseable.
        /// </summary>
        public yrl QueryPath
        {
            get
            {
                if (QueryString["path"] == null) return null;
                return new yrl(QueryString["path"]);
            }
            set
            {
                QueryString["path"] = value.ToString();
            }
        }
        /// <summary>
        /// yrl.QueryString["dir"]. Returns null if not found, throws exception if not parseable.
        /// </summary>
        public yrl QueryDir
        {
            get
            {
                if (QueryString["dir"] == null) return null;
                return new yrl(QueryString["dir"]);
            }
            set
            {
                if (value == null) QueryString.Remove("dir");
                else
                QueryString["dir"] = value.ToString();
            }
        }
        /// <summary>
        /// Tries to track down the ID by looking at QueryString["id"], then QueryString["path"]. Returns -1 if unsuccessful
        /// </summary>
        public int QueryFindID
        {
            get
            {
                int try1 = QueryID;
                if (try1 > 0) return try1;

                yrl try2 = QueryPath;
                if (try2 != null)
                {
                    if (try2.ID > 0) return try2.ID;
                }
                return -1;

            }
        }

        /// <summary>
        /// Creates a deep copy of the yrl
        /// </summary>
        /// <returns></returns>
        public yrl Copy()
        {
            yrl temp = new yrl();
            temp.BaseFile = string.Copy(this.BaseFile);
            foreach (string s in QueryString.Keys)
            {
                string value = QueryString[s];
                temp.QueryString.Add(string.Copy(s), string.Copy(value));
            }
            return temp;
        }

        /// <summary>
        /// Returns a site-root-relative path along with query paramaters
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder path = new StringBuilder();
            path.Append(BaseFile);
            if (QueryString.Count > 0)
            {
                path.Append('?');
                foreach (string key in QueryString.Keys)
                {
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
        /// Returns the physical filesystem path for this yrl
        /// </summary>
        /// <returns></returns>
        public string Local
        {
            get
            {
                if (BaseFile.Length == 0) return HostingEnvironment.MapPath("~");
                return HostingEnvironment.MapPath("~/" + HttpUtility.UrlDecode(BaseFile));
            }
        }
        /// <summary>
        /// Returns the virtual path(~/...)of the base file without query paramaters.
        /// </summary>
        public string Virtual
        {
            get
            {
                if (BaseFile.Length == 0) return "~";
                return "~/" + this.BaseFile;
            }
        }

        /// <summary>
        /// Returns the virtual path (~/...) of the base file with query paramaters appended.
        /// </summary>
        public string VirtualURL
        {
            get
            {
                if (this.URL.Length == 0) return "~";
                return "~/" + this.URL;
            }
        }
        /// <summary>
        /// Returns a ~/ path designed for the Hyperlink.NavigateUrl property
        /// </summary>
        public string NavigateURL
        {
            get
            {
            StringBuilder path = new StringBuilder();
            path.Append(BaseFile);
            if (QueryString.Count > 0)
            {
                path.Append('?');
                foreach (string key in QueryString.Keys)
                {
                    string value = QueryString[key];

                    path.Append(NavigateUrlEncode(key));
                    path.Append('=');
                    path.Append(NavigateUrlEncode(value));
                    path.Append('&');
                }
                if (path[path.Length - 1] == '&') path.Remove(path.Length - 1, 1);
            }
            return "~/" + path.ToString();
            }
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string NavigateUrlEncode(string text)
        {

            return text.Replace("/", "%2f").Replace("?","%3f").Replace("&","&amp;").Replace(" ","%20");
        }

        /// <summary>
        /// Returns root-relative URL with query paramaters in the form 'articles/00001002 article.aspx'
        /// </summary>
        public string URL
        {
            get
            {
                return this.ToString();
            }

        }
        /// <summary>
        /// Returns root-relative URL with query paramaters in the form 'articles/00001002%20article.aspx'
        /// </summary>
        /// <returns></returns>
        public string URLEncoded
        {
            get { return HttpUtility.UrlEncode(this.ToString()); }
        }

        /// <summary>
        /// Returns root-relative URL with query paramaters in the form 'articles/00001002%20article.aspx'.
        /// Encodes special characters into HTML entities using Server.URLPathEncode
        /// </summary>
        public string URLHtmlEncoded
        {
            get
            {
                return HttpUtility.UrlPathEncode(this.ToString());
                //.Replace("&amp;", "&").Replace(" ", "%20").Replace("&", "&amp;");
            }
        }
        /// <summary>
        /// Returns a domain-relative path in the form '/yf/articles/00001002%20article.aspx'
        /// </summary>
        public string URLAnchorTarget
        {
            get
            {
                return yrl.GetAppFolderName().TrimEnd(new char[] { '/' }) + "/" + HttpUtility.HtmlAttributeEncode(this.URL);
            }
        }

        /// <summary>
        /// Returns relaitve URL with query paramaters in the form '../articles/00001002 article.aspx'
        /// </summary>
        public string RelativeURL
        {
            get
            {
                return this.ToString();
            }

        }
   


        /// <summary>
        /// Returns the ID if a leading 8-digit ID is found in the base file. Returns -1 otherwise
        /// </summary>
        public int ID
        {
            get
            {
                int id = -1;
                //GetFileName works with URIs and paths.
                //Had errors using this.Local - WebResource.Axd isn't mappable, but would trigger an ID check.
                string filename = System.IO.Path.GetFileName(BaseFile);

                if (filename.Length < 8) return -1;
                if (!int.TryParse(filename.Substring(0, 8), out id))
                {
                    //It doesn't start with a 10-digit number.
                    return -1;
                }
                return id;
            }

        }


        /// <summary>
        /// Returns first leading ID found in a segment of the path.
        /// </summary>
        public int FirstID
        {
            get
            {
                string[] segments = BaseFile.Split(new char[] { '/' }, StringSplitOptions.None);

                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i].Length >= 8)
                    {
                        int tempid = 0;
                        if (int.TryParse(segments[i].Substring(0, 8), out tempid))
                        {
                            return tempid;
                        }
                    }

                }
                return -1;
            }
        }


        /// <summary>
        /// Returns true if BaseFile exists as a local file.
        /// </summary>
        public bool FileExists
        {
            get
            {
                return System.IO.File.Exists(Local);
            }
        }

        /// <summary>
        /// Returns true if BaseFile exists as a local Directory.
        /// </summary>
        public bool DirExists
        {
            get
            {
                return System.IO.Directory.Exists(Local);
            }
        }
        /// <summary>
        /// Returns a DirectoryInfo object for BaseFile if it is a directory. If not, it returns the parent directory
        /// </summary>
        public DirectoryInfo DirInfo
        {
            get
            {
                if (DirExists)
                    return new DirectoryInfo(Local);
                else
                    return new DirectoryInfo(System.IO.Path.GetDirectoryName(Local));
            }
        }
        /// <summary>
        /// Returns a yrl of the parent directory. If the current yrl is already the root, returns null;
        /// Use 
        /// </summary>
        public yrl Parent
        {
            get
            {
                if (IsRoot) return null;
                if (IsAspx) return new yrl(System.IO.Path.GetDirectoryName(Local) );
                if (!DirExists) return new yrl(System.IO.Path.GetDirectoryName(Local) );
                else
                {
                    DirectoryInfo d = new DirectoryInfo(Local);
                    return new yrl(d.Parent.FullName);
                }
            }
        }
        /// <summary>
        /// Returns the directory portion of this yrl.
        /// </summary>
        public yrl Directory
        {
            get
            {
                if (IsRoot) return this;
                if (IsAspx) return new yrl(System.IO.Path.GetDirectoryName(Local));
                if (!DirExists) return new yrl(System.IO.Path.GetDirectoryName(Local));
                else
                {
                    return this;
                }
            }
        }

        /// <summary>
        /// Returns an instance of yrl that points to the application root.
        /// </summary>
        public static yrl Root
        {
            get { return new yrl(); }
        }

        //public class RequestHelper
        //{
        //    public static yrl TrueYrl { get { return yrl.FromString(TruePath); } }
        //    /// <summary>
        //    /// Attempts to retrieve the destination virtual path if it was rewritten. If not, it returns
        //    /// Request.Url.PathAndQuery. Paths are in the form "/default.aspx"
        //    /// </summary>
        //    public static string TruePath
        //    {
        //        get
        //        {
        //            object o = HttpContext.Current.Items[UrlRewritingNet.Web.UrlRewriteModule.PhysicalPath];
        //            if (o != null)
        //            {
        //                return (string)o;
        //            }
        //            return HttpContext.Current.Request.Url.PathAndQuery;

        //        }
        //    }
        //}
        /// <summary>
        /// Returns the REAL file that is being executed. Falls back to CurrentBrowserUrl if unavailable.
        /// Unavailable if UrlRewritingNet isn't in use
        /// </summary>
        public static yrl Current
        {
            get
            {
                if (HttpContext.Current == null) return null;
                //This should us UrlRewritingNet.Web.UrlRewriteModule.PhysicalPath
                //instead of the magic string. Done for dependency elimination

                object o = HttpContext.Current.Items["UrlRewritingNet.UrlRewriter.CachedPathAfterRewrite"];
                if (o != null)
                {
                    return yrl.FromString((string)o);
                }
                return CurrentBrowserURL;
            }
        }
        /// <summary>
        /// Retreieves the current URL from HttpContext.Current.Request.Url (pre-rewrite path)
        /// </summary>
        public static yrl CurrentBrowserURL
        {
            get
            {
                if (HttpContext.Current == null) return null;
                if (HttpContext.Current.Request == null) return null;
                return FromUri(HttpContext.Current.Request.Url);
                //return FromString(HttpContext.Current.Request.RawUrl);
            }
        }
        public static yrl FromUri(Uri url)
        {
            if (url == null) return null;
            return new yrl(url.AbsoluteUri);
        }
        
        /// <summary>
        /// Returns null if unavailable
        /// </summary>
        public static yrl Referrer
        {
            get
            {
                if (HttpContext.Current == null) return null;
                if (HttpContext.Current.Request == null) return null;
                if (HttpContext.Current.Request.UrlReferrer == null) return null;
                if (HttpContext.Current.Request.UrlReferrer.OriginalString == null) return null;
                return new yrl(HttpContext.Current.Request.UrlReferrer.OriginalString);
            }
        }


        /// <summary>
        /// Compares the hash codes of the two instances.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return (obj.GetHashCode() == this.GetHashCode());
        }
        /// <summary>
        /// Returns a unique hash code derived from the URL property of this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return URL.ToLower().GetHashCode();
        }


        ///// <summary>
        ///// Returns true if BaseFile contains '_vti' or should be ignored (the discard bin, subversion, etc)
        ///// </summary>
        //public bool IsExcluded
        //{
        //    get
        //    {
        //        return ExclusiBaseFile.Contains("_vti");
        //    }
        //}
        
        /// <summary>
        /// An empty yrl signifies the application root. This verifies both the path and the querystring are empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return (BaseFile.Length == 0 && QueryString.Count == 0);
            }
        }


        /// <summary>
        /// Returns true if the basefile is empty (the root) (the QueryString can have data). Use IsEmpty to check that both are empty.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return (BaseFile.Length == 0);
            }
        }
        /// <summary>
        /// Returns true if this path has no parent directory (the parent directory is the root).
        /// </summary>
        public bool IsInRoot
        {
            get
            {
                if (Parent == null) return true;
                return (Parent.IsRoot);
            }
        }

        /// <summary>
        ///  Returns the name of the directory (like 'archives') if it is one, and the name of the file (like 'universal.css') if it is a file.
        /// </summary>
        public string Name
        {
            get
            {
                if (IsAspx) return System.IO.Path.GetFileName(Local);
                if (DirExists) return new DirectoryInfo(Local).Name;
                else return System.IO.Path.GetFileName(Local);
            }
        }
        #region File Extension stuff & associated .aspx/.cs file lookup. Also contains FilenameWithout.... properties
        /// <summary>
        /// Returns the name of BaseFile without the extension.
        /// </summary>
        public string NameWithoutExtension
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(Local);
            }
        }

        /// <summary>
        /// Returns the filename without the ID or extension, converts underscores to spaces, and trims the string.
        /// </summary>
        public string PrettyTitle
        {
            get
            {
                return FilenameWithoutIdAndExtension.Replace('_', ' ').Trim();
            }
        }
        /// <summary>
        /// Returns the filename minus path information, extensions, or Article ID.
        /// </summary>
        public string FilenameWithoutIdAndExtension
        {
            get
            {
                int id = 0;
                string filename = this.NameWithoutExtension;
                if (filename.Length >= 8)
                {
                    if (int.TryParse(filename.Substring(0, 8), out id))
                    {
                        //ID now refers to the id on the filename.
                        //Remove ID
                        return filename.Substring(8, filename.Length - 8);
                    }
                }
                //It doesn't start with a 10-digit number.
                //Set the title to the entire filename
                return filename;
            }
        }
        /// <summary>
        /// Returns the extension of BaseFile in the form '.aspx'
        /// </summary>
        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(Local);
            }
        }
        /// <summary>
        /// returns true if BaseFile ends with .aspx
        /// </summary>
        public bool IsAspx
        {
            get
            {
                return BaseFile.EndsWith(".aspx", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public bool HasExtension
        {
            get
            {
                return (Extension.Length > 0);
            }
        }
        /// <summary>
        /// returns true if BaseFile ends with .cs or .vb
        /// </summary>
        public bool IsCodeFile
        {
            get
            {
                return BaseFile.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase) |
                    BaseFile.EndsWith(".vb", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        /// <summary>
        /// If this is .aspx.cs or .aspx.vb file, attempts to find and return the associated .aspx file. 
        /// If this is a .aspx file, returns the current instance.
        /// Otherwise, this returns null;
        /// </summary>
        public yrl FindAssociatedMarkupFile
        {
            get
            {
                if (IsCodeFile)
                {
                    yrl baseAspx = new yrl(this.Local.ToLower().Replace(".cs", "").Replace(".vb", ""));
                    if (baseAspx.FileExists) return baseAspx;
                    else return null;
                }
                if (IsAspx)
                {
                    return this;
                }
                return null;
            }
        }
        /// <summary>
        /// If this is an .aspx file, attempts to find and return an associated .aspx.cs or .aspx.vb file. 
        /// If this is .aspx.cs or .aspx.vb file, returns the current instance.
        /// Otherwise, this returns null;
        /// </summary>
        public yrl FindAssociatedCodeFile
        {
            get
            {
                if (IsAspx)
                {
                    yrl codeCs = new yrl(this.Local + ".cs");
                    yrl codeVb = new yrl(this.Local + ".vb");
                    if (codeCs.FileExists) return codeCs;
                    else if (codeVb.FileExists) return codeVb;
                    else return null;
                }
                if (IsCodeFile)
                {
                    return this;
                }
                return null;
            }
        }

        /// <summary>
        /// Deletes the old extension and replaces it with the specified extension. A leading '.' is not neccesary
        /// </summary>
        /// <param name="newextension"></param>
        public void ChangeExtension(string newextension)
        {
            BaseFile = BaseFile.Remove(BaseFile.Length - this.Extension.Length, this.Extension.Length);
            BaseFile += "." + newextension.TrimStart(new char[] { '.' });
        }
        #endregion

        /// <summary>
        /// Returns the next unused filename similar to the current one,by incrementing (i)
        /// </summary>
        /// <returns></returns>
        public yrl GetNextAvailableDerivitive()
        {
            yrl newfilename = this.Copy();
            if (newfilename.FileExists)
            {

                //This section here just looks for the next unused filname in the form
                // oldfilename (1).oldextension
                // This way, if the same file is uploaded 5 times it will appear as
                // file.txt
                // file (1).txt
                // file (2).txt
                // file (3).txt
                // file (4).txt
                string minusext = newfilename.Parent.Local +
                    System.IO.Path.DirectorySeparatorChar +
                    newfilename.NameWithoutExtension;

                for (int i = 1; i < 100; i++)
                {
                    if (!System.IO.File.Exists(minusext + " (" + i.ToString() + ")" + newfilename.Extension))
                    {
                        newfilename = new yrl(minusext + " (" + i.ToString() + ")" + newfilename.Extension);
                        break;
                    }
                }
            }
            return newfilename;
        }
        public static explicit operator string(yrl url)
        {
            return url.ToString();
        }
        public static explicit operator yrl(string s)
        {
            return yrl.FromString(s);
        }
        #endregion
        /// <summary>
        /// Joins two yrls. Querystrings on either are discarded.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filenameOrSubdir"></param>
        /// <returns></returns>
        public static yrl Combine(yrl folder, yrl filenameOrSubdir)
        {
            return new yrl(folder.Local + System.IO.Path.DirectorySeparatorChar + filenameOrSubdir.BaseFile.Replace('/','\\'));
        }
        /// <summary>
        /// Returns an absolute path to the current yrl using the specified base path
        /// </summary>
        /// <param name="protocolHostnameFolder">A base path like http://youngfoundations.org/ or http://localhost:2755/yf/</param>
        /// <returns></returns>
        public string CreateAbsoluteUrl(string protocolHostnameFolder)
        {
            return protocolHostnameFolder + this.URL;
        }
        /// <summary>
        /// Returns true if the yrl is null or empty (site root)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(yrl path)
        {
            if (path == null) return true;
            if (path.IsEmpty) return true;
            return false;
        }
    }
}