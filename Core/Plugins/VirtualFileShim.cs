using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace ImageResizer.Plugins
{
    public class VirtualFileShim : VirtualFile
    {
        private IVirtualFile f;
        public VirtualFileShim(IVirtualFile f): base(f.VirtualPath)
        {
            this.f = f;
        }
        public override System.IO.Stream Open()
        {
            return f.Open();
        }
    }
}
