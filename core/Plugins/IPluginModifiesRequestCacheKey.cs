using System.Collections.Specialized;

namespace ImageResizer.Plugins
{
    public interface IPluginModifiesRequestCacheKey
    {
        string ModifyRequestCacheKey(string currentKey, string virtualPath, NameValueCollection queryString);
    }
}