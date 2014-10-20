using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace BuildTools {
    public class FolderFinder {
        /// <summary>
        /// Attempts to locate the specified folder by traversing up (Towards C:\) the directory tree from two locations (the original Builder exe location, and the current working directory).
        /// Checks siblings at each level, looking for the folder. 
        /// 
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="solutionName"></param>
        public FolderFinder(string folderName) {
            Find(folderName);
        }

        /// <summary>
        /// Physical _specPath to the folder that was searched for.
        /// </summary>
        public string FolderPath;
        /// <summary>
        /// Parent folder of 'folderPath'
        /// </summary>
        public string ParentPath;


        /// <summary>
        /// Starts in the 'startPath' directory, searches for any child directories matching 'query', and continues up to C until it finds a match.
        /// Returns null if there are no matches.
        /// </summary>
        /// <param name="startPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
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

            FolderPath = FindFolder(workingFolder, folderName);
            if (FolderPath == null) FolderPath = FindFolder(originalFolder, folderName);
            if (FolderPath != null) {
                ParentPath = Path.GetDirectoryName(FolderPath);
            }
        }



    }
}
