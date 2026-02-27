// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ImageResizer.IISExpressTests {
    public class SiteCreator {
        public SiteCreator() {
            FindFolders();
            if (dllFolder == null || imagesFolder == null)
                throw new Exception("Could not find image and dll folders! dllFolder=" + dllFolder + " imagesFolder=" + imagesFolder);
        }

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

        public string dllFolder = null;
        public string imagesFolder = null;

        protected void FindFolders() {
            string originalFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string workingFolder = Directory.GetCurrentDirectory();

            dllFolder = FindFolder(workingFolder, "dlls");
            if (dllFolder == null) dllFolder = FindFolder(originalFolder, "dlls");
            if (dllFolder != null) dllFolder = FindFolder(dllFolder, "release");

            string samplesFolder = FindFolder(workingFolder, "Samples");
            if (samplesFolder == null) samplesFolder = FindFolder(originalFolder, "Samples");

            if (samplesFolder != null) imagesFolder = FindFolder(samplesFolder, "Images");
        }

        protected void Copy(string sourceFolder, string destFolder, string[] filters, string[] excludePartialMatches = null) {
            foreach (string filter in filters) {
                string[] files = Directory.GetFiles(sourceFolder, filter);
                foreach (string file in files) {
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
            dir = null;
            do {
                dir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            } while (Directory.Exists(dir));

            binDir = Path.Combine(dir, "bin");

            Debug.WriteLine("Create " + dir);
            Directory.CreateDirectory(dir);
            Debug.WriteLine("Create " + binDir);
            Directory.CreateDirectory(binDir);

            // Copy DLLs — exclude test assemblies
            Copy(dllFolder, binDir, new string[] { "*.dll", "*.pdb" }, new string[] { ".Tests", "xunit", "NHamcrest" });
            // Copy images
            Copy(imagesFolder, dir, new string[] { "*.jpg", "*.png", "*.jpeg", "*.tiff", "*.tif", "*.tff", "*.psd", "*.gif", "*.bmp" });

            // Create a non-image file for passthrough test
            File.WriteAllText(Path.Combine(dir, "notanimage.txt"), "This is not an image.", System.Text.Encoding.UTF8);

            return this;
        }

        public void WriteWebConfig(string contents) {
            Debug.WriteLine("Create " + Path.Combine(dir, "web.config"));
            File.WriteAllText(Path.Combine(dir, "web.config"), contents, System.Text.Encoding.UTF8);
        }
    }
}
