using System;
using System.Collections.Generic;
using System.Text;
using fbs;
using System.Text.RegularExpressions;
namespace fbs.ImageResizer
{
    /// <summary>
    /// Here is where you can set up custom image size defaults for folders (or any pattern you want). You can also perform URL rewriting on your images.
    /// </summary>
    public static class CustomFolders
    {
        /// <summary>
        /// Settings inserted here are overridden by the querystring values.
        /// Provides default resizing settings
        /// </summary>
        /// <param name="filePath">The virtual domain-relative path (/app/folder/file.jpg). Doesn't include the querystring.</param>
        /// <returns>Inserts /resize(x,y,f)/ into path when defaults are wanted. The /resize(x,y,f)/ is parsed and removed by the caller.</returns>
        public static string folderDefaults(string filePath)
        {
            //You can make certain folders default to certain dimensions.
            //return imagePath.Replace("/productThumbnails/","/resize(50,50)/productThumbnails/");

            //You can also configure or disable client caching.
            //context.Items["ContentExpires"] = DateTime.Now.AddHours(5);
            //or context.Items["ContentExpires"] = null
            return filePath;
        }
        /// <summary>
        /// Settings inserted here override the querystring values.
        /// This forces all images in a folder to be resized to the settings.
        /// </summary>
        /// <param name="filePath">The virtual domain-relative path (/app/folder/file.jpg). Doesn't include the querystring.</param>
        /// <returns>Inserts /resize(x,y,f)/ into path. The /resize(x,y,f)/ is parsed and removed by the caller.</returns>
        public static string folderOverrides(string filePath)
        {
            //You can make certain folders always resize to the same dimensions, regardless of the querystring.
            //return imagePath.Replace("/productThumbnails/","/resize(50,50)/productThumbnails/");
            return filePath;
        }

    }
}
