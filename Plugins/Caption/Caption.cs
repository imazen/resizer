using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.Basic.Caption {
    public class Caption:BuilderExtension,IPlugin,IQuerystringPlugin {
        //Accepts 
        //caption=text
        //caption.align=left|right|center
        //caption.padding=all|left,top,right,bottom
        //caption.position=insidemargin|insideborder|insidepadding
        //caption.font=arial|verdana
        //caption.size=10
        public IPlugin Install(Configuration.Config c) {
            throw new NotImplementedException();
        }

        public bool Uninstall(Configuration.Config c) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            throw new NotImplementedException();
        }
    }
}
