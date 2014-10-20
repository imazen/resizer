using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using System.IO;

namespace BuildTools {
    public class Package:IDisposable {

        ZipFile z = null;
        string basePath;
        /// <summary>
        /// Creates a zip file at the specified location, using the specified base _specPath
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="basePath"></param>
        public Package(string zipFile, string basePath) {
            if (File.Exists(zipFile)) {
                return;
            }
            z = new ZipFile(zipFile, Console.Out);
            z.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;


            this.basePath = basePath.TrimEnd('/','\\');
        }

        public void Add(IEnumerable<string> files, string targetDir = null, string sourceBaseDir = null) {
            if (z == null) return;
            string relativeTo = sourceBaseDir != null ? Path.Combine(basePath,sourceBaseDir.Replace('/','\\')) : basePath;
            string lastDir = null;
            foreach (string s in files) {
                string dir = targetDir;
                if (dir == null || sourceBaseDir != null){
                    string relPath = Path.GetDirectoryName(s);
                    if (relPath.StartsWith(relativeTo)) dir = Path.Combine(dir ?? "", relPath.Substring(relativeTo.Length).TrimStart('\\','/'));
                    else throw new Exception("SpecPath outside baseDir: " + relPath + " , " + relativeTo);
                }
                dir = dir ?? "";
                if (dir != lastDir) Console.WriteLine("\nIn folder \"" + dir + "\":");
                z.AddFile(s, dir);
                lastDir = dir;

            }
        }
        

        public void Dispose() {
            if (z == null) {
                Console.WriteLine("No changes were made to the package");
                return;
            }
            z.ZipErrorAction = ZipErrorAction.InvokeErrorEvent;
            z.ZipError += z_ZipError;
            try
            {
                z.Save();

                Console.WriteLine("Archive created successfully: " + z.Name);
                Console.WriteLine((new FileInfo(z.Name).Length / 1024) + "k compressed.");
                Console.WriteLine("Top 20 largest files (compressed)");
                var bigFiles = z.Entries.OrderByDescending<ZipEntry, long>(delegate(ZipEntry e) { return e.CompressedSize; }).Take(20);

                foreach (ZipEntry entry in bigFiles)
                {
                    Console.WriteLine((entry.CompressedSize / 1024) + "k " + entry.FileName);
                }

            }
            finally
            {
                z.Dispose();
            }
        }

        void z_ZipError(object sender, ZipErrorEventArgs e)
        {
            Console.WriteLine("Error adding file " + e.FileName);
            Console.WriteLine(e.Exception.Message);
            Console.WriteLine("(s)kip, (r)etry, or (c)ancel?");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
                e.CurrentEntry.ZipErrorAction = ZipErrorAction.Skip;
            else if (key.Key == ConsoleKey.R)
                e.CurrentEntry.ZipErrorAction = ZipErrorAction.Retry;
            else if (key.Key == ConsoleKey.C)
            {
                e.CurrentEntry.ZipErrorAction = ZipErrorAction.Throw;
            }
        }
    }
}
