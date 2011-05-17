using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageResizerGUI.Code
{
    class FileTools
    {
        /// <summary>
        /// Taken from http://www.dotnetperls.com/recursively-find-files
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static List<string> GetFilesRecursive(string rootPath)
        {
            // 1.
            // Store results in the file results list.
            List<string> result = new List<string>();

            // 2.
            // Store a stack of our directories.
            Stack<string> stack = new Stack<string>();

            // 3.
            // Add initial directory.
            stack.Push(rootPath);

            // 4.
            // Continue while there are directories to process
            while (stack.Count > 0)
            {
                // A.
                // Get top directory
                string dir = stack.Pop();

                try
                {
                    // B
                    // Add all files at this directory to the result List.
                    // Now filtered by Image File Types
                    result.AddRange(Directory.GetFiles(dir, "*.bmp"));
                    result.AddRange(Directory.GetFiles(dir, "*.jpg"));
                    result.AddRange(Directory.GetFiles(dir, "*.gif"));
                    result.AddRange(Directory.GetFiles(dir, "*.png"));

                    // C
                    // Add all directories at this directory.
                    foreach (string dn in Directory.GetDirectories(dir))
                    {
                        stack.Push(dn);
                    }
                }
                catch
                {
                    // D
                    // Could not open the directory
                }
            }
            return result;
        }
    }
}
