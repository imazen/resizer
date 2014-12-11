using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace BuildTools {
    public class NPackageDescriptor {

        public NPackageDescriptor() {
            VariableSubstitutions = new NameValueCollection();
        }

        private string _specPath;

        public string SpecPath {
            get { return _specPath; }
            set { _specPath = value; }
        }


        private string version;

        public string Version {
            get { return version; }
            set { version = value; }
        }


        public NameValueCollection VariableSubstitutions { get; set; }

        private string _symbolSpecPath;

        public string SymbolSpecPath {
            get { return _symbolSpecPath; }
            set { _symbolSpecPath = value; }
        }

        public string SymbolPackagePath {
            get { return Path.Combine(OutputDirectory, BaseName + "." + version + ".symbols.nupkg"); }
        }

        public string PackagePath {
            get { return Path.Combine(OutputDirectory, BaseName + "." + version + ".nupkg"); }
        }

        private string _outputDir = null;
        public string OutputDirectory {
            get { return _outputDir == null ? Path.GetDirectoryName(SpecPath) : _outputDir; }
            set {
                _outputDir = value;
            }
        }
        
        public bool PackageExists {
            get {
                return File.Exists(PackagePath);
            }
        }
        public bool SymbolPackageExists {
            get {
                return File.Exists(SymbolPackagePath);
            }
        }
        private string _baseName;
        /// <summary>
        /// The package name
        /// </summary>
        public string BaseName {
            get { return _baseName; }
            set { _baseName = value; }
        }

        public bool Exists { get { return System.IO.File.Exists(_specPath); } }

        private string options = "";

        public string Options {
            get { return options; }
            set { options = value; }
        }


        public static IList<NPackageDescriptor> GetPackagesIn(string dir) {
            string nuspecExt = ".nuspec";
            string symbolsExt = ".symbols";
            Dictionary<string, NPackageDescriptor> byName = new Dictionary<string, NPackageDescriptor>(StringComparer.OrdinalIgnoreCase);
            List<NPackageDescriptor> packages = new List<NPackageDescriptor>();
            string[] files = Directory.GetFiles(dir, "*" + nuspecExt);
            foreach (string s in files) {
                //Strip the _specPath
                string fname = System.IO.Path.GetFileName(s);
                //Ignore all files that start with ".".
                if (fname.StartsWith(".")) continue;


                //Strip nuspec extension
                if (fname.EndsWith(nuspecExt, StringComparison.OrdinalIgnoreCase)) fname = fname.Substring(0, fname.Length - nuspecExt.Length);
                //Is it a symbol?
                bool isSymbol = fname.EndsWith(symbolsExt, StringComparison.OrdinalIgnoreCase);
                //Strip symbol extension
                if (fname.EndsWith(symbolsExt, StringComparison.OrdinalIgnoreCase)) fname = fname.Substring(0, fname.Length - symbolsExt.Length);
                //Look it up, add if missing
                NPackageDescriptor desc = null;
                if (!byName.TryGetValue(fname, out desc)) {
                    desc = new NPackageDescriptor();
                    packages.Add(desc);
                    desc.BaseName = fname;
                    byName[fname] = desc;
                }
                //Set the appropriate value on the package.
                if (isSymbol)
                    desc.SymbolSpecPath = s;
                else
                    desc.SpecPath = s;
            }
            return packages;
        }


        public bool BuildIfMissing { get { return options.IndexOf("r", StringComparison.OrdinalIgnoreCase) > -1; } }
        public bool Build { get { return options.IndexOf("c", StringComparison.OrdinalIgnoreCase) > -1 || (BuildIfMissing && (!this.PackageExists || !this.SymbolPackageExists)); } }
        public bool Upload { get { return options.IndexOf("u", StringComparison.OrdinalIgnoreCase) > -1; } }
        public bool Skip { get { return options.IndexOf("s", StringComparison.OrdinalIgnoreCase) > -1; } }

    }
}
