using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageResizer.ReleaseBuilder {
    public class Build {
        SolutionFinder f = new SolutionFinder();
        Devenv d = null;
        public Build() {
            d = new Devenv(f.solutionPath);
        }

        public void CleanAll(){
            d.Run("/Clean Debug");
            d.Run("/Clean Release");
            d.Run("/Clean Trial");
        }

        public void BuildAll() {
            d.Run("/Build Debug");
            d.Run("/Build Release");
            d.Run("/Build Trial");
        }

    }
}
