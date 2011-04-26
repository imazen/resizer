using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ImageResizer.ReleaseBuilder {
    public class SolutionFinder {

        public SolutionFinder() {
            Find();
        }

        public string solutionPath;
        public string corePath;
        public string rootPath;



        public string FindFolder(string startPath, string query) {
            DirectoryInfo di = new DirectoryInfo(startPath);
            while (di != null) {
                DirectoryInfo[] results = di.GetDirectories(query, SearchOption.TopDirectoryOnly);
                if (results != null && results.Count<DirectoryInfo>() > 0)
                    return results[0].FullName;

                di = di.Parent;
            }
            return null;
        }


        protected void Find() {
            //Where the assembly originated
            string originalFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            //Working directory
            string workingFolder = Directory.GetCurrentDirectory();

            corePath = FindFolder(workingFolder, "Core");
            if (corePath == null) corePath = FindFolder(originalFolder, "Core");
            if (corePath != null) {
                solutionPath = Path.Combine(corePath, "ImageResizer.sln");
                rootPath = Path.GetDirectoryName(corePath);
            }


        }



    }
}
