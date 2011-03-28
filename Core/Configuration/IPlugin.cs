using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration {
    public interface IPlugin {
        public IPlugin Install(Config c);
        public bool Uninstall(Config c);
        public string ShortName { get; }
    }
}
