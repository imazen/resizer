using System;
using System.Collections.Generic;
using System.Text;
using fbs;
using System.Text.RegularExpressions;
namespace fbs.ImageResizer
{
    public static class CustomURLs
    {

        private static Regex resizeFolder = new Regex(@"(?:^|\/)resize\(\s*(?<maxwidth>\d+)\s*,\s*(?<maxheight>\d+)\s*(?:,\s*(?<format>jpg|png|gif)\s*)?\)\/", RegexOptions.Compiled
            | RegexOptions.IgnoreCase);
        /// <summary>
        /// Image request URLs are passed here.... this way we can make custom rules for folders and things.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static yrl customizeURL(yrl y)
        {
            string appRelPath = y.BaseFile;

            Match m = resizeFolder.Match(y.BaseFile);
            if (m.Success){
                int maxwidth = -1;
                int.TryParse(m.Groups["maxwidth"].Value, out maxwidth);
                int maxheight = -1;
                int.TryParse(m.Groups["maxheight"].Value, out maxheight);
                string format = null;
                if (m.Groups["format"].Captures.Count > 0)
                {
                    format = m.Groups["format"].Captures[0].Value;
                }
                //Remove resize folder from URL
                y.BaseFile = resizeFolder.Replace(y.BaseFile, "");
                //Add values to querystring
                if (maxwidth > 0) y["maxwidth"] = maxwidth.ToString();
                if (maxheight > 0) y["maxheight"] = maxheight.ToString();
                if (format != null) y["format"] = format;
            }

            return y;
        }
    }
}
