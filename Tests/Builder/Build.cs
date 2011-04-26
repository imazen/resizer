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

        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            var q = new FsQuery(this.f.corePath);

            
            //delete /Tests/binaries  *.pdb, *.xml, *.dll
            //delete /samples/ * /bin/ *.pdb, *.xml, *.dll
            f.DelFiles(q.files(new Pattern("^/Tests/binaries/*.(pdb|xml|dll)$"),
                                new Pattern("^/Samples/*/bin/*.(pdb|xml|dll)$")));
            //delete /tests/   /bin and /obj folders
            //delete /samples/ /imagecache
            //delete /core/obj
            //delete Plugins */obj* and */bin
            f.DelFolders(q.folders(new Pattern("^/Tests/*/(bin|obj)$"),
                                   new Pattern("^/Samples/*/imagecache$"),
                                   new Pattern("^/Plugins/*/(bin|obj)$"),
                                   new Pattern("^/Core/obj$")));


            //delete Thumbs.db
            //delete */.DS_Store
            q.exclusions = null;
            f.DelFiles(q.files(new Pattern("/Thumbs.db$"),
                                new Pattern("/.DS_Store$")));
            
            



        }
    }
}
