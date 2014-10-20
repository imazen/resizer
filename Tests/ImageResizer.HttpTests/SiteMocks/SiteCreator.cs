using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace ImageResizer.Core.Tests.SiteMocks {
    public class SiteCreator {
        public SiteCreator() {
            FindFolders();
            if (dllFolder == null || imagesFolder == null) throw new Exception("Could not find image and dll folders!");
        }

        public string FindFolder(string startPath,string query) {
            DirectoryInfo di =  new DirectoryInfo(startPath);
            while (di != null) {
                DirectoryInfo[] results = di.GetDirectories(query, SearchOption.TopDirectoryOnly);
                if (results != null && results.Count<DirectoryInfo>() > 0)
                    return results[0].FullName;

                di = di.Parent;
            }
            return null;
        }

        public string dllFolder = null;
        public string imagesFolder = null;

        protected void FindFolders() {
            //Where the assembly originated (since MBunit may copy dlls)
            string originalFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            //Working directory
            string workingFolder = Directory.GetCurrentDirectory();

            dllFolder = FindFolder(workingFolder, "Dlls");
            if (dllFolder == null) dllFolder = FindFolder(originalFolder, "Dlls");
            if (dllFolder != null) dllFolder = FindFolder(dllFolder, "debug");

            string samplesFolder = FindFolder(workingFolder, "Samples");
            if (samplesFolder == null) samplesFolder = FindFolder(originalFolder, "Samples");

            if (samplesFolder != null) imagesFolder = FindFolder(samplesFolder, "Images");
        }

        protected void Copy(string sourceFolder, string destFolder, string[] filters, string[] excludePartialMatches = null) {
            foreach (string filter in filters) {
                string[] files = Directory.GetFiles(sourceFolder, filter);
                foreach (string file in files) {
                    //Is the file excluded?
                    bool excluded = false;
                    if (excludePartialMatches != null)
                        foreach (string s in excludePartialMatches) 
                            if (file.IndexOf(s, StringComparison.OrdinalIgnoreCase) > -1) excluded = true;
                    
                    
                    if (!excluded) {
                        string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                        Debug.WriteLine("Copy " + file.PadRight(150) + " to " + destFile);
                        File.Copy(file, destFile, false);
                    }
                }
            }
        }
        public string dir = null;
        public string binDir = null;
        public SiteCreator Create() {
            //Find an non-existent temp directory.
            dir = null;
            do { dir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            } while (Directory.Exists(dir));

            binDir = Path.Combine(dir, "bin");

            //create app dir
            Debug.WriteLine("Create " + dir);
            Directory.CreateDirectory(dir);
            //create bin dir
            Debug.WriteLine("Create " + binDir);
            Directory.CreateDirectory(binDir);
            
            //Copy DLLs
            Copy(dllFolder, binDir, new string[] { "*.dll", "*.pdb" }, new string[]{"mbunit","gallio",".Tests","NHamcrest", "Cassini"});
            //Copy images
            Copy(imagesFolder, dir, new string[] { "*.jpg", "*.png", "*.jpeg", "*.tiff", "*.tif", "*.tff", "*.psd", "*.gif", "*.bmp" });
            return this;
        }

        public void WriteWebConfig(string contents) {
            //Create web.config
            Debug.WriteLine("Create " + Path.Combine(dir, "web.config"));
            System.IO.File.WriteAllText(Path.Combine(dir, "web.config"), contents, UTF8Encoding.UTF8);

        }
    }
}
