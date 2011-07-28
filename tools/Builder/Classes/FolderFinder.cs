using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ImageResizer.ReleaseBuilder {
    public class FolderFinder {
        /// <summary>
        /// Attempts to locate the specified folder by traversing up the directory tree from two locations (the original Builder exe location, and the current working directory).
        /// Checks siblings at each level.
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="solutionName"></param>
        public FolderFinder(string folderName) {
            Find(folderName);
        }

        /// <summary>
        /// Physical _specPath to the folder that was searched for.
        /// </summary>
        public string folderPath;
        /// <summary>
        /// Parent folder of 'folderPath'
        /// </summary>
        public string parentPath;



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


        protected void Find(string folderName) {
            //Where the assembly originated
            string originalFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            //Working directory
            string workingFolder = Directory.GetCurrentDirectory();

            folderPath = FindFolder(workingFolder, folderName);
            if (folderPath == null) folderPath = FindFolder(originalFolder, folderName);
            if (folderPath != null) {
                parentPath = Path.GetDirectoryName(folderPath);
            }


        }



    }
}
