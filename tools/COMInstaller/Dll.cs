using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace COMInstaller {
    public class Dll  {
        public string path;
        public Dll(string path) {
            this.path = path;
        }

        public string NameWithoutExtension {
            get {
                return Path.GetFileNameWithoutExtension(path);
            }
        }
        /// <summary>
        /// Deletes the target dll, pdb, and xml, then copies the source dll, pdb (if present), and xml (if present). 
        /// </summary>
        /// <param name="destFolder"></param>
        public void CopyAllTo(string destFolder) {
            if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

            string destDll = Path.Combine(destFolder, Path.GetFileName(path));
            string destPdb = Path.Combine(destFolder, Path.GetFileName(PdbPath));
            string destXml = Path.Combine(destFolder, Path.GetFileName(XmlPath));
            //Delete the existing files, even if they are read only.
            foreach (string existingFile in new string[] { destDll, destPdb, destXml }) {
                if (File.Exists(existingFile)) {
                    FileInfo fi = new FileInfo(existingFile);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    DeleteFile(existingFile,5,50,true);
                }
            }
            File.Copy(path, destDll, true);
            if (HasPdb) File.Copy(PdbPath, destPdb, true);
            if (HasXml) File.Copy(XmlPath, destXml, true);
        }
        /// <summary>
        /// Deletes the files
        /// </summary>
        public void DeleteAll() {
            //Delete the files, even if they are read only.
            foreach (string existingFile in new string[] { path, PdbPath, XmlPath }) {
                if (File.Exists(existingFile)) {
                    FileInfo fi = new FileInfo(existingFile);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    DeleteFile(existingFile, 5, 50, true);
                }
            }
        }


        public void DeleteFile(string file, int retries, int msBetweenRetries, bool promptUser) {
            var tries = 0;
            while (true) {
                try {
                    File.Delete(file);
                    return;
                } catch (IOException e) {
                    if (!IsFileLocked(e))
                        throw;
                    if (++tries > retries) {
                        if (promptUser && System.Windows.Forms.MessageBox.Show("Please shutdown IIS and any other applications that may be using the following file: \n" +
                            file, "File locked", System.Windows.Forms.MessageBoxButtons.RetryCancel) == System.Windows.Forms.DialogResult.Retry) {
                                tries = 0;
                        } else {
                            throw e;
                        }
                    }
                    Thread.Sleep(msBetweenRetries);
                }
            }
        }

        private static bool IsFileLocked(IOException exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }

        public string Register() {
            return new Regasm().Register(path,true, false);
        }

        public string Unregister() {
            return new Regasm().Register(path,true, true);
        }
        

        public string PdbPath{
            get{
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                    return path.Substring(0, path.Length - 4) + ".pdb";
                } else throw new Exception("Dll must end in .dll: " + path);
            }
        }
        public string XmlPath {
            get {
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                    return path.Substring(0, path.Length - 4) + ".xml";
                } else throw new Exception("Dll must end in .dll: " + path);
            }
        }

        public bool HasPdb{
            get {
                return File.Exists(PdbPath);
            }
        }

        public bool HasXml {
            get {
                return File.Exists(XmlPath);
            }
        }

        public FileVersionInfo Version { get { return FileVersionInfo.GetVersionInfo(path); } }

        public bool IsNewer(Dll other) {
            return Compare(other) == 1;
        }
        public bool IsSameVersion(Dll other) {
            return Compare(other) == 0;
        }
        /// <summary>
        /// Returns true if the assemblies share the same file version and product version numbers.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSameBuild(Dll other) {
            return this.InformationalVersion == other.InformationalVersion && this.Version.FileVersion == other.Version.FileVersion;
        }

        public string NameAndVersions {
            get {
                return this.NameWithoutExtension + ", Version=" + this.Version.FileVersion + " (" + this.InformationalVersion + ")";
            }
        }

        /// <summary>
        /// Returns 1 if the current dll is newer than other, returns -1 of 'other' is newer, and 0 if the file version numbers are identical.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int Compare(Dll other) {
            FileVersionInfo t = this.Version;
            FileVersionInfo o = other.Version;
            if (t.FileMajorPart > o.FileMajorPart) return 1;
            else if (t.FileMajorPart < o.FileMajorPart) return -1;
            if (t.FileMinorPart > o.FileMinorPart) return 1;
            else if (t.FileMinorPart < o.FileMinorPart) return -1;
            if (t.FileBuildPart > o.FileBuildPart) return 1;
            else if (t.FileBuildPart < o.FileBuildPart) return -1;
            if (t.FilePrivatePart > o.FilePrivatePart) return 1;
            else if (t.FilePrivatePart < o.FilePrivatePart) return -1;
            return 0;
        }

        public string InformationalVersion {
            get {
                return this.Version.ProductVersion;
            }
        }
        
    }
}
