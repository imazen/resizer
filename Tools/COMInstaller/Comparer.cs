using System;
using System.Collections.Generic;
using System.Text;

namespace COMInstaller {
    public class Comparer {


        List<Dll> notInstalled = new List<Dll>();
        List<Dll> leftovers = new List<Dll>();
        /// <summary>
        /// old-new
        /// </summary>
        List<KeyValuePair<Dll, Dll>> pairs = new List<KeyValuePair<Dll, Dll>>();

        DllCollection oldFiles;
        DllCollection newFiles;
        public Comparer(DllCollection oldFiles, DllCollection newFiles) {
            this.oldFiles = oldFiles;
            this.newFiles = newFiles;
            foreach (string key in oldFiles.Keys){
                if (!newFiles.ContainsKey(key)) 
                    leftovers.Add(oldFiles[key]);
                else{
                    pairs.Add(new KeyValuePair<Dll,Dll>(oldFiles[key],newFiles[key]));
                }
            }
            foreach (string key in newFiles.Keys){
                if (!oldFiles.ContainsKey(key)) notInstalled.Add(newFiles[key]);
            }
        }


        public List<KeyValuePair<Dll, Dll>> GetDifferences() {
            List<KeyValuePair<Dll, Dll>> diff = new List<KeyValuePair<Dll, Dll>>();
            foreach (KeyValuePair<Dll, Dll> pair in pairs) {
                if (!pair.Key.IsSameBuild(pair.Value)) diff.Add(pair);
            }
            return diff;
        }

        public  List<Dll> GetMatchs() {
            List<Dll> matches = new  List<Dll>();
            foreach (KeyValuePair<Dll, Dll> pair in pairs) {
                if (pair.Key.IsSameBuild(pair.Value)) matches.Add(pair.Key);
            }
            return matches;
        }
        

        public string GetAnalysis() {
            StringBuilder sb = new StringBuilder();

            if (!newFiles.ContainsKey("ImageResizer"))
                sb.AppendLine("Could not locate ImageResizer.dll! Verify that ImageResizer.dll and all the other assemblies to install are in the same folder as this application\r\n");

            if (oldFiles.Count == 0) sb.AppendLine("Nothing is currently installed! Press Install.\r\n\r\n");

            List<KeyValuePair<Dll, Dll>> differences = GetDifferences();
            if (leftovers.Count == 0 && notInstalled.Count == 0 && differences.Count == 0)
                sb.AppendLine("Everything is installed and up-to-date. Go away.\r\n\r\n");

            if (notInstalled.Count > 0) {
                sb.AppendLine("The following assemblies are not installed:\r\n--------");
                foreach (Dll d in notInstalled) {
                    sb.AppendLine(d.NameAndVersions);
                }
                sb.AppendLine();
            }

            if (differences.Count > 0) {
                sb.AppendLine("The following assemblies are installed, but have a different version number:\r\n--------");
                foreach (KeyValuePair<Dll,Dll> pair in differences) {
                    sb.AppendLine(pair.Key.NameWithoutExtension.PadRight(35)
                        + " From " + pair.Key.Version.FileVersion + " (" + pair.Key.InformationalVersion + 
                           ")");
                    sb.AppendLine("".PadLeft(35) + " ->To " + pair.Value.Version.FileVersion + " (" + pair.Value.InformationalVersion + ")");
                }
                sb.AppendLine("'From' is the existing version, 'To' is the version that will be installed");
                sb.AppendLine();
            }

            if (leftovers.Count > 0) {
                sb.AppendLine("The following left-over assemblies will be removed:\r\n--------");
                foreach (Dll d in leftovers) {
                    sb.AppendLine(d.NameAndVersions);
                }
                sb.AppendLine();
            }

            List<Dll> matches = GetMatchs();

            if (matches.Count > 0) {
                sb.AppendLine("The following assemblies are installed and up-to-date:\r\n--------");
                foreach (Dll d in matches) {
                    sb.AppendLine(d.NameAndVersions);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }


    }
}
