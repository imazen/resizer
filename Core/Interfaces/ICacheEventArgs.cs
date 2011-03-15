using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer {
    public interface ICacheEventArgs {
        string CacheKey { get; }
        string ContentType { get; }
        fbs.ImageResizer.CacheEventArgs.ModifiedDateDelegate GetModifiedDateUTC { get; }
        bool HasModifiedDate { get; }
        fbs.ImageResizer.CacheEventArgs.ResizeImageDelegate ResizeImageToStream { get; }
    }
}
