using System;
using System.Web.Mvc;
using System.Collections.Specialized;
using ImageResizer.Configuration;
using ImageResizer.Util;


namespace ImageResizer{

    public static class ImageResizerUrlHelpers {

        /// <summary>
        /// Requires the Gradient plugin be installed
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="imageFileName"></param>
        /// <param name="commands"></param>
        /// <param name="urlOptions"></param>
        /// <returns></returns>
        public static string Gradient(this UrlHelper helper, string color1, string color2, double angle, Instructions commands, UrlOptions urlOptions = null) {
            commands["color1"] = color1;
            commands["color2"] = color2;
            commands["angle"] = ParseUtils.SerializePrimitive<double>(angle);
            return Image(helper, "gradient.png", commands, urlOptions);
        }

        public static string Image(this UrlHelper helper, string imageFileName, Instructions commands, UrlOptions urlOptions = null) {
            return Config.Current.UrlBuilder.Default(imageFileName, commands, urlOptions);
        }


        public static string Image(this UrlHelper helper,string config, string imageFileName, Instructions commands, UrlOptions urlOptions = null) {
            return Config.Current.UrlBuilder.Url(config, imageFileName, commands, urlOptions);
        }
    }
}
