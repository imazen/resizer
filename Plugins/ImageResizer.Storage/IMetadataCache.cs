using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Storage
{
    public interface IMetadataCache
    {
        object Get(string key);
        void Put(string key, object data);

    }
}
