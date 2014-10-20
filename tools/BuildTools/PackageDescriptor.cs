using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildTools {

    public delegate void PackageBuilder(PackageDescriptor p);

    public class PackageDescriptor {

        public PackageDescriptor(string kind, PackageBuilder builder) {
   
            this.kind = kind;
            this.builder = builder;
        }
        public PackageDescriptor(string path, string kind, PackageBuilder builder) {
            this.path = path;
            this.kind = kind;
            this.builder = builder;
        }
        private string path;

        public string Path {
            get { return path; }
            set { path = value; }
        }

        public bool Exists { get { return System.IO.File.Exists(path); } }

        private string options = "";

        public string Options {
            get { return options; }
            set { options = value; }
        }

        private string kind;

        public string Kind {
            get { return kind; }
            set { kind = value; }
        }

        private PackageBuilder builder;

        public PackageBuilder Builder {
            get { return builder; }
            set { builder = value; }
        }


        public bool Build { get { return options.IndexOf("c", StringComparison.OrdinalIgnoreCase) > -1 || options.IndexOf("b", StringComparison.OrdinalIgnoreCase) > -1; } }
        public bool Upload { get { return options.IndexOf("u", StringComparison.OrdinalIgnoreCase) > -1; } }
        public bool Skip { get { return options.IndexOf("s", StringComparison.OrdinalIgnoreCase) > -1; } }
        public bool Private { get { return options.IndexOf("p", StringComparison.OrdinalIgnoreCase) > -1; } }

    }
}
